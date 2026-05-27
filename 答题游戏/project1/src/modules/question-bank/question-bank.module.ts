import { Module } from '@nestjs/common';
import { QuestionService } from './question.service';
import { KnowledgeService } from './knowledge.service';
import { QuestionController } from './question.controller';
import { KnowledgeController } from './knowledge.controller';

@Module({
  controllers: [QuestionController, KnowledgeController],
  providers: [QuestionService, KnowledgeService],
  exports: [QuestionService, KnowledgeService],
})
export class QuestionBankModule {}
