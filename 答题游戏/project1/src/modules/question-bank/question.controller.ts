import { Controller, Get, Post, Put, Delete, Param, Body, Query } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { QuestionService } from './question.service';
import { EventGateway } from '../../common/websocket/event.gateway';
import { CurrentUser } from '../../common/decorators';
import {
  CreateQuestionDto, UpdateQuestionDto, QueryQuestionDto,
  ReviewQuestionDto, SmartPickDto,
} from './dto';

@ApiTags('题库管理')
@Controller('admin/questions')
export class QuestionController {
  constructor(
    private questionService: QuestionService,
    private eventGateway: EventGateway,
  ) {}

  @Post()
  @ApiOperation({ summary: '创建题目' })
  async create(
    @Body() dto: CreateQuestionDto,
    @CurrentUser('username') username: string,
  ) {
    const result = await this.questionService.create(dto, username);
    this.eventGateway.emitQuestionCreated({ id: result.id, type: result.type, content: result.content.slice(0, 50) });
    return result;
  }

  @Get()
  @ApiOperation({ summary: '题目列表' })
  findAll(@Query() query: QueryQuestionDto) {
    return this.questionService.findAll(query);
  }

  @Get(':id')
  @ApiOperation({ summary: '题目详情' })
  findOne(@Param('id') id: string) {
    return this.questionService.findOne(id);
  }

  @Put(':id')
  @ApiOperation({ summary: '更新题目' })
  async update(
    @Param('id') id: string,
    @Body() dto: UpdateQuestionDto,
    @CurrentUser('username') username: string,
  ) {
    const result = await this.questionService.update(id, dto, username);
    this.eventGateway.emitQuestionUpdated({ id, type: result.type, content: result.content.slice(0, 50) });
    return result;
  }

  @Delete(':id')
  @ApiOperation({ summary: '删除题目' })
  async remove(@Param('id') id: string) {
    await this.questionService.remove(id);
    this.eventGateway.emitQuestionDeleted({ id });
    return { deleted: true };
  }

  @Post(':id/review')
  @ApiOperation({ summary: '审核题目' })
  async review(
    @Param('id') id: string,
    @Body() dto: ReviewQuestionDto,
    @CurrentUser('username') username: string,
  ) {
    const result = await this.questionService.review(id, dto, username);
    this.eventGateway.emitQuestionUpdated({ id, status: dto.status });
    return result;
  }

  @Post(':id/submit-review')
  @ApiOperation({ summary: '提交审核' })
  async submitForReview(@Param('id') id: string) {
    const result = await this.questionService.submitForReview(id);
    this.eventGateway.emitQuestionUpdated({ id, status: 'PENDING_REVIEW' });
    return result;
  }

  @Post('smart-pick')
  @ApiOperation({ summary: '智能组卷' })
  smartPick(@Body() dto: SmartPickDto) {
    return this.questionService.smartPick(dto);
  }

  @Get(':id/stats')
  @ApiOperation({ summary: '题目统计' })
  getStats(@Param('id') id: string) {
    return this.questionService.getQuestionStats(id);
  }
}
