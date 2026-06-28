import { SysMenuNavDto } from './sys-menu.model';

export interface SysUserDto {
  id: number;
  userName: string;
  displayName?: string;
  email?: string;
  avatarUrl?: string;
  isActive: boolean;
  createdAt: string;
  roles: string[];
  permissions: string[];
  menus: SysMenuNavDto[];
}

export interface SysUserCreateDto {
  userName: string;
  password: string;
  displayName?: string;
  email?: string;
  avatarUrl?: string;
}

export interface SysUserUpdateDto {
  displayName?: string;
  email?: string;
  isActive: boolean;
  password?: string;
  avatarUrl?: string;
}
