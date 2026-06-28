import env from '@/config/env';
import * as logApi from '@/client/log.api';

export function loadLogs(query = {}) {
  return logApi.getLogPaged({
    pageIndex: query.pageIndex ?? 1,
    pageSize: query.pageSize ?? env.defaultPageSize,
    keyword: query.keyword || undefined
  });
}
