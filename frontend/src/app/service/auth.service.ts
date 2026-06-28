import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { AuthSession, LoginRequestDto, LoginResponseDto } from '../core/models/auth.model';
import { SysMenuNavDto } from '../core/models/sys-menu.model';
import { SysUserDto } from '../core/models/sys-user.model';
import { StorageUtil } from '../shared/utils/storage.util';
import { environment } from '@env/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUser = signal<SysUserDto | null>(
    StorageUtil.getJson<SysUserDto>(environment.userStorageKey)
  );

  readonly user = this.currentUser.asReadonly();

  constructor(private readonly authApi: AuthApi) {}

  login(request: LoginRequestDto): Observable<LoginResponseDto> {
    return this.authApi.login(request).pipe(tap(res => this.saveSession(res)));
  }

  refreshCurrentUser(): Observable<SysUserDto> {
    return this.authApi.getMe().pipe(tap(user => this.updateUser(user)));
  }

  logout(): void {
    this.clearSession();
  }

  getAccessToken(): string | null {
    return StorageUtil.getString(environment.tokenStorageKey);
  }

  isAuthenticated(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;

    const session = StorageUtil.getJson<AuthSession>(environment.userStorageKey + '_session');
    if (session?.expiresAt && new Date(session.expiresAt).getTime() <= Date.now()) {
      this.clearSession();
      return false;
    }
    return true;
  }

  hasRole(roleCode: string): boolean {
    return this.currentUser()?.roles?.includes(roleCode) ?? false;
  }

  hasPermission(permissionCode: string): boolean {
    return this.currentUser()?.permissions?.includes(permissionCode) ?? false;
  }

  getMenus(): SysMenuNavDto[] {
    return this.currentUser()?.menus ?? [];
  }

  clearSession(): void {
    StorageUtil.remove(environment.tokenStorageKey);
    StorageUtil.remove(environment.userStorageKey);
    StorageUtil.remove(environment.userStorageKey + '_session');
    this.currentUser.set(null);
  }

  private saveSession(response: LoginResponseDto): void {
    StorageUtil.setString(environment.tokenStorageKey, response.accessToken);
    StorageUtil.setJson(environment.userStorageKey, response.user);
    StorageUtil.setJson(environment.userStorageKey + '_session', {
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
      user: response.user
    } satisfies AuthSession);
    this.currentUser.set(response.user);
  }

  private updateUser(user: SysUserDto): void {
    StorageUtil.setJson(environment.userStorageKey, user);
    const session = StorageUtil.getJson<AuthSession>(environment.userStorageKey + '_session');
    if (session) {
      StorageUtil.setJson(environment.userStorageKey + '_session', { ...session, user });
    }
    this.currentUser.set(user);
  }
}