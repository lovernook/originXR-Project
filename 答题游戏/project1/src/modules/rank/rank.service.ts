import { Injectable } from '@nestjs/common';
import { RedisService } from '../../common/redis/redis.service';
import { PrismaService } from '../../common/prisma/prisma.service';

@Injectable()
export class RankService {
  constructor(
    private redis: RedisService,
    private prisma: PrismaService,
  ) {}

  async getGlobalRank(page = 1, pageSize = 20) {
    const start = (page - 1) * pageSize;
    const end = start + pageSize - 1;
    const data = await this.redis.zrevrange('rank:global:score', start, end);
    const total = await this.redis.zcard('rank:global:score');

    const items: any[] = [];
    for (let i = 0; i < data.length; i += 2) {
      const userId = data[i];
      const score = parseInt(data[i + 1], 10);
      const user = await this.prisma.user.findUnique({
        where: { id: userId },
        select: { id: true, username: true, nickname: true, avatar: true, level: true },
      });
      if (user) {
        items.push({ rank: start + items.length + 1, ...user, score });
      }
    }

    return { items, total, page, pageSize };
  }

  async getUserRank(userId: string) {
    const rank = await this.redis.zrevrank('rank:global:score', userId);
    const score = await this.redis.zscore('rank:global:score', userId);
    return {
      rank: rank !== null ? rank + 1 : null,
      score: score ? parseInt(score, 10) : 0,
    };
  }

  async getDailyRank(page = 1, pageSize = 20) {
    const today = new Date().toISOString().slice(0, 10);
    const key = `rank:daily:${today}`;
    const start = (page - 1) * pageSize;
    const end = start + pageSize - 1;
    const data = await this.redis.zrevrange(key, start, end);

    const items: any[] = [];
    for (let i = 0; i < data.length; i += 2) {
      const userId = data[i];
      const score = parseInt(data[i + 1], 10);
      const user = await this.prisma.user.findUnique({
        where: { id: userId },
        select: { id: true, username: true, nickname: true, avatar: true, level: true },
      });
      if (user) items.push({ rank: start + items.length + 1, ...user, score });
    }

    return { items, total: await this.redis.zcard(key), page, pageSize };
  }
}
