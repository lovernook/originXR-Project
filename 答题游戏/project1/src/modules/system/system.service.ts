import { Injectable, NotFoundException, BadRequestException } from '@nestjs/common';
import { PrismaService } from '../../common/prisma/prisma.service';
import { PaginatedResult } from '../../common/dto';

@Injectable()
export class SystemService {
  constructor(private prisma: PrismaService) {}

  // ===== System Config =====
  async getConfigs() {
    return this.prisma.systemConfig.findMany({ orderBy: { configKey: 'asc' } });
  }

  async upsertConfig(key: string, value: any, description?: string, updatedBy?: string) {
    return this.prisma.systemConfig.upsert({
      where: { configKey: key },
      create: { configKey: key, configValue: value, description, updatedBy },
      update: { configValue: value, description, updatedBy },
    });
  }

  // ===== Announcements =====
  async getAnnouncements(page = 1, pageSize = 20) {
    const [items, total] = await Promise.all([
      this.prisma.announcement.findMany({
        skip: (page - 1) * pageSize, take: pageSize,
        orderBy: [{ priority: 'desc' }, { createdAt: 'desc' }],
      }),
      this.prisma.announcement.count(),
    ]);
    return new PaginatedResult(items, total, page, pageSize);
  }

  async createAnnouncement(data: any) {
    return this.prisma.announcement.create({ data });
  }

  async updateAnnouncement(id: string, data: any) {
    return this.prisma.announcement.update({ where: { id }, data });
  }

  async deleteAnnouncement(id: string) {
    await this.prisma.announcement.delete({ where: { id } });
    return { deleted: true };
  }

  // ===== Items (道具) =====
  async getItems() {
    return this.prisma.item.findMany({ orderBy: { createdAt: 'desc' } });
  }

  async createItem(data: any) {
    return this.prisma.item.create({ data });
  }

  async updateItem(id: string, data: any) {
    return this.prisma.item.update({ where: { id }, data });
  }

  async deleteItem(id: string) {
    await this.prisma.item.update({ where: { id }, data: { isActive: false } });
    return { deleted: true };
  }

  // ===== Activities =====
  async getActivities() {
    return this.prisma.activity.findMany({ orderBy: { startTime: 'desc' } });
  }

  async createActivity(data: any) {
    return this.prisma.activity.create({ data });
  }

  async updateActivity(id: string, data: any) {
    return this.prisma.activity.update({ where: { id }, data });
  }

  // ===== Redemption Codes =====
  async getRedemptionCodes() {
    return this.prisma.redemptionCode.findMany({ orderBy: { createdAt: 'desc' } });
  }

  async createRedemptionCode(data: any) {
    return this.prisma.redemptionCode.create({ data });
  }

  async redeemCode(userId: string, code: string) {
    const redemption = await this.prisma.redemptionCode.findUnique({ where: { code } });
    if (!redemption) throw new NotFoundException('兑换码不存在');
    if (!redemption.isActive) throw new BadRequestException('兑换码已失效');
    if (redemption.expiresAt && redemption.expiresAt < new Date()) throw new BadRequestException('兑换码已过期');
    if (redemption.usedCount >= redemption.maxUses) throw new BadRequestException('兑换码已用完');

    // 检查是否已使用
    const used = await this.prisma.redemptionLog.findUnique({
      where: { codeId_userId: { codeId: redemption.id, userId } },
    });
    if (used) throw new BadRequestException('已使用过此兑换码');

    await this.prisma.$transaction([
      this.prisma.redemptionLog.create({
        data: { codeId: redemption.id, userId },
      }),
      this.prisma.redemptionCode.update({
        where: { id: redemption.id },
        data: { usedCount: { increment: 1 } },
      }),
    ]);

    return { redeemed: true, rewardType: redemption.rewardType, amount: redemption.rewardAmount };
  }

  // ===== Operation Logs =====
  async getOperationLogs(page = 1, pageSize = 50) {
    const [items, total] = await Promise.all([
      this.prisma.operationLog.findMany({
        skip: (page - 1) * pageSize, take: pageSize,
        orderBy: { createdAt: 'desc' },
      }),
      this.prisma.operationLog.count(),
    ]);
    return new PaginatedResult(items, total, page, pageSize);
  }

  async createLog(data: { userId?: string; action: string; module: string; detail?: string; ip?: string }) {
    return this.prisma.operationLog.create({ data });
  }
}
