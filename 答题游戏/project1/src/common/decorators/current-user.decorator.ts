import { createParamDecorator, ExecutionContext } from '@nestjs/common';

const FALLBACK_USER = {
  id: process.env.DEFAULT_USER_ID || '00000000-0000-0000-0000-000000000001',
  username: process.env.DEFAULT_USERNAME || 'unity-client',
  role: 'STUDENT',
};

/**
 * 从JWT payload中提取当前用户信息。
 * 无认证时返回环境变量配置的默认用户，方便 Unity 端联调。
 * 用法: @CurrentUser() user / @CurrentUser('id') userId
 */
export const CurrentUser = createParamDecorator(
  (data: string | undefined, ctx: ExecutionContext) => {
    const request = ctx.switchToHttp().getRequest();
    const user = request.user || FALLBACK_USER;
    if (data) {
      return user?.[data];
    }
    return user;
  },
);
