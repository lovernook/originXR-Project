import { Module } from '@nestjs/common';
import { AnalyticService } from './analytic.service';
import { AnalyticController } from './analytic.controller';

@Module({
  controllers: [AnalyticController],
  providers: [AnalyticService],
})
export class AnalyticModule {}
