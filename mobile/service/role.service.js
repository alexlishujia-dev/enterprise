import env from '@/config/env';
import * as roleApi from '@/client/role.api';

export function loadRoles(query = {}) {
  return roleApi.getRolePaged({
    pageIndex: query.pageIndex ?? 1,
    pageSize: query.pageSize ?? env.defaultPageSize,
    keyword: query.keyword || undefined
  });
}

export function getRoleById(id) {
  return roleApi.getRoleById(id);
}

export function createRole(dto) {
  return roleApi.createRole(dto);
}

export function updateRole(id, dto) {
  return roleApi.updateRole(id, dto);
}

export function deleteRole(id) {
  return roleApi.deleteRole(id);
}
