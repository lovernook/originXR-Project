import { Controller, Get, Post, Param, Body } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { GameService } from './game.service';
import { CurrentUser } from '../../common/decorators';
import { SubmitAnswerDto } from './dto';

@ApiTags('游戏')
@Controller('game')
export class GameController {
  constructor(private gameService: GameService) {}

  @Get('stages')
  @ApiOperation({ summary: '关卡列表' })
  getStages(@CurrentUser('id') userId: string) {
    return this.gameService.getStages(userId);
  }

  @Post('stages/:id/start')
  @ApiOperation({ summary: '开始关卡' })
  startStage(@CurrentUser('id') userId: string, @Param('id') id: string) {
    return this.gameService.startStage(userId, id);
  }

  @Post('stages/:id/answer')
  @ApiOperation({ summary: '提交答案' })
  submitAnswer(
    @CurrentUser('id') userId: string,
    @Param('id') id: string,
    @Body() dto: SubmitAnswerDto,
  ) {
    return this.gameService.submitAnswer(userId, id, dto);
  }

  @Get('daily-challenge')
  @ApiOperation({ summary: '每日挑战' })
  getDailyChallenge(@CurrentUser('id') userId: string) {
    return this.gameService.getDailyChallenge(userId);
  }

  @Post('tower/start')
  @ApiOperation({ summary: '开始爬塔' })
  startTower(@CurrentUser('id') userId: string) {
    return this.gameService.startTower(userId);
  }
}
