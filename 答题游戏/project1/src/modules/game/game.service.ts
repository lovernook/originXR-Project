import { Injectable, NotFoundException, BadRequestException, ForbiddenException, Logger } from '@nestjs/common';
import { PrismaService } from '../../common/prisma/prisma.service';
import { RedisService } from '../../common/redis/redis.service';
import { UserService } from '../user/user.service';
import { QuestionService } from '../question-bank/question.service';
import { SubmitAnswerDto, CreateStageDto } from './dto';

interface BattleSession {
  stageId: string;
  userId: string;
  questions: any[];
  currentIndex: number;
  lives: number;
  maxLives: number;
  score: number;
  combo: number;
  maxCombo: number;
  correctCount: number;
  totalQuestions: number;
  startedAt: number;
}

@Injectable()
export class GameService {
  private readonly logger = new Logger(GameService.name);

  constructor(
    private prisma: PrismaService,
    private redis: RedisService,
    private userService: UserService,
    private questionService: QuestionService,
  ) {}

  async getStages(userId: string) {
    const stages = await this.prisma.stage.findMany({
      where: { isActive: true },
      orderBy: { stageNumber: 'asc' },
      select: {
        id: true, name: true, description: true, stageNumber: true,
        bossName: true, bossModelId: true, bossHp: true, questionCount: true,
        expReward: true, goldReward: true, energyCost: true, unlockLevel: true,
      },
    });

    const progress = await this.prisma.userProgress.findMany({
      where: { userId },
    });

    const progressMap = new Map(progress.map((p) => [p.stageId, p]));

    return stages.map((stage) => ({
      ...stage,
      progress: progressMap.get(stage.id) || { status: stage.stageNumber === 1 ? 'UNLOCKED' : 'LOCKED' },
    }));
  }

  async startStage(userId: string, stageId: string) {
    const stage = await this.prisma.stage.findUnique({
      where: { id: stageId },
      include: { questionPools: true },
    });
    if (!stage) throw new NotFoundException('关卡不存在');

    const hasEnergy = await this.userService.consumeEnergy(userId, stage.energyCost);
    if (!hasEnergy) throw new BadRequestException('体力不足');

    const user = await this.prisma.user.findUnique({ where: { id: userId } });
    if (!user) throw new NotFoundException('用户不存在');
    if (user.level < stage.unlockLevel) throw new ForbiddenException('等级不够');

    this.logger.log(`🎮 [${user.nickname || user.username}] 开始关卡: ${stage.name} (体力消耗:${stage.energyCost})`);

    // 根据题目池配置抽题
    let knowledgePointIds: string[] = [];
    if (stage.questionPools.length > 0) {
      knowledgePointIds = stage.questionPools.map((p) => p.knowledgePointId);
    }

    let questions: any[];
    if (knowledgePointIds.length > 0) {
      questions = await this.questionService.smartPick({
        knowledgePointIds,
        count: stage.questionCount,
        difficultyMin: 1,
        difficultyMax: 5,
      });
    } else {
      // 随机抽取已发布题目
      const allQuestions = await this.prisma.question.findMany({
        where: { status: 'PUBLISHED' },
        include: { options: { orderBy: { sortOrder: 'asc' } } },
        take: 100,
      });
      const shuffled = allQuestions.sort(() => Math.random() - 0.5);
      questions = shuffled.slice(0, stage.questionCount).map((q) => ({
        ...q,
        options: q.options.map(({ isCorrect, ...rest }) => rest),
      }));
    }

    // 存储战斗会话到Redis
    const session: BattleSession = {
      stageId, userId, questions,
      currentIndex: 0, lives: stage.maxLives, maxLives: stage.maxLives,
      score: 0, combo: 0, maxCombo: 0,
      correctCount: 0, totalQuestions: questions.length,
      startedAt: Date.now(),
    };

    const sessionKey = `battle:${userId}:${stageId}`;
    await this.redis.set(sessionKey, JSON.stringify(session), 3600);

    return {
      stageId, stageName: stage.name,
      bossName: stage.bossName, bossHp: stage.bossHp,
      maxLives: stage.maxLives, totalQuestions: questions.length,
      currentQuestion: questions[0] || null,
      currentIndex: 0,
    };
  }

