import {
  IsString,
  IsOptional,
  IsEnum,
  IsInt,
  IsArray,
  IsBoolean,
  ValidateNested,
  Min,
  Max,
  MinLength,
} from 'class-validator';
import { Type } from 'class-transformer';
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import { PaginationDto } from '../../../common/dto';

// ========== Question DTOs ==========

export class CreateOptionDto {
  @ApiProperty({ example: 'A' })
  @IsString()
  optionKey: string;

  @ApiProperty({ example: '选项内容' })
  @IsString()
  content: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  mediaUrl?: string;

  @ApiProperty({ example: false })
  @IsBoolean()
  isCorrect: boolean;

  @ApiPropertyOptional()
  @IsOptional()
  @IsInt()
  sortOrder?: number;
}

export class CreateQuestionDto {
  @ApiProperty({ enum: ['SINGLE_CHOICE', 'MULTI_CHOICE', 'TRUE_FALSE', 'FILL_BLANK', 'SORTING', 'MATCHING'] })
  @IsEnum(['SINGLE_CHOICE', 'MULTI_CHOICE', 'TRUE_FALSE', 'FILL_BLANK', 'SORTING', 'MATCHING'])
  type: string;

  @ApiProperty({ description: '题目内容' })
  @IsString()
  @MinLength(1)
  content: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  mediaUrl?: string;

  @ApiProperty({ description: '难度 1-5', default: 3 })
  @IsInt()
  @Min(1)
  @Max(5)
  difficulty: number;

  @ApiPropertyOptional({ description: '限时秒数', default: 30 })
  @IsOptional()
  @IsInt()
  @Min(5)
  timeLimit?: number;

  @ApiPropertyOptional({ description: '解析' })
  @IsOptional()
  @IsString()
  explanation?: string;

  @ApiProperty({ description: '选项列表', type: [CreateOptionDto] })
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => CreateOptionDto)
  options: CreateOptionDto[];

  @ApiPropertyOptional({ description: '知识点ID列表' })
  @IsOptional()
  @IsArray()
  @IsString({ each: true })
  knowledgePointIds?: string[];

  @ApiPropertyOptional({ description: '标签列表' })
  @IsOptional()
  @IsArray()
  @IsString({ each: true })
  tags?: string[];

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  source?: string;
}

export class UpdateQuestionDto {
  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  content?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  mediaUrl?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsInt()
  @Min(1)
  @Max(5)
  difficulty?: number;

  @ApiPropertyOptional()
  @IsOptional()
  @IsInt()
  timeLimit?: number;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  explanation?: string;

  @ApiPropertyOptional({ type: [CreateOptionDto] })
  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => CreateOptionDto)
  options?: CreateOptionDto[];

  @ApiPropertyOptional()
  @IsOptional()
  @IsArray()
  @IsString({ each: true })
  knowledgePointIds?: string[];

  @ApiPropertyOptional()
  @IsOptional()
  @IsArray()
  @IsString({ each: true })
  tags?: string[];
}

export class QueryQuestionDto extends PaginationDto {
  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  search?: string;

  @ApiPropertyOptional({ enum: ['SINGLE_CHOICE', 'MULTI_CHOICE', 'TRUE_FALSE', 'FILL_BLANK', 'SORTING', 'MATCHING'] })
  @IsOptional()
  @IsString()
  type?: string;

  @ApiPropertyOptional({ enum: ['DRAFT', 'PENDING_REVIEW', 'PUBLISHED', 'DEPRECATED'] })
  @IsOptional()
  @IsString()
  status?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @Type(() => Number)
  @IsInt()
  difficulty?: number;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  knowledgePointId?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  tag?: string;
}

export class ReviewQuestionDto {
  @ApiProperty({ enum: ['PUBLISHED', 'DEPRECATED'] })
  @IsEnum(['PUBLISHED', 'DEPRECATED'])
  status: string;
}

// ========== Knowledge DTOs ==========

export class CreateSubjectDto {
  @ApiProperty()
  @IsString()
  name: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  description?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  icon?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsInt()
  sortOrder?: number;
}

export class CreateChapterDto {
  @ApiProperty()
  @IsString()
  subjectId: string;

  @ApiProperty()
  @IsString()
  name: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  description?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsInt()
  sortOrder?: number;
}

export class CreateKnowledgePointDto {
  @ApiProperty()
  @IsString()
  chapterId: string;

  @ApiProperty()
  @IsString()
  name: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  description?: string;

  @ApiPropertyOptional()
  @IsOptional()
  @IsInt()
  @Min(1)
  @Max(5)
  difficulty?: number;

  @ApiPropertyOptional()
  @IsOptional()
  @IsString()
  parentId?: string;

  @ApiPropertyOptional({ default: 80 })
  @IsOptional()
  @IsInt()
  masteryThreshold?: number;

  @ApiPropertyOptional()
  @IsOptional()
  @IsInt()
  sortOrder?: number;
}

export class SmartPickDto {
  @ApiProperty({ description: '知识点ID列表' })
  @IsArray()
  @IsString({ each: true })
  knowledgePointIds: string[];

  @ApiPropertyOptional({ description: '难度范围最小值', default: 1 })
  @IsOptional()
  @IsInt()
  @Min(1)
  difficultyMin?: number;

  @ApiPropertyOptional({ description: '难度范围最大值', default: 5 })
  @IsOptional()
  @IsInt()
  @Max(5)
  difficultyMax?: number;

  @ApiProperty({ description: '抽取数量', default: 10 })
  @IsInt()
  @Min(1)
  @Max(50)
  count: number;
}
