import { Controller, Get, Post, Put, Delete, Param, Body } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { KnowledgeService } from './knowledge.service';
import { EventGateway } from '../../common/websocket/event.gateway';
import { CreateSubjectDto, CreateChapterDto, CreateKnowledgePointDto } from './dto';

@ApiTags('知识点管理')
@Controller('admin/knowledge')
export class KnowledgeController {
  constructor(
    private knowledgeService: KnowledgeService,
    private eventGateway: EventGateway,
  ) {}

  @Get('tree')
  @ApiOperation({ summary: '知识点树' })
  getTree() { return this.knowledgeService.getKnowledgeTree(); }

  @Get('subjects')
  @ApiOperation({ summary: '学科列表' })
  findSubjects() { return this.knowledgeService.findAllSubjects(); }

  @Post('subjects')
  @ApiOperation({ summary: '创建学科' })
  async createSubject(@Body() dto: CreateSubjectDto) {
    const result = await this.knowledgeService.createSubject(dto);
    this.eventGateway.emitKnowledgeUpdated({ action: 'subject_created', id: result.id, name: result.name });
    return result;
  }

  @Put('subjects/:id')
  @ApiOperation({ summary: '更新学科' })
  async updateSubject(@Param('id') id: string, @Body() dto: Partial<CreateSubjectDto>) {
    const result = await this.knowledgeService.updateSubject(id, dto);
    this.eventGateway.emitKnowledgeUpdated({ action: 'subject_updated', id, name: result.name });
    return result;
  }

  @Delete('subjects/:id')
  @ApiOperation({ summary: '删除学科' })
  async removeSubject(@Param('id') id: string) {
    const result = await this.knowledgeService.removeSubject(id);
    this.eventGateway.emitKnowledgeUpdated({ action: 'subject_deleted', id });
    return result;
  }

  @Get('chapters/:subjectId')
  @ApiOperation({ summary: '章节列表' })
  findChapters(@Param('subjectId') subjectId: string) {
    return this.knowledgeService.findChaptersBySubject(subjectId);
  }

  @Post('chapters')
  @ApiOperation({ summary: '创建章节' })
  async createChapter(@Body() dto: CreateChapterDto) {
    const result = await this.knowledgeService.createChapter(dto);
    this.eventGateway.emitKnowledgeUpdated({ action: 'chapter_created', id: result.id, name: result.name });
    return result;
  }

  @Put('chapters/:id')
  @ApiOperation({ summary: '更新章节' })
  async updateChapter(@Param('id') id: string, @Body() dto: Partial<CreateChapterDto>) {
    const result = await this.knowledgeService.updateChapter(id, dto);
    this.eventGateway.emitKnowledgeUpdated({ action: 'chapter_updated', id, name: result.name });
    return result;
  }

  @Delete('chapters/:id')
  @ApiOperation({ summary: '删除章节' })
  async removeChapter(@Param('id') id: string) {
    const result = await this.knowledgeService.removeChapter(id);
    this.eventGateway.emitKnowledgeUpdated({ action: 'chapter_deleted', id });
    return result;
  }

  @Get('points/:chapterId')
  @ApiOperation({ summary: '知识点列表' })
  findPoints(@Param('chapterId') chapterId: string) {
    return this.knowledgeService.findKnowledgePointsByChapter(chapterId);
  }

  @Post('points')
  @ApiOperation({ summary: '创建知识点' })
  async createPoint(@Body() dto: CreateKnowledgePointDto) {
    const result = await this.knowledgeService.createKnowledgePoint(dto);
    this.eventGateway.emitKnowledgeUpdated({ action: 'point_created', id: result.id, name: result.name });
    return result;
  }

  @Put('points/:id')
  @ApiOperation({ summary: '更新知识点' })
  async updatePoint(@Param('id') id: string, @Body() dto: Partial<CreateKnowledgePointDto>) {
    const result = await this.knowledgeService.updateKnowledgePoint(id, dto);
    this.eventGateway.emitKnowledgeUpdated({ action: 'point_updated', id, name: result.name });
    return result;
  }

  @Delete('points/:id')
  @ApiOperation({ summary: '删除知识点' })
  async removePoint(@Param('id') id: string) {
    const result = await this.knowledgeService.removeKnowledgePoint(id);
    this.eventGateway.emitKnowledgeUpdated({ action: 'point_deleted', id });
    return result;
  }
}
