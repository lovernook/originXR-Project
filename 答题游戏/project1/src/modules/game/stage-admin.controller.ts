import { Controller, Get, Post, Put, Delete, Param, Body } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { GameService } from './game.service';
import { EventGateway } from '../../common/websocket/event.gateway';
import { CreateStageDto } from './dto';

@ApiTags('关卡配置[管理]')
@Controller('admin/stages')
export class StageAdminController {
  constructor(
    private gameService: GameService,
    private eventGateway: EventGateway,
  ) {}

  @Get()
  @ApiOperation({ summary: '所有关卡' })
  findAll() { return this.gameService.getAllStagesAdmin(); }

  @Post()
  @ApiOperation({ summary: '创建关卡' })
  async create(@Body() dto: CreateStageDto) {
    const result = await this.gameService.createStage(dto);
    this.eventGateway.emitStageCreated({ id: result.id, name: result.name, stageNumber: result.stageNumber });
    return result;
  }

  @Put(':id')
  @ApiOperation({ summary: '更新关卡' })
  async update(@Param('id') id: string, @Body() dto: Partial<CreateStageDto>) {
    const result = await this.gameService.updateStage(id, dto);
    this.eventGateway.emitStageUpdated({ id, name: result.name, stageNumber: result.stageNumber });
    return result;
  }

  @Delete(':id')
  @ApiOperation({ summary: '删除关卡' })
  async remove(@Param('id') id: string) {
    await this.gameService.deleteStage(id);
    this.eventGateway.emitStageDeleted({ id });
    return { deleted: true };
  }
}
