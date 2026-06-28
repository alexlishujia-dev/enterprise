import CryptoJS from 'crypto-js';

export function sha256Hex(text) {
  return CryptoJS.SHA256(text).toString(CryptoJS.enc.Hex);
}

export function hmacSha256Hex(key, message) {
  return CryptoJS.HmacSHA256(message, key).toString(CryptoJS.enc.Hex);
}

export function createNonce() {
  return `${Date.now()}${Math.random().toString(16).slice(2)}`.replace(/\./g, '');
}

export function parseApiPath(url, apiBaseUrl) {
  if (url.startsWith('http://') || url.startsWith('https://')) {
    try {
      const parsed = new URL(url);
      return { path: parsed.pathname, queryString: parsed.search };
    } catch {
      // fall through
    }
  }

  let path = url;
  let queryString = '';
  const queryIndex = path.indexOf('?');
  if (queryIndex >= 0) {
    queryString = path.slice(queryIndex);
    path = path.slice(0, queryIndex);
  }
  if (!path.startsWith('/')) {
    path = `/${path}`;
  }
  return { path, queryString };
}

export async function buildRequestSignPayloadAsync(method, path, queryString, timestamp, nonce, body) {
  const bodyHash = sha256Hex(body || '');
  return [method.toUpperCase(), path, queryString, timestamp, nonce, bodyHash].join('\n');
}

const REQUEST_SIGN_SKIP_PREFIXES = ['/health', '/swagger', '/uploads', '/api/File'];

export function shouldSkipRequestSign(path) {
  return REQUEST_SIGN_SKIP_PREFIXES.some(prefix => path.startsWith(prefix));
}
