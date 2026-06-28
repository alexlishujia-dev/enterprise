import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SysRoleApi } from '../api/sys-role.api';
import { APP_CONSTANTS } from '../core/config/app.constants';
import { PagedResult, PageQuery } from '../core/models/paged-result.model';
import {
  AssignUserRolesDto,
  SysRoleCreateDto,
  SysRoleDto,
  SysRoleUpdateDto
} from '../core/models/sys-role.model';
import { AssignRolePermissionsDto } from '../core/models/sys-menu.model';

@Injectable({ providedIn: 'root' })
export class SysRoleService {
  constructor(private readonly roleApi: SysRoleApi) {}

  loadRoles(query: PageQuery = {}): Observable<PagedResult<SysRoleDto>> {
    return this.roleApi.getPaged({
      pageIndex: query.pageIndex ?? 1,
      pageSize: query.pageSize ?? APP_CONSTANTS.defaultPageSize,
      keyword: query.keyword
    });
  }

  createRole(dto: SysRoleCreateDto): Observable<number> {
    return this.roleApi.create(dto);
  }

  getRoleById(id: number): Observable<SysRoleDto> {
    return this.roleApi.getById(id);
  }

  updateRole(id: number, dto: SysRoleUpdateDto): Observable<unknown> {
    return this.roleApi.update(id, dto);
  }

  deleteRole(id: number): Observable<unknown> {
    return this.roleApi.delete(id);
  }

  getUserRoles(userId: number): Observable<SysRoleDto[]> {
    return this.roleApi.getUserRoles(userId);
  }

  assignUserRoles(userId: number, dto: AssignUserRolesDto): Observable<unknown> {
    return this.roleApi.assignUserRoles(userId, dto);
  }

  getRolePermissions(roleId: number): Observable<number[]> {
    return this.roleApi.getRolePermissions(roleId);
  }

  assignRolePermissions(roleId: number, dto: AssignRolePermissionsDto): Observable<unknown> {
    return this.roleApi.assignRolePermissions(roleId, dto);
  }

  exportRoles(query: Pick<PageQuery, 'keyword'> = {}): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.roleApi.exportExcel({ keyword: query.keyword });
  }
}
