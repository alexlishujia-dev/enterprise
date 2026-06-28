import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SysMenuApi } from '../api/sys-menu.api';
import { SysMenuTreeDto } from '../core/models/sys-menu.model';

@Injectable({ providedIn: 'root' })
export class SysMenuService {
  constructor(private readonly menuApi: SysMenuApi) {}

  loadMenuTree(): Observable<SysMenuTreeDto[]> {
    return this.menuApi.getTree();
  }
}
