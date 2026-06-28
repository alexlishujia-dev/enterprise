/** 分页查询参数 — 对齐 PageQuery */
export const defaultPageQuery = () => ({
  pageIndex: 1,
  pageSize: 20,
  keyword: ''
});

/** @typedef {{ pageIndex: number; pageSize: number; keyword?: string }} PageQuery */

/** @typedef {{ items: T[]; total: number; pageIndex: number; pageSize: number }} PagedResult */

export function createPagedState() {
  return {
    items: [],
    total: 0,
    pageIndex: 1,
    pageSize: 20,
    loading: false,
    finished: false,
    refreshing: false
  };
}
