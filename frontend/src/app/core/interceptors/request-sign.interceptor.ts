import { HttpInterceptorFn } from '@angular/common/http';
import { from, switchMap } from 'rxjs';
import { environment } from '@env/environment';
import { REQUEST_SIGN_HEADERS } from '../config/app.constants';
import {
  buildRequestSignPayloadAsync,
  createNonce,
  hmacSha256Hex,
  parseApiPath
} from '../../shared/utils/request-sign.util';

/** 与后端 RequestSignOptions.SkipPaths 保持一致 */
const REQUEST_SIGN_SKIP_PATH_PREFIXES = ['/health', '/swagger', '/uploads', '/api/File'];

/** 请求数据验签，与后端 RequestSignatureMiddleware 对齐 */
export const requestSignInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.requestSignEnabled || shouldSkipRequestSign(req)) {
    return next(req);
  }

  return from(signRequest(req)).pipe(switchMap(signed => next(signed)));
};

function shouldSkipRequestSign(req: import('@angular/common/http').HttpRequest<unknown>): boolean {
  if (req.body instanceof FormData) {
    return true;
  }

  const { path } = parseApiPath(req.urlWithParams, environment.apiBaseUrl);
  return REQUEST_SIGN_SKIP_PATH_PREFIXES.some(prefix =>
    path.startsWith(prefix)
  );
}

async function signRequest(req: import('@angular/common/http').HttpRequest<unknown>) {
  const timestamp = Math.floor(Date.now() / 1000).toString();
  const nonce = createNonce();
  const bodyText = typeof req.body === 'string' ? req.body : req.body ? JSON.stringify(req.body) : '';
  const { path, queryString } = parseApiPath(req.urlWithParams, environment.apiBaseUrl);

  const payload = await buildRequestSignPayloadAsync(
    req.method,
    path,
    queryString,
    timestamp,
    nonce,
    bodyText
  );
  const signature = await hmacSha256Hex(environment.requestSignSecretKey, payload);

  return req.clone({
    setHeaders: {
      [REQUEST_SIGN_HEADERS.timestamp]: timestamp,
      [REQUEST_SIGN_HEADERS.nonce]: nonce,
      [REQUEST_SIGN_HEADERS.signature]: signature
    }
  });
}
