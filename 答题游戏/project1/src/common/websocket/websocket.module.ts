import { Global, Module } from '@nestjs/common';
import { EventGateway } from './event.gateway';

@Global()
@Module({
  providers: [EventGateway],
  exports: [EventGateway],
})
export class WebsocketModule {}
