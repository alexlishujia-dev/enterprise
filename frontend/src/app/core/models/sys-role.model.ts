export interface SysRoleDto {
  id: number;
  roleCode: string;
  roleName: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface SysRoleCreateDto {
  roleCode: string;
  roleName: string;
  description?: string;
}

export interface SysRoleUpdateDto {
  roleName: string;
  description?: string;
  isActive: boolean;
}

export interface AssignUserRolesDto {
  roleIds: number[];
}
