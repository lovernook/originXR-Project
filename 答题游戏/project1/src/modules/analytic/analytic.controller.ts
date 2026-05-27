import { Controller, Get } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { AnalyticService } from './analytic.service';

@ApiTags('数据分析[管理]')
@Controller('admin/analytics')
export class AnalyticController {
  constructor(private analyticService: AnalyticService) {}

  @Get('overview')
  @ApiOperation({ summary: '全局概览' })
  getOverview() { return this.analyticService.getOverview(); }

  @Get('questions')
  @ApiOperation({ summary: '题目分析' })
  getQuestions() { return this.analyticService.getQuestionAnalytics(); }

  @Get('students')
  @ApiOperation({ summary: '学员分析' })
  getStudents() { return this.analyticService.getStudentAnalytics(); }

  @Get('stages')
  @ApiOperation({ summary: '关卡分析' })
  getStages() { return this.analyticService.getStageAnalytics(); }
}
