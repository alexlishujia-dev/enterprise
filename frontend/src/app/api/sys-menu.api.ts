import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { SysMenuTreeDto } from '../core/models/sys-menu.model';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class SysMenuApi extends ApiHttpService {
  getTree(): Observable<SysMenuTreeDto[]> {
    return this.get<SysMenuTreeDto[]>('/SysMenu/tree');
  }
}
