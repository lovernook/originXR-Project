import { Injectable } from '@nestjs/common';
import { PrismaService } from '../../common/prisma/prisma.service';
import { CreateSubjectDto, CreateChapterDto, CreateKnowledgePointDto } from './dto';

@Injectable()
export class KnowledgeService {
  constructor(private prisma: PrismaService) {}

  async findAllSubjects() {
    return this.prisma.subject.findMany({
      where: { isActive: true },
      include: {
        chapters: {
          where: { isActive: true },
          orderBy: { sortOrder: 'asc' },
          include: {
            knowledgePoints: {
              where: { isActive: true },
              orderBy: { sortOrder: 'asc' },
            },
          },
        },
      },
      orderBy: { sortOrder: 'asc' },
    });
  }

  async createSubject(dto: CreateSubjectDto) {
    return this.prisma.subject.create({ data: dto });
  }

  async updateSubject(id: string, dto: Partial<CreateSubjectDto>) {
    return this.prisma.subject.update({ where: { id }, data: dto });
  }

  async removeSubject(id: string) {
    await this.prisma.subject.update({ where: { id }, data: { isActive: false } });
    return { deleted: true };
  }

  async findChaptersBySubject(subjectId: string) {
    return this.prisma.chapter.findMany({
      where: { subjectId, isActive: true },
      include: {
        knowledgePoints: {
          where: { isActive: true },
          orderBy: { sortOrder: 'asc' },
        },
      },
      orderBy: { sortOrder: 'asc' },
    });
  }

  async createChapter(dto: CreateChapterDto) {
    return this.prisma.chapter.create({ data: dto });
  }

  async updateChapter(id: string, dto: Partial<CreateChapterDto>) {
    return this.prisma.chapter.update({ where: { id }, data: dto });
  }

  async removeChapter(id: string) {
    await this.prisma.chapter.update({ where: { id }, data: { isActive: false } });
    return { deleted: true };
  }

  async getKnowledgeTree() {
    return this.prisma.subject.findMany({
      where: { isActive: true },
      include: {
        chapters: {
          where: { isActive: true },
          orderBy: { sortOrder: 'asc' },
          include: {
            knowledgePoints: {
              where: { isActive: true, parentId: null },
              orderBy: { sortOrder: 'asc' },
              include: {
                children: {
                  where: { isActive: true },
                  orderBy: { sortOrder: 'asc' },
                  include: {
                    children: { where: { isActive: true }, orderBy: { sortOrder: 'asc' } },
                  },
                },
                _count: { select: { questionLinks: true } },
              },
            },
          },
        },
      },
      orderBy: { sortOrder: 'asc' },
    });
  }

  async findKnowledgePointsByChapter(chapterId: string) {
    return this.prisma.knowledgePoint.findMany({
      where: { chapterId, isActive: true },
      include: {
        children: { where: { isActive: true }, orderBy: { sortOrder: 'asc' } },
        questionLinks: { select: { questionId: true } },
      },
      orderBy: { sortOrder: 'asc' },
    });
  }

  async createKnowledgePoint(dto: CreateKnowledgePointDto) {
    return this.prisma.knowledgePoint.create({ data: dto });
  }

  async updateKnowledgePoint(id: string, dto: Partial<CreateKnowledgePointDto>) {
    return this.prisma.knowledgePoint.update({ where: { id }, data: dto });
  }

  async removeKnowledgePoint(id: string) {
    await this.prisma.knowledgePoint.update({ where: { id }, data: { isActive: false } });
    return { deleted: true };
  }
}
