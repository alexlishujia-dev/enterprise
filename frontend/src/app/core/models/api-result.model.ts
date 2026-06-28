/** 与后端 ApiResult 一致 */
export interface ApiResult<T = unknown> {
  code: number;
  message: string;
  data?: T;
  traceId?: string;
}

export const ApiStatusCode = {
  Success: 200,
  BadRequest: 400,
  Unauthorized: 401,
  Forbidden: 403,
  NotFound: 404,
  Conflict: 409,
  InternalError: 500
} as const;

export class ApiBusinessError extends Error {
  constructor(
    message: string,
    public readonly code: number,
    public readonly traceId?: string
  ) {
    super(message);
    this.name = 'ApiBusinessError';
  }
}
