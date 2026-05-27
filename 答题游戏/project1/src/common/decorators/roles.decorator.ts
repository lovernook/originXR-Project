import { SetMetadata } from '@nestjs/common';

export const ROLES_KEY = 'roles';

/**
 * 设置允许访问的角色
 * 用法: @Roles('ADMIN', 'SUPER_ADMIN')
 */
export const Roles = (...roles: string[]) => SetMetadata(ROLES_KEY, roles);
