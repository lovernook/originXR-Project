import { Injectable, NotFoundException, BadRequestException } from '@nestjs/common';
import { PrismaService } from '../../common/prisma/prisma.service';
import { RedisService } from '../../common/redis/redis.service';
import { PaginatedResult } from '../../common/dto';
import {
  CreateQuestionDto,
  UpdateQuestionDto,
  QueryQuestionDto,
  ReviewQuestionDto,
  SmartPickDto,
} from './dto';

@Injectable()
export class QuestionService {
  constructor(
    private prisma: PrismaService,
    private redis: RedisService,
  ) {}

  async create(dto: CreateQuestionDto, createdBy: string) {
    const question = await this.prisma.question.create({
      data: {
        type: dto.type as any,
        content: dto.content,
        mediaUrl: dto.mediaUrl,
        difficulty: dto.difficulty,
        timeLimit: dto.timeLimit || 30,
        explanation: dto.explanation,
        source: dto.source,
        createdBy,
        options: {
          create: dto.options.map((opt, idx) => ({
            optionKey: opt.optionKey,
            content: opt.content,
            mediaUrl: opt.mediaUrl,
            isCorrect: opt.isCorrect,
            sortOrder: opt.sortOrder ?? idx,
          })),
        },
        knowledgePoints: dto.knowledgePointIds
          ? {
              create: dto.knowledgePointIds.map((kpId) => ({
                knowledgePointId: kpId,
              })),
            }
          : undefined,
        tags: dto.tags
          ? {
              create: dto.tags.map((tag) => ({ tag })),
            }
          : undefined,
      },
      include: {
        options: true,
        knowledgePoints: { include: { knowledgePoint: true } },
        tags: true,
      },
    });

    return question;
  }

  async findAll(query: QueryQuestionDto) {
    const { page = 1, pageSize = 20, search, type, status, difficulty, knowledgePointId, tag } = query;
    const where: any = {};

    if (search) {
      where.content = { contains: search, mode: 'insensitive' };
    }
    if (type) where.type = type;
    if (status) where.status = status;
    if (difficulty) where.difficulty = difficulty;

    if (knowledgePointId) {
      where.knowledgePoints = {
        some: { knowledgePointId },
      };
    }

    if (tag) {
      where.tags = {
        some: { tag: { contains: tag, mode: 'insensitive' } },
      };
    }

    const [items, total] = await Promise.all([
      this.prisma.question.findMany({
        where,
        include: {
          options: { orderBy: { sortOrder: 'asc' } },
          knowledgePoints: { include: { knowledgePoint: { select: { id: true, name: true } } } },
          tags: true,
        },
        skip: (page - 1) * pageSize,
        take: pageSize,
        orderBy: { createdAt: 'desc' },
      }),
      this.prisma.question.count({ where }),
    ]);

    return new PaginatedResult(items, total, page, pageSize);
  }

  async findOne(id: string) {
    const question = await this.prisma.question.findUnique({
      where: { id },
      include: {
        options: { orderBy: { sortOrder: 'asc' } },
        knowledgePoints: { include: { knowledgePoint: true } },
        tags: true,
        versions: { orderBy: { version: 'desc' }, take: 10 },
      },
    });

    if (!question) throw new NotFoundException('题目不存在');
    return question;
  }

