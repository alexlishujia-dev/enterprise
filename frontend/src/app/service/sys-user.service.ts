import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SysUserApi } from '../api/sys-user.api';
import { APP_CONSTANTS } from '../core/config/app.constants';
import { PagedResult, PageQuery } from '../core/models/paged-result.model';
import { SysUserCreateDto, SysUserDto, SysUserUpdateDto } from '../core/models/sys-user.model';

@Injectable({ providedIn: 'root' })
export class SysUserService {
  constructor(private readonly userApi: SysUserApi) {}

  loadUsers(query: PageQuery = {}): Observable<PagedResult<SysUserDto>> {
    return this.userApi.getPaged({
      pageIndex: query.pageIndex ?? 1,
      pageSize: query.pageSize ?? APP_CONSTANTS.defaultPageSize,
      keyword: query.keyword
    });
  }

  createUser(dto: SysUserCreateDto): Observable<number> {
    return this.userApi.create(dto);
  }

  getUserById(id: number): Observable<SysUserDto> {
    return this.userApi.getById(id);
  }

  updateUser(id: number, dto: SysUserUpdateDto): Observable<unknown> {
    return this.userApi.update(id, dto);
  }

  deleteUser(id: number): Observable<unknown> {
    return this.userApi.delete(id);
  }

  exportUsers(query: Pick<PageQuery, 'keyword'> = {}): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.userApi.exportExcel({ keyword: query.keyword });
  }
}
