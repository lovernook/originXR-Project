import { Controller, Get } from '@nestjs/common';
import { ApiTags, ApiOperation } from '@nestjs/swagger';

@ApiTags('系统')
@Controller('health')
export class HealthController {
  @Get()
  @ApiOperation({ summary: '健康检查（公开）' })
  check() {
    return {
      status: 'ok',
      uptime: process.uptime(),
      timestamp: new Date().toISOString(),
    };
  }
}
