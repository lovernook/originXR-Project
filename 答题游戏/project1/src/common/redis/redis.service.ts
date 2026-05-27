import { Injectable, OnModuleInit, OnModuleDestroy } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import Redis from 'ioredis';

@Injectable()
export class RedisService implements OnModuleInit, OnModuleDestroy {
  private client: Redis;

  constructor(private configService: ConfigService) {}

  onModuleInit() {
    this.client = new Redis({
      host: this.configService.get('REDIS_HOST', '192.168.126.140'),
      port: this.configService.get<number>('REDIS_PORT', 6379),
      password: this.configService.get('REDIS_PASSWORD', '122699'),
      db: this.configService.get<number>('REDIS_DB', 0),
    });
  }

  onModuleDestroy() {
    this.client?.disconnect();
  }

  getClient(): Redis {
    return this.client;
  }

  // 便捷方法
  async get(key: string): Promise<string | null> {
    return this.client.get(key);
  }

  async set(key: string, value: string, ttl?: number): Promise<void> {
    if (ttl) {
      await this.client.set(key, value, 'EX', ttl);
    } else {
      await this.client.set(key, value);
    }
  }

  async del(key: string): Promise<void> {
    await this.client.del(key);
  }

  async incr(key: string): Promise<number> {
    return this.client.incr(key);
  }

  async decr(key: string): Promise<number> {
    return this.client.decr(key);
  }

  // Sorted Set 用于排行榜
  async zadd(key: string, score: number, member: string): Promise<void> {
    await this.client.zadd(key, score, member);
  }

  async zrevrange(
    key: string,
    start: number,
    stop: number,
  ): Promise<string[]> {
    return this.client.zrevrange(key, start, stop, 'WITHSCORES');
  }

  async zrevrank(key: string, member: string): Promise<number | null> {
    return this.client.zrevrank(key, member);
  }

  async zscore(key: string, member: string): Promise<string | null> {
    return this.client.zscore(key, member);
  }

  async zcard(key: string): Promise<number> {
    return this.client.zcard(key);
  }
}
