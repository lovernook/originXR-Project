import {
  WebSocketGateway, WebSocketServer,
  OnGatewayConnection, OnGatewayDisconnect,
  SubscribeMessage, MessageBody, ConnectedSocket,
} from '@nestjs/websockets';
import { Server, Socket } from 'socket.io';
import { Injectable, Logger } from '@nestjs/common';

@Injectable()
@WebSocketGateway({
  namespace: '/events',
  cors: { origin: '*', credentials: true },
  pingInterval: 25000,
  pingTimeout: 60000,
})
export class EventGateway implements OnGatewayConnection, OnGatewayDisconnect {
  @WebSocketServer()
  server: Server;

  private readonly logger = new Logger(EventGateway.name);
  private authenticatedClients = new Set<string>();

  handleConnection(client: Socket) {
    client.data.userId = '00000000-0000-0000-0000-000000000001';
    client.data.username = 'unity-client';
    this.authenticatedClients.add(client.id);
    this.logger.log(`客户端已连接: ${client.id}`);
  }

  handleDisconnect(client: Socket) {
    this.authenticatedClients.delete(client.id);
    this.logger.log(`客户端断开: ${client.data?.username || client.id}`);
  }

  // ========== 客户端可调用的方法 ==========

  @SubscribeMessage('ping')
  handlePing(@ConnectedSocket() client: Socket) {
    client.emit('pong', { serverTime: Date.now() });
  }

  @SubscribeMessage('subscribe')
  handleSubscribe(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: { channels: string[] },
  ) {
    if (data?.channels) {
      data.channels.forEach((ch) => client.join(ch));
      this.logger.debug(`${client.data.username} 订阅频道: ${data.channels.join(', ')}`);
    }
  }

  @SubscribeMessage('unsubscribe')
  handleUnsubscribe(
    @ConnectedSocket() client: Socket,
    @MessageBody() data: { channels: string[] },
  ) {
    if (data?.channels) {
      data.channels.forEach((ch) => client.leave(ch));
      this.logger.debug(`${client.data.username} 取消订阅: ${data.channels.join(', ')}`);
    }
  }

  // ========== 供其他模块调用的广播方法 ==========

  /** 题库变更 */
  emitQuestionCreated(data: any) {
    this.server.emit('questions:created', data);
    this.logger.debug(`📡 questions:created — ${data.id}`);
  }

  emitQuestionUpdated(data: any) {
    this.server.emit('questions:updated', data);
    this.logger.debug(`📡 questions:updated — ${data.id}`);
  }

  emitQuestionDeleted(data: { id: string }) {
    this.server.emit('questions:deleted', data);
    this.logger.debug(`📡 questions:deleted — ${data.id}`);
  }

  /** 关卡变更 */
  emitStageCreated(data: any) {
    this.server.emit('stages:created', data);
    this.logger.debug(`📡 stages:created — ${data.id}`);
  }

  emitStageUpdated(data: any) {
    this.server.emit('stages:updated', data);
    this.logger.debug(`📡 stages:updated — ${data.id}`);
  }

  emitStageDeleted(data: { id: string }) {
    this.server.emit('stages:deleted', data);
    this.logger.debug(`📡 stages:deleted — ${data.id}`);
  }

  /** 知识点变更 */
  emitKnowledgeUpdated(data: any) {
    this.server.emit('knowledge:updated', data);
    this.logger.debug(`📡 knowledge:updated — ${JSON.stringify(data)}`);
  }

  /** 系统配置变更 */
  emitConfigChanged(data: { key: string; value: any }) {
    this.server.emit('system:config_changed', data);
    this.logger.debug(`📡 system:config_changed — ${data.key}`);
  }

  /** 公告变更 */
  emitAnnouncementChanged(action: 'created' | 'updated' | 'deleted', data: any) {
    this.server.emit(`system:announcement_${action}`, data);
    this.logger.debug(`📡 system:announcement_${action} — ${data.id || ''}`);
  }

  /** 道具变更 */
  emitItemChanged(action: 'created' | 'updated' | 'deleted', data: any) {
    this.server.emit(`system:item_${action}`, data);
    this.logger.debug(`📡 system:item_${action} — ${data.id || ''}`);
  }

  /** 活动变更 */
  emitActivityChanged(action: 'created' | 'updated', data: any) {
    this.server.emit(`system:activity_${action}`, data);
    this.logger.debug(`📡 system:activity_${action} — ${data.id || ''}`);
  }

  /** 游戏事件：关卡通关 */
  emitStageCleared(data: { userId: string; stageId: string; stageName: string }) {
    this.server.emit('game:stage_cleared', data);
    this.logger.debug(`📡 game:stage_cleared — ${data.userId} 通过了 ${data.stageName}`);
  }

  /** 向特定用户推送 */
  emitToUser(userId: string, event: string, data: any) {
    this.server.emit(event, data); // socket.io namespace 广播，客户端按 userId 过滤
    this.logger.debug(`📡 [user:${userId}] ${event}`);
  }
}
