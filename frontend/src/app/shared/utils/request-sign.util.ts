/** 与后端 RequestSignHelper 一致的 HMAC-SHA256 签名 */
export async function sha256Hex(text: string): Promise<string> {
  const data = new TextEncoder().encode(text);
  const hash = await crypto.subtle.digest('SHA-256', data);
  return Array.from(new Uint8Array(hash))
    .map(b => b.toString(16).padStart(2, '0'))
    .join('');
}

export async function hmacSha256Hex(key: string, message: string): Promise<string> {
  const encoder = new TextEncoder();
  const cryptoKey = await crypto.subtle.importKey(
    'raw',
    encoder.encode(key),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign']
  );
  const signature = await crypto.subtle.sign('HMAC', cryptoKey, encoder.encode(message));
  return Array.from(new Uint8Array(signature))
    .map(b => b.toString(16).padStart(2, '0'))
    .join('');
}

export function buildRequestSignPayload(
  method: string,
  path: string,
  queryString: string,
  timestamp: string,
  nonce: string,
  body: string
): string {
  return [method.toUpperCase(), path, queryString, timestamp, nonce, ''].join('\n');
}

export async function buildRequestSignPayloadAsync(
  method: string,
  path: string,
  queryString: string,
  timestamp: string,
  nonce: string,
  body: string
): Promise<string> {
  const bodyHash = await sha256Hex(body);
  return [method.toUpperCase(), path, queryString, timestamp, nonce, bodyHash].join('\n');
}

export function createNonce(): string {
  return crypto.randomUUID().replace(/-/g, '');
}

export function parseApiPath(fullUrl: string, apiBaseUrl: string): { path: string; queryString: string } {
  try {
    const base = apiBaseUrl.startsWith('http') ? apiBaseUrl : `${window.location.origin}${apiBaseUrl}`;
    const url = new URL(fullUrl, base.endsWith('/') ? base : `${base}/`);
    return { path: url.pathname, queryString: url.search };
  } catch {
    const [pathPart, queryPart = ''] = fullUrl.split('?');
    const path = pathPart.startsWith('/') ? pathPart : `/${pathPart}`;
    return { path, queryString: queryPart ? `?${queryPart}` : '' };
  }
}