  async submitAnswer(userId: string, stageId: string, dto: SubmitAnswerDto) {
    const sessionKey = `battle:${userId}:${stageId}`;
    const sessionData = await this.redis.get(sessionKey);
    if (!sessionData) throw new BadRequestException('对战会话不存在或已过期');

    const session: BattleSession = JSON.parse(sessionData);

    // 服务端校验答案
    const question = await this.prisma.question.findUnique({
      where: { id: dto.questionId },
      include: { options: true },
    });
    if (!question) throw new NotFoundException('题目不存在');

    const correctOptions = question.options.filter((o) => o.isCorrect);
    const isCorrect = correctOptions.some((o) => o.optionKey === dto.selectedAnswer);

    // 防作弊：答题时间过短检查
    const isSuspicious = dto.timeSpent < 2;

    let comboBonus = 0;
    if (isCorrect) {
      session.correctCount++;
      session.combo++;
      if (session.combo > session.maxCombo) session.maxCombo = session.combo;
      const baseScore = 10;
      comboBonus = session.combo >= 3 ? baseScore : 0;
      session.score += baseScore + comboBonus;

      if (comboBonus > 0) {
        this.logger.log(`🔥 连击x${session.combo}! 双倍分 +${baseScore + comboBonus}`);
      }
    } else {
      session.combo = 0;
      session.lives--;
      this.logger.warn(`❌ 答错 (题:${dto.questionId.slice(-6)}), 剩余生命:${session.lives}/${session.maxLives}`);
    }

    session.currentIndex++;

    // 记录答题
    await this.prisma.userAnswerRecord.create({
      data: {
        userId, questionId: dto.questionId, stageId,
        mode: 'PVE_STAGE', selectedAnswer: dto.selectedAnswer,
        isCorrect, timeSpent: dto.timeSpent,
        score: isCorrect ? 10 + comboBonus : 0,
      },
    });

    // 更新题目统计
    await this.prisma.question.update({
      where: { id: dto.questionId },
      data: {
        totalAttempts: { increment: 1 },
        ...(isCorrect ? { correctCount: { increment: 1 } } : { incorrectCount: { increment: 1 } }),
      },
    });

    // 检查生命或题目耗尽
    const isGameOver = session.lives <= 0;
    const isCompleted = session.currentIndex >= session.totalQuestions;

    // 更新Redis会话
    await this.redis.set(sessionKey, JSON.stringify(session), 3600);

    const result: any = {
      isCorrect, combo: session.combo, comboBonus, lives: session.lives,
      score: session.score, currentIndex: session.currentIndex,
      correctAnswer: correctOptions.map((o) => o.optionKey).join(','),
      explanation: question.explanation,
      isSuspicious,
    };

    if (!isGameOver && !isCompleted) {
      result.nextQuestion = session.questions[session.currentIndex] || null;
    }

    if (isGameOver || isCompleted) {
      result.finished = true;
      result.summary = await this.finishStage(userId, stageId, session, isGameOver);
    }

    return result;
  }

  private async finishStage(userId: string, stageId: string, session: BattleSession, isGameOver: boolean) {
    const stage = await this.prisma.stage.findUnique({ where: { id: stageId } });
    if (!stage) throw new NotFoundException('关卡不存在');
    const accuracy = session.totalQuestions > 0 ? session.correctCount / session.totalQuestions : 0;
    const passed = !isGameOver && accuracy >= (stage.passScore / 100);

    let expGained = 0;
    let goldGained = 0;
    let stars = 0;

    if (passed) {
      expGained = stage.expReward;
      goldGained = stage.goldReward;
      stars = accuracy >= 0.9 ? 3 : accuracy >= 0.7 ? 2 : 1;

      await this.userService.addExp(userId, expGained);
      await this.prisma.user.update({
        where: { id: userId },
        data: { gold: { increment: goldGained } },
      });

      await this.redis.zadd('rank:global:score', session.score, userId);
    }

    // 更新关卡进度
    await this.prisma.userProgress.upsert({
      where: { userId_stageId: { userId, stageId } },
      create: {
        userId, stageId,
        status: passed ? 'COMPLETED' : 'UNLOCKED',
        bestScore: session.score, bestAccuracy: accuracy,
        attempts: 1, stars,
        completedAt: passed ? new Date() : null,
      },
      update: {
        status: passed ? 'COMPLETED' : undefined,
        bestScore: { set: session.score },
        bestAccuracy: accuracy,
        attempts: { increment: 1 },
        stars: { set: stars },
        completedAt: passed ? new Date() : undefined,
      },
    });

    // 解锁下一关
    if (passed) {
      const nextStage = await this.prisma.stage.findFirst({
        where: { stageNumber: stage.stageNumber + 1, isActive: true },
      });
      if (nextStage) {
        await this.prisma.userProgress.upsert({
          where: { userId_stageId: { userId, stageId: nextStage.id } },
          create: { userId, stageId: nextStage.id, status: 'UNLOCKED' },
          update: {},
        });
      }
    }

    // 清理Redis会话
    await this.redis.del(`battle:${userId}:${stageId}`);

    const timeSpent = Math.floor((Date.now() - session.startedAt) / 1000);
    const resultLabel = passed ? '🏆 通关' : (isGameOver ? '💀 失败' : '📋 完成');
    this.logger.log(`${resultLabel} | 关卡:${stage.name} | 得分:${session.score} | 准确率:${Math.round(accuracy * 100)}% | ⭐${stars} | 连击:${session.maxCombo} | 耗时:${timeSpent}s${passed ? ` | +${expGained}EXP +${goldGained}金币` : ''}`);

    return {
      passed, score: session.score, accuracy: Math.round(accuracy * 100),
      correctCount: session.correctCount, totalQuestions: session.totalQuestions,
      maxCombo: session.maxCombo, stars, expGained, goldGained,
      timeSpent,
    };
  }

