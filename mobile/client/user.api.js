import { get, post, put, del, uploadFile } from './request';

export function getUserPaged(params) {
  return get('/SysUser', params);
}

export function getUserById(id) {
  return get(`/SysUser/${id}`);
}

export function createUser(data) {
  return post('/SysUser', data);
}

export function updateUser(id, data) {
  return put(`/SysUser/${id}`, data);
}

export function deleteUser(id) {
  return del(`/SysUser/${id}`);
}

export function uploadAvatar(filePath) {
  return uploadFile('/File/avatar', filePath, 'file');
}
