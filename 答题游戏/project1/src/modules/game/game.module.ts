import { Module } from '@nestjs/common';
import { GameService } from './game.service';
import { GameController } from './game.controller';
import { StageAdminController } from './stage-admin.controller';
import { QuestionBankModule } from '../question-bank/question-bank.module';
import { UserModule } from '../user/user.module';

@Module({
  imports: [QuestionBankModule, UserModule],
  controllers: [GameController, StageAdminController],
  providers: [GameService],
  exports: [GameService],
})
export class GameModule {}