  async update(id: string, dto: UpdateQuestionDto, updatedBy: string) {
    const existing = await this.prisma.question.findUnique({
      where: { id },
      include: { options: true },
    });

    if (!existing) throw new NotFoundException('题目不存在');

    // 保存版本快照
    await this.prisma.questionVersion.create({
      data: {
        questionId: id,
        version: existing.version,
        content: existing.content,
        options: existing.options as any,
        answer: existing.options
          .filter((o) => o.isCorrect)
          .map((o) => o.optionKey)
          .join(','),
        changedBy: updatedBy,
      },
    });

    // 更新题目
    const updateData: any = {
      version: { increment: 1 },
    };

    if (dto.content !== undefined) updateData.content = dto.content;
    if (dto.mediaUrl !== undefined) updateData.mediaUrl = dto.mediaUrl;
    if (dto.difficulty !== undefined) updateData.difficulty = dto.difficulty;
    if (dto.timeLimit !== undefined) updateData.timeLimit = dto.timeLimit;
    if (dto.explanation !== undefined) updateData.explanation = dto.explanation;

    // 更新选项
    if (dto.options) {
      await this.prisma.questionOption.deleteMany({ where: { questionId: id } });
      await this.prisma.questionOption.createMany({
        data: dto.options.map((opt, idx) => ({
          questionId: id,
          optionKey: opt.optionKey,
          content: opt.content,
          mediaUrl: opt.mediaUrl,
          isCorrect: opt.isCorrect,
          sortOrder: opt.sortOrder ?? idx,
        })),
      });
    }

    // 更新知识点关联
    if (dto.knowledgePointIds) {
      await this.prisma.questionKnowledgePoint.deleteMany({
        where: { questionId: id },
      });
      await this.prisma.questionKnowledgePoint.createMany({
        data: dto.knowledgePointIds.map((kpId) => ({
          questionId: id,
          knowledgePointId: kpId,
        })),
      });
    }

    // 更新标签
    if (dto.tags) {
      await this.prisma.questionTag.deleteMany({ where: { questionId: id } });
      await this.prisma.questionTag.createMany({
        data: dto.tags.map((tag) => ({ questionId: id, tag })),
      });
    }

    const updated = await this.prisma.question.update({
      where: { id },
      data: updateData,
      include: {
        options: { orderBy: { sortOrder: 'asc' } },
        knowledgePoints: { include: { knowledgePoint: true } },
        tags: true,
      },
    });

    return updated;
  }

  async remove(id: string) {
    await this.prisma.question.delete({ where: { id } });
    return { deleted: true };
  }

  async review(id: string, dto: ReviewQuestionDto, reviewedBy: string) {
    const question = await this.prisma.question.findUnique({ where: { id } });
    if (!question) throw new NotFoundException('题目不存在');

    if (question.status !== 'PENDING_REVIEW' && question.status !== 'DRAFT') {
      throw new BadRequestException('当前状态不允许审核');
    }

    return this.prisma.question.update({
      where: { id },
      data: {
        status: dto.status as any,
        reviewedBy,
        reviewedAt: new Date(),
      },
    });
  }

  async submitForReview(id: string) {
    return this.prisma.question.update({
      where: { id },
      data: { status: 'PENDING_REVIEW' },
    });
  }

  /**
   * 智能组卷：按知识点+难度随机抽题
   */
  async smartPick(dto: SmartPickDto) {
    const { knowledgePointIds, difficultyMin = 1, difficultyMax = 5, count } = dto;

    const questions = await this.prisma.question.findMany({
      where: {
        status: 'PUBLISHED',
        difficulty: { gte: difficultyMin, lte: difficultyMax },
        knowledgePoints: {
          some: { knowledgePointId: { in: knowledgePointIds } },
        },
      },
      include: {
        options: { orderBy: { sortOrder: 'asc' } },
      },
    });

    // 随机抽取
    const shuffled = questions.sort(() => Math.random() - 0.5);
    const picked = shuffled.slice(0, count);

    // 不下发正确答案给客户端
    return picked.map((q) => ({
      ...q,
      options: q.options.map(({ isCorrect, ...rest }) => rest),
    }));
  }

  /**
   * 题目统计数据
   */
  async getQuestionStats(id: string) {
    const question = await this.prisma.question.findUnique({
      where: { id },
      select: {
        totalAttempts: true,
        correctCount: true,
        incorrectCount: true,
      },
    });

    if (!question) throw new NotFoundException('题目不存在');

    const avgTime = await this.prisma.userAnswerRecord.aggregate({
      where: { questionId: id },
      _avg: { timeSpent: true },
    });

    return {
      ...question,
      accuracy:
        question.totalAttempts > 0
          ? question.correctCount / question.totalAttempts
          : 0,
      avgTimeSpent: avgTime._avg.timeSpent || 0,
    };
  }
}
