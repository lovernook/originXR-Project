import {
  IsString,
  IsEmail,
  IsOptional,
  MinLength,
  MaxLength,
  IsEnum,
} from 'class-validator';
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class RegisterDto {
  @ApiProperty({ description: '用户名', example: 'player001' })
  @IsString()
  @MinLength(3)
  @MaxLength(50)
  username: string;

  @ApiProperty({ description: '密码', example: '123456' })
  @IsString()
  @MinLength(6)
  @MaxLength(100)
  password: string;

  @ApiPropertyOptional({ description: '邮箱' })
  @IsOptional()
  @IsEmail()
  email?: string;

  @ApiPropertyOptional({ description: '手机号' })
  @IsOptional()
  @IsString()
  phone?: string;

  @ApiPropertyOptional({ description: '昵称' })
  @IsOptional()
  @IsString()
  @MaxLength(50)
  nickname?: string;

  @ApiPropertyOptional({
    description: '角色',
    enum: ['STUDENT', 'TEACHER', 'ADMIN', 'SUPER_ADMIN'],
  })
  @IsOptional()
  @IsEnum(['STUDENT', 'TEACHER', 'ADMIN', 'SUPER_ADMIN'])
  role?: string;
}

export class LoginDto {
  @ApiProperty({ description: '账号(用户名/邮箱/手机号)', example: 'admin' })
  @IsString()
  account: string;

  @ApiProperty({ description: '密码', example: '123456' })
  @IsString()
  password: string;
}

export class RefreshTokenDto {
  @ApiProperty({ description: 'Refresh Token' })
  @IsString()
  refreshToken: string;
}
