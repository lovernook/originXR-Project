import { Injectable, NotFoundException, BadRequestException, Logger } from '@nestjs/common';
import { PrismaService } from '../../common/prisma/prisma.service';
import { RedisService } from '../../common/redis/redis.service';
import { PaginationDto, PaginatedResult } from '../../common/dto';
import { UpdateProfileDto, AdminQueryUsersDto } from './dto';

// 等级经验曲线：每级所需经验 = 100 * level^1.5
function expForLevel(level: number): number {
  return Math.floor(100 * Math.pow(level, 1.5));
}

@Injectable()
export class UserService {
  private readonly logger = new Logger(UserService.name);

  constructor(
    private prisma: PrismaService,
    private redis: RedisService,
  ) {}

  async getProfile(userId: string) {
    const user = await this.prisma.user.findUnique({
      where: { id: userId },
      select: {
        id: true,
        username: true,
        email: true,
        phone: true,
        nickname: true,
        avatar: true,
        role: true,
        level: true,
        exp: true,
        gold: true,
        diamond: true,
        energy: true,
        maxEnergy: true,
        energyUpdatedAt: true,
        lastLoginAt: true,
        createdAt: true,
      },
    });

    if (!user) throw new NotFoundException('用户不存在');

    // 计算实时体力
    const currentEnergy = this.calculateCurrentEnergy(
      user.energy,
      user.maxEnergy,
      user.energyUpdatedAt,
    );

    return {
      ...user,
      energy: currentEnergy,
      expToNextLevel: expForLevel(user.level),
    };
  }

  async updateProfile(userId: string, dto: UpdateProfileDto) {
    return this.prisma.user.update({
      where: { id: userId },
      data: dto,
      select: {
        id: true,
        username: true,
        nickname: true,
        avatar: true,
      },
    });
  }

  async getUserProgress(userId: string) {
    const progress = await this.prisma.userProgress.findMany({
      where: { userId },
      include: {
        stage: {
          select: {
            id: true,
            name: true,
            stageNumber: true,
          },
        },
      },
      orderBy: { stage: { stageNumber: 'asc' } },
    });

    return progress;
  }

  async getUserItems(userId: string) {
    return this.prisma.userItem.findMany({
      where: { userId },
      include: {
        item: true,
      },
    });
  }

  async getUserAchievements(userId: string) {
    return this.prisma.userAchievement.findMany({
      where: { userId },
      include: {
        achievement: true,
      },
    });
  }

  async signIn(userId: string) {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    // 检查今日是否已签到
    const existing = await this.prisma.signIn.findFirst({
      where: {
        userId,
        signDate: {
          gte: today,
          lt: new Date(today.getTime() + 24 * 60 * 60 * 1000),
        },
      },
    });

    if (existing) {
      throw new BadRequestException('今日已签到');
    }

    // 查询昨天的签到记录
    const yesterday = new Date(today.getTime() - 24 * 60 * 60 * 1000);
    const yesterdaySignIn = await this.prisma.signIn.findFirst({
      where: {
        userId,
        signDate: {
          gte: yesterday,
          lt: today,
        },
      },
    });

    const streak = yesterdaySignIn ? yesterdaySignIn.streak + 1 : 1;

    // 签到奖励: 基础20金币，连续签到加成
    const goldReward = 20 + Math.min(streak, 7) * 5;

    const [signIn] = await this.prisma.$transaction([
      this.prisma.signIn.create({
        data: {
          userId,
          signDate: today,
          streak,
          reward: JSON.stringify({ gold: goldReward }),
        },
      }),
      this.prisma.user.update({
        where: { id: userId },
        data: { gold: { increment: goldReward } },
      }),
    ]);

    this.logger.log(`📅 签到: 连续${streak}天 | +${goldReward}金币`);

    return {
      streak,
      goldReward,
      signIn,
    };
  }

