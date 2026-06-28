import { get, post, put, del } from './request';

export function getRolePaged(params) {
  return get('/SysRole', params);
}

export function getRoleById(id) {
  return get(`/SysRole/${id}`);
}

export function createRole(data) {
  return post('/SysRole', data);
}

export function updateRole(id, data) {
  return put(`/SysRole/${id}`, data);
}

export function deleteRole(id) {
  return del(`/SysRole/${id}`);
}
