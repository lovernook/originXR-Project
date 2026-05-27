import { IsString, IsOptional, IsInt, Min, Max, IsArray, ValidateNested } from 'class-validator';
import { Type } from 'class-transformer';
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class SubmitAnswerDto {
  @ApiProperty()
  @IsString()
  questionId: string;

  @ApiProperty({ description: '选中的答案(A/B/C/D)' })
  @IsString()
  selectedAnswer: string;

  @ApiProperty({ description: '作答耗时(秒)' })
  @IsInt()
  @Min(0)
  timeSpent: number;
}

export class CreateStageDto {
  @ApiProperty()
  @IsString()
  name: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  description?: string;

  @ApiProperty()
  @IsInt()
  stageNumber: number;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  chapterId?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  bossName?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  bossModelId?: string;

  @ApiPropertyOptional({ default: 1000 })
  @IsOptional()
  @IsInt()
  bossHp?: number;

  @ApiPropertyOptional({ default: 10 })
  @IsOptional()
  @IsInt()
  questionCount?: number;

  @ApiPropertyOptional({ default: 60 })
  @IsOptional()
  @IsInt()
  passScore?: number;

  @ApiPropertyOptional({ default: 3 })
  @IsOptional()
  @IsInt()
  maxLives?: number;

  @ApiPropertyOptional({ default: 100 })
  @IsOptional()
  @IsInt()
  expReward?: number;

  @ApiPropertyOptional({ default: 50 })
  @IsOptional()
  @IsInt()
  goldReward?: number;

  @ApiPropertyOptional({ default: 0 })
  @IsOptional()
  @IsInt()
  diamondReward?: number;

  @ApiPropertyOptional({ default: 10 })
  @IsOptional()
  @IsInt()
  energyCost?: number;

  @ApiPropertyOptional({ default: 1 })
  @IsOptional()
  @IsInt()
  unlockLevel?: number;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  unlockStageId?: string;
}

export class UpdateStageDto extends CreateStageDto {}
