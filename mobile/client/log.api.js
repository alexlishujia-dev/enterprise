import { get } from './request';

export function getLogPaged(params) {
  return get('/SysLog', params);
}
