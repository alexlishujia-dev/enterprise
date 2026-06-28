export interface SysPermissionDto {
  id: number;
  menuId: number;
  permissionCode: string;
  permissionName: string;
  description?: string;
}

export interface SysMenuTreeDto {
  id: number;
  parentId?: number | null;
  menuCode: string;
  menuName: string;
  path?: string;
  icon?: string;
  sortOrder: number;
  isActive: boolean;
  permissions: SysPermissionDto[];
  children: SysMenuTreeDto[];
}

export interface SysMenuNavDto {
  id: number;
  menuCode: string;
  menuName: string;
  path?: string;
  icon?: string;
  sortOrder: number;
  children: SysMenuNavDto[];
}

export interface AssignRolePermissionsDto {
  permissionIds: number[];
}
