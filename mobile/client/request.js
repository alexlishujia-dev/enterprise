import env from '@/config/env';
import { ApiBusinessError, ApiStatusCode } from '@/model/api.model';
import { getAccessToken, clearSession } from '@/utils/storage';
import {
  buildRequestSignPayloadAsync,
  createNonce,
  hmacSha256Hex,
  parseApiPath,
  shouldSkipRequestSign
} from '@/utils/request-sign';
import { buildQuery } from '@/utils/format';

function joinUrl(path) {
  const base = env.apiBaseUrl.replace(/\/$/, '');
  const normalized = path.startsWith('/') ? path : `/${path}`;
  return `${base}${normalized}`;
}

async function buildHeaders(method, url, bodyText, extraHeaders = {}) {
  const headers = {
    'Content-Type': 'application/json',
    ...extraHeaders
  };

  const token = getAccessToken();
  if (token && !url.includes('/Auth/login')) {
    headers.Authorization = `Bearer ${token}`;
  }

  if (env.requestSignEnabled) {
    const { path, queryString } = parseApiPath(url, env.apiBaseUrl);
    if (!shouldSkipRequestSign(path)) {
      const timestamp = Math.floor(Date.now() / 1000).toString();
      const nonce = createNonce();
      const payload = await buildRequestSignPayloadAsync(method, path, queryString, timestamp, nonce, bodyText);
      const signature = hmacSha256Hex(env.requestSignSecretKey, payload);
      headers['X-Platform-Timestamp'] = timestamp;
      headers['X-Platform-Nonce'] = nonce;
      headers['X-Platform-Signature'] = signature;
    }
  }

  return headers;
}

function unwrapApiResult(result) {
  if (!result || typeof result !== 'object' || !('code' in result)) {
    return result;
  }
  if (result.code !== ApiStatusCode.Success) {
    throw new ApiBusinessError(result.message || '请求失败', result.code, result.traceId);
  }
  return result.data ?? null;
}

function handleUnauthorized() {
  clearSession();
  uni.reLaunch({ url: '/pages/login/login' });
}

function requestRaw(options) {
  return new Promise((resolve, reject) => {
    uni.request({
      ...options,
      timeout: env.requestTimeout,
      success: res => resolve(res),
      fail: err => reject(new ApiBusinessError(err.errMsg || '网络请求失败', 0))
    });
  });
}

export async function request(method, path, { data, params, headers } = {}) {
  const query = buildQuery(params);
  const url = joinUrl(path) + (query ? `?${query}` : '');
  const bodyText = data != null ? JSON.stringify(data) : '';
  const requestHeaders = await buildHeaders(method, url, bodyText, headers);

  const response = await requestRaw({
    url,
    method,
    data,
    header: requestHeaders
  });

  const statusCode = response.statusCode;
  const body = response.data;

  if (statusCode === 401) {
    handleUnauthorized();
    throw new ApiBusinessError('未授权或 Token 无效', ApiStatusCode.Unauthorized);
  }

  if (statusCode >= 400) {
    try {
      unwrapApiResult(body);
    } catch (err) {
      if (err instanceof ApiBusinessError) {
        if (err.code === ApiStatusCode.Unauthorized) handleUnauthorized();
        throw err;
      }
    }
    throw new ApiBusinessError(typeof body === 'object' && body?.message ? body.message : `HTTP ${statusCode}`, statusCode);
  }

  try {
    return unwrapApiResult(body);
  } catch (err) {
    if (err instanceof ApiBusinessError) {
      if (err.code === ApiStatusCode.Unauthorized) handleUnauthorized();
      throw err;
    }
    throw err;
  }
}

export function get(path, params) {
  return request('GET', path, { params });
}

export function post(path, data) {
  return request('POST', path, { data });
}

export function put(path, data) {
  return request('PUT', path, { data });
}

export function del(path) {
  return request('DELETE', path);
}

export function uploadFile(path, filePath, name = 'file') {
  return new Promise((resolve, reject) => {
    const url = joinUrl(path);
    const token = getAccessToken();
    const header = {};
    if (token) header.Authorization = `Bearer ${token}`;

    uni.uploadFile({
      url,
      filePath,
      name,
      header,
      timeout: env.requestTimeout,
      success: res => {
        if (res.statusCode === 401) {
          handleUnauthorized();
          reject(new ApiBusinessError('未授权或 Token 无效', ApiStatusCode.Unauthorized));
          return;
        }
        try {
          const body = typeof res.data === 'string' ? JSON.parse(res.data) : res.data;
          resolve(unwrapApiResult(body));
        } catch (err) {
          reject(err instanceof ApiBusinessError ? err : new ApiBusinessError('上传失败', 500));
        }
      },
      fail: err => reject(new ApiBusinessError(err.errMsg || '上传失败', 0))
    });
  });
}

export default { request, get, post, put, del, uploadFile };
