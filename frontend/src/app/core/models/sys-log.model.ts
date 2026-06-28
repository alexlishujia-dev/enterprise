export interface SysLogDto {
  id: number;
  userId?: number;
  userName?: string;
  module: string;
  action: string;
  httpMethod: string;
  requestPath: string;
  ipAddress?: string;
  requestBody?: string;
  statusCode: number;
  durationMs: number;
  createdAt: string;
}
