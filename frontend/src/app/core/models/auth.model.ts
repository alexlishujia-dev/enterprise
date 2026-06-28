import { SysUserDto } from './sys-user.model';

export interface LoginRequestDto {
  userName: string;
  password: string;
}

export interface LoginResponseDto {
  accessToken: string;
  expiresAt: string;
  user: SysUserDto;
}

export interface AuthSession {
  accessToken: string;
  expiresAt: string;
  user: SysUserDto;
}
