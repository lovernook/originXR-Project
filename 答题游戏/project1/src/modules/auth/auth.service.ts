import {
  Injectable,
  UnauthorizedException,
  ConflictException,
  Logger,
} from '@nestjs/common';
import { JwtService } from '@nestjs/jwt';
import * as bcrypt from 'bcryptjs';
import { PrismaService } from '../../common/prisma/prisma.service';
import { RedisService } from '../../common/redis/redis.service';
import { RegisterDto, LoginDto } from './dto';

@Injectable()
export class AuthService {
  private readonly logger = new Logger(AuthService.name);

  constructor(
    private prisma: PrismaService,
    private jwtService: JwtService,
    private redis: RedisService,
  ) {}

  async register(dto: RegisterDto) {
    const existing = await this.prisma.user.findFirst({
      where: {
        OR: [
          { username: dto.username },
          ...(dto.email ? [{ email: dto.email }] : []),
          ...(dto.phone ? [{ phone: dto.phone }] : []),
        ],
      },
    });

    if (existing) {
      this.logger.warn(`注册失败 - 用户名已存在: ${dto.username}`);
      throw new ConflictException('用户名、邮箱或手机号已存在');
    }

    const passwordHash = await bcrypt.hash(dto.password, 10);

    const user = await this.prisma.user.create({
      data: {
        username: dto.username,
        email: dto.email,
        phone: dto.phone,
        passwordHash,
        nickname: dto.nickname || dto.username,
        role: (dto.role as any) || 'STUDENT',
      },
    });

    const tokens = await this.generateTokens(user.id, user.role);
    this.logger.log(`🆕 新用户注册: ${user.username} (${user.id})`);

    return {
      user: this.sanitizeUser(user),
      ...tokens,
    };
  }

  async login(dto: LoginDto) {
    const user = await this.prisma.user.findFirst({
      where: {
        OR: [
          { username: dto.account },
          { email: dto.account },
          { phone: dto.account },
        ],
      },
    });

    if (!user) {
      this.logger.warn(`登录失败 - 账号不存在: ${dto.account}`);
      throw new UnauthorizedException('账号或密码错误');
    }

    const isPasswordValid = await bcrypt.compare(
      dto.password,
      user.passwordHash,
    );
    if (!isPasswordValid) {
      this.logger.warn(`登录失败 - 密码错误: ${user.username}`);
      throw new UnauthorizedException('账号或密码错误');
    }

    await this.prisma.user.update({
      where: { id: user.id },
      data: { lastLoginAt: new Date() },
    });

    const tokens = await this.generateTokens(user.id, user.role);
    this.logger.log(`🔑 用户登录: ${user.username} (Lv.${user.level})`);

    return {
      user: this.sanitizeUser(user),
      ...tokens,
    };
  }

  async refreshToken(refreshToken: string) {
    const stored = await this.prisma.refreshToken.findUnique({
      where: { token: refreshToken },
      include: { user: true },
    });

    if (!stored || stored.expiresAt < new Date()) {
      throw new UnauthorizedException('Refresh token已过期，请重新登录');
    }

    await this.prisma.refreshToken.delete({ where: { id: stored.id } });

    const tokens = await this.generateTokens(
      stored.user.id,
      stored.user.role,
    );

    return {
      user: this.sanitizeUser(stored.user),
      ...tokens,
    };
  }

  private async generateTokens(userId: string, role: string) {
    const payload = { sub: userId, role };

    const accessToken = this.jwtService.sign(payload);
    const refreshToken = this.jwtService.sign(payload, { expiresIn: '30d' });

    await this.prisma.refreshToken.create({
      data: {
        userId,
        token: refreshToken,
        expiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
      },
    });

    return { accessToken, refreshToken };
  }

  private sanitizeUser(user: any) {
    const { passwordHash, ...rest } = user;
    return rest;
  }
}
