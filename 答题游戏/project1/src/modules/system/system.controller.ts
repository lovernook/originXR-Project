import { Controller, Get, Post, Put, Delete, Param, Body, Query } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { SystemService } from './system.service';
import { EventGateway } from '../../common/websocket/event.gateway';

@ApiTags('系统管理[管理]')
@Controller('admin/system')
export class SystemController {
  constructor(
    private systemService: SystemService,
    private eventGateway: EventGateway,
  ) {}

  // Config
  @Get('configs')
  @ApiOperation({ summary: '系统配置列表' })
  getConfigs() { return this.systemService.getConfigs(); }

  @Post('configs')
  @ApiOperation({ summary: '更新系统配置' })
  async upsertConfig(@Body() body: { key: string; value: any; description?: string }) {
    const result = await this.systemService.upsertConfig(body.key, body.value, body.description);
    this.eventGateway.emitConfigChanged({ key: body.key, value: body.value });
    return result;
  }

  // Announcements
  @Get('announcements')
  @ApiOperation({ summary: '公告列表' })
  getAnnouncements(@Query('page') page = 1, @Query('pageSize') pageSize = 20) {
    return this.systemService.getAnnouncements(+page, +pageSize);
  }

  @Post('announcements')
  @ApiOperation({ summary: '创建公告' })
  async createAnnouncement(@Body() body: any) {
    const result = await this.systemService.createAnnouncement(body);
    this.eventGateway.emitAnnouncementChanged('created', { id: result.id, title: result.title });
    return result;
  }

  @Put('announcements/:id')
  @ApiOperation({ summary: '更新公告' })
  async updateAnnouncement(@Param('id') id: string, @Body() body: any) {
    const result = await this.systemService.updateAnnouncement(id, body);
    this.eventGateway.emitAnnouncementChanged('updated', { id, title: result.title });
    return result;
  }

  @Delete('announcements/:id')
  @ApiOperation({ summary: '删除公告' })
  async deleteAnnouncement(@Param('id') id: string) {
    await this.systemService.deleteAnnouncement(id);
    this.eventGateway.emitAnnouncementChanged('deleted', { id });
    return { deleted: true };
  }

  // Items
  @Get('items')
  @ApiOperation({ summary: '道具列表' })
  getItems() { return this.systemService.getItems(); }

  @Post('items')
  @ApiOperation({ summary: '创建道具' })
  async createItem(@Body() body: any) {
    const result = await this.systemService.createItem(body);
    this.eventGateway.emitItemChanged('created', { id: result.id, name: result.name });
    return result;
  }

  @Put('items/:id')
  @ApiOperation({ summary: '更新道具' })
  async updateItem(@Param('id') id: string, @Body() body: any) {
    const result = await this.systemService.updateItem(id, body);
    this.eventGateway.emitItemChanged('updated', { id, name: result.name });
    return result;
  }

  @Delete('items/:id')
  @ApiOperation({ summary: '删除道具' })
  async deleteItem(@Param('id') id: string) {
    await this.systemService.deleteItem(id);
    this.eventGateway.emitItemChanged('deleted', { id });
    return { deleted: true };
  }

  // Activities
  @Get('activities')
  @ApiOperation({ summary: '活动列表' })
  getActivities() { return this.systemService.getActivities(); }

  @Post('activities')
  @ApiOperation({ summary: '创建活动' })
  async createActivity(@Body() body: any) {
    const result = await this.systemService.createActivity(body);
    this.eventGateway.emitActivityChanged('created', { id: result.id, title: result.title });
    return result;
  }

  @Put('activities/:id')
  @ApiOperation({ summary: '更新活动' })
  async updateActivity(@Param('id') id: string, @Body() body: any) {
    const result = await this.systemService.updateActivity(id, body);
    this.eventGateway.emitActivityChanged('updated', { id, title: result.title });
    return result;
  }

  // Redemption Codes
  @Get('redemption-codes')
  @ApiOperation({ summary: '礼包码列表' })
  getCodes() { return this.systemService.getRedemptionCodes(); }

  @Post('redemption-codes')
  @ApiOperation({ summary: '创建礼包码' })
  createCode(@Body() body: any) { return this.systemService.createRedemptionCode(body); }

  // Logs
  @Get('logs')
  @ApiOperation({ summary: '操作日志' })
  getLogs(@Query('page') page = 1, @Query('pageSize') pageSize = 50) {
    return this.systemService.getOperationLogs(+page, +pageSize);
  }
}
