import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, PageQuery } from '../core/models/paged-result.model';
import {
  AssignUserRolesDto,
  SysRoleCreateDto,
  SysRoleDto,
  SysRoleUpdateDto
} from '../core/models/sys-role.model';
import { AssignRolePermissionsDto } from '../core/models/sys-menu.model';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class SysRoleApi extends ApiHttpService {
  getPaged(query: PageQuery): Observable<PagedResult<SysRoleDto>> {
    return this.get<PagedResult<SysRoleDto>>('/SysRole', query);
  }

  getById(id: number): Observable<SysRoleDto> {
    return this.get<SysRoleDto>(`/SysRole/${id}`);
  }

  create(dto: SysRoleCreateDto): Observable<number> {
    return this.post<number>('/SysRole', dto);
  }

  update(id: number, dto: SysRoleUpdateDto): Observable<unknown> {
    return this.put<unknown>(`/SysRole/${id}`, dto);
  }

  delete(id: number): Observable<unknown> {
    return this.deleteRequest<unknown>(`/SysRole/${id}`);
  }

  getUserRoles(userId: number): Observable<SysRoleDto[]> {
    return this.get<SysRoleDto[]>(`/SysRole/user/${userId}`);
  }

  assignUserRoles(userId: number, dto: AssignUserRolesDto): Observable<unknown> {
    return this.put<unknown>(`/SysRole/user/${userId}`, dto);
  }

  getRolePermissions(roleId: number): Observable<number[]> {
    return this.get<number[]>(`/SysRole/${roleId}/permissions`);
  }

  assignRolePermissions(roleId: number, dto: AssignRolePermissionsDto): Observable<unknown> {
    return this.put<unknown>(`/SysRole/${roleId}/permissions`, dto);
  }

  exportExcel(query: PageQuery): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.getBlob('/SysRole/export', query);
  }
}