  /**
   * 增加经验值，自动升级
   */
  async addExp(userId: string, amount: number) {
    const user = await this.prisma.user.findUnique({
      where: { id: userId },
    });

    if (!user) throw new NotFoundException('用户不存在');

    let exp = user.exp + amount;
    let level = user.level;
    let leveledUp = false;

    while (exp >= expForLevel(level)) {
      exp -= expForLevel(level);
      level++;
      leveledUp = true;
    }

    await this.prisma.user.update({
      where: { id: userId },
      data: { exp, level },
    });

    if (leveledUp) {
      this.logger.log(`⬆️ [${user.nickname || user.username}] 升级! Lv.${user.level} → Lv.${level}`);
    }

    return { level, exp, leveledUp, expToNextLevel: expForLevel(level) };
  }

  /**
   * 消耗体力
   */
  async consumeEnergy(userId: string, amount: number): Promise<boolean> {
    const user = await this.prisma.user.findUnique({
      where: { id: userId },
    });

    if (!user) throw new NotFoundException('用户不存在');

    const currentEnergy = this.calculateCurrentEnergy(
      user.energy,
      user.maxEnergy,
      user.energyUpdatedAt,
    );

    if (currentEnergy < amount) {
      return false;
    }

    await this.prisma.user.update({
      where: { id: userId },
      data: {
        energy: currentEnergy - amount,
        energyUpdatedAt: new Date(),
      },
    });

    return true;
  }

  /**
   * 计算实时体力（每5分钟恢复1点）
   */
  private calculateCurrentEnergy(
    storedEnergy: number,
    maxEnergy: number,
    lastUpdated: Date,
  ): number {
    const elapsed = Date.now() - lastUpdated.getTime();
    const recovered = Math.floor(elapsed / (5 * 60 * 1000));
    return Math.min(storedEnergy + recovered, maxEnergy);
  }

  // ========== Admin Methods ==========

  async findAllUsers(query: AdminQueryUsersDto) {
    const { page = 1, pageSize = 20, search, role } = query;
    const where: any = {};

    if (search) {
      where.OR = [
        { username: { contains: search, mode: 'insensitive' } },
        { nickname: { contains: search, mode: 'insensitive' } },
        { email: { contains: search, mode: 'insensitive' } },
      ];
    }

    if (role) {
      where.role = role;
    }

    const [items, total] = await Promise.all([
      this.prisma.user.findMany({
        where,
        select: {
          id: true,
          username: true,
          nickname: true,
          email: true,
          phone: true,
          role: true,
          level: true,
          exp: true,
          gold: true,
          diamond: true,
          lastLoginAt: true,
          createdAt: true,
        },
        skip: (page - 1) * pageSize,
        take: pageSize,
        orderBy: { createdAt: 'desc' },
      }),
      this.prisma.user.count({ where }),
    ]);

    return new PaginatedResult(items, total, page, pageSize);
  }

  async getUserDetail(userId: string) {
    const user = await this.prisma.user.findUnique({
      where: { id: userId },
      include: {
        progress: {
          include: { stage: { select: { name: true, stageNumber: true } } },
        },
        achievements: {
          include: { achievement: true },
          where: { isCompleted: true },
        },
        answerRecords: {
          take: 50,
          orderBy: { createdAt: 'desc' },
        },
      },
    });

    if (!user) throw new NotFoundException('用户不存在');

    // 统计
    const stats = await this.prisma.userAnswerRecord.aggregate({
      where: { userId },
      _count: true,
      _avg: { timeSpent: true },
    });

    const correctCount = await this.prisma.userAnswerRecord.count({
      where: { userId, isCorrect: true },
    });

    return {
      ...this.sanitize(user),
      stats: {
        totalAnswered: stats._count,
        correctCount,
        accuracy: stats._count > 0 ? correctCount / stats._count : 0,
        avgTimeSpent: stats._avg.timeSpent || 0,
      },
    };
  }

  private sanitize(user: any) {
    const { passwordHash, ...rest } = user;
    return rest;
  }
}
