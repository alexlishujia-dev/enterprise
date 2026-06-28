/** 与后端 PagedResult 一致 */
export interface PagedResult<T> {
  items: T[];
  total: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface PageQuery {
  pageIndex?: number;
  pageSize?: number;
  keyword?: string;
}

export interface SysLogQuery extends PageQuery {
  userId?: number;
  module?: string;
  startDate?: string;
  endDate?: string;
}
