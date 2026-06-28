/** @typedef {import('./sys-menu.model').SysMenuNavDto} SysMenuNavDto */

/** @typedef {{
 *   id: number;
 *   userName: string;
 *   displayName?: string;
 *   email?: string;
 *   avatarUrl?: string;
 *   isActive: boolean;
 *   createdAt: string;
 *   roles: string[];
 *   permissions: string[];
 *   menus: SysMenuNavDto[];
 * }} SysUserDto */

/** @typedef {{ userName: string; password: string }} LoginRequestDto */

/** @typedef {{ accessToken: string; expiresAt: string; user: SysUserDto }} LoginResponseDto */

/** @typedef {{ accessToken: string; expiresAt: string; user: SysUserDto }} AuthSession */

export {};
