import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, PageQuery } from '../core/models/paged-result.model';
import { SysUserCreateDto, SysUserDto, SysUserUpdateDto } from '../core/models/sys-user.model';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class SysUserApi extends ApiHttpService {
  getPaged(query: PageQuery): Observable<PagedResult<SysUserDto>> {
    return this.get<PagedResult<SysUserDto>>('/SysUser', query);
  }

  getById(id: number): Observable<SysUserDto> {
    return this.get<SysUserDto>(`/SysUser/${id}`);
  }

  create(dto: SysUserCreateDto): Observable<number> {
    return this.post<number>('/SysUser', dto);
  }

  update(id: number, dto: SysUserUpdateDto): Observable<unknown> {
    return this.put<unknown>(`/SysUser/${id}`, dto);
  }

  delete(id: number): Observable<unknown> {
    return this.deleteRequest<unknown>(`/SysUser/${id}`);
  }

  exportExcel(query: PageQuery): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.getBlob('/SysUser/export', query);
  }
}
