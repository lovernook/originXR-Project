import { Injectable } from '@nestjs/common';
import { PrismaService } from '../../common/prisma/prisma.service';

@Injectable()
export class AnalyticService {
  constructor(private prisma: PrismaService) {}

  async getOverview() {
    const [totalUsers, totalQuestions, totalStages] = await Promise.all([
      this.prisma.user.count({ where: { role: 'STUDENT' } }),
      this.prisma.question.count(),
      this.prisma.stage.count({ where: { isActive: true } }),
    ]);

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const [todayActive, todayAnswers, publishedQuestions] = await Promise.all([
      this.prisma.user.count({ where: { lastLoginAt: { gte: today } } }),
      this.prisma.userAnswerRecord.count({ where: { createdAt: { gte: today } } }),
      this.prisma.question.count({ where: { status: 'PUBLISHED' } }),
    ]);

    const accuracyAgg = await this.prisma.userAnswerRecord.aggregate({
      where: { createdAt: { gte: today } },
      _count: true,
    });

    const correctToday = await this.prisma.userAnswerRecord.count({
      where: { createdAt: { gte: today }, isCorrect: true },
    });

    return {
      totalUsers, totalQuestions, publishedQuestions, totalStages,
      todayActive, todayAnswers,
      todayAccuracy: accuracyAgg._count > 0 ? correctToday / accuracyAgg._count : 0,
    };
  }

  async getQuestionAnalytics() {
    // 难度分布
    const difficultyDist = await this.prisma.question.groupBy({
      by: ['difficulty'],
      _count: true,
      where: { status: 'PUBLISHED' },
    });

    // 类型分布
    const typeDist = await this.prisma.question.groupBy({
      by: ['type'],
      _count: true,
    });

    // 异常题目（正确率过低 <20% 或过高 >95%，且答题次数>10）
    const anomalies = await this.prisma.question.findMany({
      where: {
        totalAttempts: { gt: 10 },
        OR: [
          { correctCount: { lt: this.prisma.question.fields?.totalAttempts } },
        ],
      },
      take: 20,
      orderBy: { totalAttempts: 'desc' },
      select: {
        id: true, content: true, difficulty: true,
        totalAttempts: true, correctCount: true, incorrectCount: true,
      },
    });

    // 手动计算异常
    const allWithStats = await this.prisma.question.findMany({
      where: { totalAttempts: { gt: 10 }, status: 'PUBLISHED' },
      select: { id: true, content: true, difficulty: true, totalAttempts: true, correctCount: true },
      orderBy: { totalAttempts: 'desc' },
      take: 100,
    });

    const anomalyQuestions = allWithStats.filter((q) => {
      const rate = q.correctCount / q.totalAttempts;
      return rate < 0.2 || rate > 0.95;
    });

    return { difficultyDist, typeDist, anomalyQuestions };
  }

  async getStudentAnalytics() {
    // 活跃度趋势（最近7天）
    const days: { date: string; activeUsers: number }[] = [];
    for (let i = 6; i >= 0; i--) {
      const date = new Date();
      date.setDate(date.getDate() - i);
      date.setHours(0, 0, 0, 0);
      const nextDate = new Date(date.getTime() + 24 * 60 * 60 * 1000);

      const count = await this.prisma.user.count({
        where: { lastLoginAt: { gte: date, lt: nextDate } },
      });

      days.push({ date: date.toISOString().slice(0, 10), activeUsers: count });
    }

    // 用户等级分布
    const levelDist = await this.prisma.user.groupBy({
      by: ['level'],
      _count: true,
      where: { role: 'STUDENT' },
      orderBy: { level: 'asc' },
    });

    return { activeTrend: days, levelDistribution: levelDist };
  }

  async getStageAnalytics() {
    const stages = await this.prisma.stage.findMany({
      where: { isActive: true },
      orderBy: { stageNumber: 'asc' },
      select: { id: true, name: true, stageNumber: true },
    });

    const results = await Promise.all(
      stages.map(async (stage) => {
        const progress = await this.prisma.userProgress.findMany({
          where: { stageId: stage.id },
        });

        const totalAttempts = progress.reduce((s, p) => s + p.attempts, 0);
        const completed = progress.filter((p) => p.status === 'COMPLETED').length;

        return {
          ...stage,
          totalAttempts, completedCount: completed,
          passRate: progress.length > 0 ? completed / progress.length : 0,
          avgScore: progress.length > 0
            ? progress.reduce((s, p) => s + p.bestScore, 0) / progress.length : 0,
        };
      }),
    );

    return results;
  }
}
