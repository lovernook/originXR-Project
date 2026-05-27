import { IsString, IsOptional, MaxLength, IsEnum } from 'class-validator';
import { ApiPropertyOptional } from '@nestjs/swagger';
import { PaginationDto } from '../../../common/dto';

export class UpdateProfileDto {
  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  @MaxLength(50)
  nickname?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  @MaxLength(500)
  avatar?: string;
}

export class AdminQueryUsersDto extends PaginationDto {
  @ApiPropertyOptional({ description: '搜索关键词' })
  @IsOptional()
  @IsString()
  search?: string;

  @ApiPropertyOptional({
    description: '角色筛选',
    enum: ['STUDENT', 'TEACHER', 'ADMIN', 'SUPER_ADMIN'],
  })
  @IsOptional()
  @IsString()
  role?: string;
}
