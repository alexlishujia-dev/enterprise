import * as userApi from '@/client/user.api';
import env from '@/config/env';

export function loadUsers(query = {}) {
  return userApi.getUserPaged({
    pageIndex: query.pageIndex ?? 1,
    pageSize: query.pageSize ?? env.defaultPageSize,
    keyword: query.keyword || undefined
  });
}

export function getUserById(id) {
  return userApi.getUserById(id);
}

export function createUser(dto) {
  return userApi.createUser(dto);
}

export function updateUser(id, dto) {
  return userApi.updateUser(id, dto);
}

export function deleteUser(id) {
  return userApi.deleteUser(id);
}

export function uploadAvatar(filePath) {
  return userApi.uploadAvatar(filePath);
}