  // ========== 每日挑战 ==========
  async getDailyChallenge(userId: string) {
    const today = new Date().toISOString().slice(0, 10);
    const record = await this.prisma.dailyChallengeRecord.findFirst({
      where: { userId, challengeDate: new Date(today) },
      orderBy: { attempt: 'desc' },
    });

    const attempts = record ? record.attempt : 0;
    const questions = await this.questionService.smartPick({
      knowledgePointIds: [],
      count: 20, difficultyMin: 1, difficultyMax: 5,
    });

    // 对于每日挑战，如果没有指定知识点就随机抽
    if (questions.length === 0) {
      const allQ = await this.prisma.question.findMany({
        where: { status: 'PUBLISHED' },
        include: { options: { orderBy: { sortOrder: 'asc' } } },
        take: 50,
      });
      const shuffled = allQ.sort(() => Math.random() - 0.5).slice(0, 20);
      return { attempts, maxAttempts: 3, questions: shuffled };
    }

    return { attempts, maxAttempts: 3, questions };
  }

  // ========== 知识塔 ==========
  async startTower(userId: string) {
    const seasonId = new Date().toISOString().slice(0, 7);
    let record = await this.prisma.towerRecord.findUnique({
      where: { userId_seasonId: { userId, seasonId } },
    });

    if (!record) {
      record = await this.prisma.towerRecord.create({
        data: { userId, seasonId, currentFloor: 0, highestFloor: 0, score: 0 },
      });
    }

    const nextFloor = record.currentFloor + 1;
    const difficulty = Math.min(Math.ceil(nextFloor / 10), 5);

    const questions = await this.prisma.question.findMany({
      where: { status: 'PUBLISHED', difficulty: { lte: difficulty } },
      include: { options: { orderBy: { sortOrder: 'asc' } } },
      take: 50,
    });

    const shuffled = questions.sort(() => Math.random() - 0.5).slice(0, 5);

    return {
      floor: nextFloor, difficulty, seasonId,
      highestFloor: record.highestFloor,
      isCheckpoint: nextFloor % 5 === 0,
      isBossFloor: nextFloor % 10 === 0,
      questions: shuffled.map((q) => ({
        ...q, options: q.options.map(({ isCorrect, ...r }) => r),
      })),
    };
  }

  // ========== Admin: Stage CRUD ==========
  async createStage(dto: CreateStageDto) {
    return this.prisma.stage.create({ data: dto as any });
  }

  async updateStage(id: string, dto: Partial<CreateStageDto>) {
    return this.prisma.stage.update({ where: { id }, data: dto as any });
  }

  async deleteStage(id: string) {
    await this.prisma.stage.update({ where: { id }, data: { isActive: false } });
    return { deleted: true };
  }

  async getAllStagesAdmin() {
    return this.prisma.stage.findMany({
      orderBy: { stageNumber: 'asc' },
      include: { questionPools: true, rewards: true },
    });
  }
}
