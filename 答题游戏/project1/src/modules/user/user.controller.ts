import {
  Controller, Get, Patch, Post, Param, Body, Query,
} from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';
import { UserService } from './user.service';
import { CurrentUser } from '../../common/decorators';
import { UpdateProfileDto, AdminQueryUsersDto } from './dto';

@ApiTags('用户')
@Controller('users')
export class UserController {
  constructor(private userService: UserService) {}

  @Get('me')
  @ApiOperation({ summary: '获取当前用户信息' })
  getProfile(@CurrentUser('id') userId: string) {
    return this.userService.getProfile(userId);
  }

  @Patch('me/profile')
  @ApiOperation({ summary: '更新个人资料' })
  updateProfile(
    @CurrentUser('id') userId: string,
    @Body() dto: UpdateProfileDto,
  ) {
    return this.userService.updateProfile(userId, dto);
  }

  @Get(':id/progress')
  @ApiOperation({ summary: '获取学习进度' })
  getProgress(@Param('id') id: string) {
    return this.userService.getUserProgress(id);
  }

  @Get(':id/items')
  @ApiOperation({ summary: '获取背包物品' })
  getItems(@Param('id') id: string) {
    return this.userService.getUserItems(id);
  }

  @Get(':id/achievements')
  @ApiOperation({ summary: '获取成就列表' })
  getAchievements(@Param('id') id: string) {
    return this.userService.getUserAchievements(id);
  }

  @Post('sign-in')
  @ApiOperation({ summary: '每日签到' })
  signIn(@CurrentUser('id') userId: string) {
    return this.userService.signIn(userId);
  }

  @Get()
  @ApiOperation({ summary: '[管理]学员列表' })
  findAll(@Query() query: AdminQueryUsersDto) {
    return this.userService.findAllUsers(query);
  }

  @Get(':id/detail')
  @ApiOperation({ summary: '[管理]学员详情' })
  getDetail(@Param('id') id: string) {
    return this.userService.getUserDetail(id);
  }
}
