import * as authApi from '@/client/auth.api';
import { StorageKeys } from '@/model/enum';
import { clearSession, getJson, setJson, setString } from '@/utils/storage';

export async function login(request) {
  const response = await authApi.login(request);
  saveSession(response);
  return response;
}

export async function refreshCurrentUser() {
  const user = await authApi.getMe();
  setJson(StorageKeys.user, user);
  const session = getJson(StorageKeys.session);
  if (session) {
    setJson(StorageKeys.session, { ...session, user });
  }
  return user;
}

export function logout() {
  clearSession();
}

function saveSession(response) {
  setString(StorageKeys.token, response.accessToken);
  setJson(StorageKeys.user, response.user);
  setJson(StorageKeys.session, {
    accessToken: response.accessToken,
    expiresAt: response.expiresAt,
    user: response.user
  });
}

export function getStoredUser() {
  return getJson(StorageKeys.user);
}

export function isSessionValid() {
  const session = getJson(StorageKeys.session);
  if (!session?.accessToken) return false;
  if (session.expiresAt && new Date(session.expiresAt).getTime() <= Date.now()) {
    clearSession();
    return false;
  }
  return true;
}
