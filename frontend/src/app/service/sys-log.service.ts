import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SysLogApi } from '../api/sys-log.api';
import { APP_CONSTANTS } from '../core/config/app.constants';
import { PagedResult, SysLogQuery } from '../core/models/paged-result.model';
import { SysLogDto } from '../core/models/sys-log.model';

@Injectable({ providedIn: 'root' })
export class SysLogService {
  constructor(private readonly logApi: SysLogApi) {}

  loadLogs(query: SysLogQuery = {}): Observable<PagedResult<SysLogDto>> {
    return this.logApi.getPaged({
      pageIndex: query.pageIndex ?? 1,
      pageSize: query.pageSize ?? APP_CONSTANTS.defaultPageSize,
      keyword: query.keyword,
      userId: query.userId,
      module: query.module,
      startDate: query.startDate,
      endDate: query.endDate
    });
  }

  exportLogs(query: Pick<SysLogQuery, 'module' | 'keyword' | 'userId' | 'startDate' | 'endDate'> = {}): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.logApi.exportExcel({
      module: query.module,
      keyword: query.keyword,
      userId: query.userId,
      startDate: query.startDate,
      endDate: query.endDate
    });
  }
}
