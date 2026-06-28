/** 与后端 ApiResult / ApiStatusCode 一致 */
export const ApiStatusCode = {
  Success: 200,
  BadRequest: 400,
  Unauthorized: 401,
  Forbidden: 403,
  NotFound: 404,
  Conflict: 409,
  InternalError: 500
};

export class ApiBusinessError extends Error {
  constructor(message, code, traceId) {
    super(message);
    this.name = 'ApiBusinessError';
    this.code = code;
    this.traceId = traceId;
  }
}

/** @typedef {{ code: number; message: string; data?: T; traceId?: string }} ApiResult */
