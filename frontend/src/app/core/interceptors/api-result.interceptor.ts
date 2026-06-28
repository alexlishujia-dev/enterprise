import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, throwError } from 'rxjs';
import { ApiBusinessError, ApiResult, ApiStatusCode } from '../models/api-result.model';
import { AuthService } from '../../service/auth.service';

/** 统一处理 ApiResult 包装响应 */
export const apiResultInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    map(event => {
      if (!(event instanceof HttpResponse)) {
        return event;
      }

      const body = event.body;
      if (!body || typeof body !== 'object' || !('code' in body)) {
        return event;
      }

      const result = body as ApiResult<unknown>;
      if (result.code !== ApiStatusCode.Success) {
        throw new ApiBusinessError(result.message || '请求失败', result.code, result.traceId);
      }

      return event.clone({ body: result.data ?? null });
    }),
    catchError(error => {
      const apiError = error instanceof ApiBusinessError
        ? error
        : toApiBusinessError(error);

      if (apiError) {
        if (apiError.code === ApiStatusCode.Unauthorized && !req.url.includes('/Auth/login')) {
          auth.clearSession();
          void router.navigate(['/login']);
        }
        return throwError(() => apiError);
      }

      return throwError(() => error);
    })
  );
};

function toApiBusinessError(error: unknown): ApiBusinessError | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const body = error.error;
  if (!body || typeof body !== 'object' || !('code' in body)) {
    return null;
  }

  const result = body as ApiResult<unknown>;
  return new ApiBusinessError(result.message || '请求失败', result.code, result.traceId);
}
