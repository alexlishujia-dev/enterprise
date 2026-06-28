import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginRequestDto, LoginResponseDto } from '../core/models/auth.model';
import { SysUserDto } from '../core/models/sys-user.model';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class AuthApi extends ApiHttpService {
  login(request: LoginRequestDto): Observable<LoginResponseDto> {
    return this.post<LoginResponseDto>('/Auth/login', request);
  }

  getMe(): Observable<SysUserDto> {
    return this.get<SysUserDto>('/Auth/me');
  }
}
