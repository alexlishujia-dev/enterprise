import { get, post } from './request';

export function login(data) {
  return post('/Auth/login', data);
}

export function getMe() {
  return get('/Auth/me');
}
