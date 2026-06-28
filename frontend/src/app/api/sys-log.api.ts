import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, SysLogQuery } from '../core/models/paged-result.model';
import { SysLogDto } from '../core/models/sys-log.model';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class SysLogApi extends ApiHttpService {
  getPaged(query: SysLogQuery): Observable<PagedResult<SysLogDto>> {
    return this.get<PagedResult<SysLogDto>>('/SysLog', query);
  }

  exportExcel(query: SysLogQuery): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.getBlob('/SysLog/export', query);
  }
}
