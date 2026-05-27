import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { PrismaModule } from './common/prisma/prisma.module';
import { RedisModule } from './common/redis/redis.module';
import { WebsocketModule } from './common/websocket/websocket.module';
import { AuthModule } from './modules/auth/auth.module';
import { UserModule } from './modules/user/user.module';
import { QuestionBankModule } from './modules/question-bank/question-bank.module';
import { GameModule } from './modules/game/game.module';
import { RankModule } from './modules/rank/rank.module';
import { AnalyticModule } from './modules/analytic/analytic.module';
import { SystemModule } from './modules/system/system.module';
import { HealthController } from './health.controller';

@Module({
  imports: [
    ConfigModule.forRoot({ isGlobal: true }),
    PrismaModule,
    RedisModule,
    WebsocketModule,
    AuthModule,
    UserModule,
    QuestionBankModule,
    GameModule,
    RankModule,
    AnalyticModule,
    SystemModule,
  ],
  controllers: [HealthController],
})
export class AppModule {}
