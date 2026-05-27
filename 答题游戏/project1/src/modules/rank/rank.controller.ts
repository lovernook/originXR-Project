import { Controller, Get, Query } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { RankService } from './rank.service';
import { CurrentUser } from '../../common/decorators';

@ApiTags('排行榜')
@Controller('rank')
export class RankController {
  constructor(private rankService: RankService) {}

  @Get('global')
  @ApiOperation({ summary: '全服排行' })
  getGlobal(@Query('page') page = 1, @Query('pageSize') pageSize = 20) {
    return this.rankService.getGlobalRank(+page, +pageSize);
  }

  @Get('me')
  @ApiOperation({ summary: '我的排名' })
  getMyRank(@CurrentUser('id') userId: string) {
    return this.rankService.getUserRank(userId);
  }

  @Get('daily')
  @ApiOperation({ summary: '每日排行' })
  getDaily(@Query('page') page = 1, @Query('pageSize') pageSize = 20) {
    return this.rankService.getDailyRank(+page, +pageSize);
  }
}
