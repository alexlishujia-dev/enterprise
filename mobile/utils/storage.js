import { StorageKeys } from '@/model/enum';

export function getString(key, defaultValue = '') {
  try {
    return uni.getStorageSync(key) || defaultValue;
  } catch {
    return defaultValue;
  }
}

export function setString(key, value) {
  uni.setStorageSync(key, value);
}

export function getJson(key) {
  const raw = getString(key);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function setJson(key, value) {
  setString(key, JSON.stringify(value));
}

export function remove(key) {
  try {
    uni.removeStorageSync(key);
  } catch {
    // ignore
  }
}

export function clearSession() {
  remove(StorageKeys.token);
  remove(StorageKeys.user);
  remove(StorageKeys.session);
}

export function getAccessToken() {
  return getString(StorageKeys.token);
}
