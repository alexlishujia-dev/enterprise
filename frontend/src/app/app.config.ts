import { registerLocaleData } from '@angular/common';
import zh from '@angular/common/locales/zh';
import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideNzI18n, zh_CN } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { NzMessageModule } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import {
  CheckCircleOutline,
  DashboardOutline,
  ExportOutline,
  FileTextOutline,
  FolderOutline,
  KeyOutline,
  LoadingOutline,
  LockOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  PlusOutline,
  SafetyCertificateOutline,
  TeamOutline,
  UserOutline
} from '@ant-design/icons-angular/icons';
import { routes } from './app.routes';
import { apiResultInterceptor } from './core/interceptors/api-result.interceptor';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { requestSignInterceptor } from './core/interceptors/request-sign.interceptor';

registerLocaleData(zh);

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([
      requestSignInterceptor,
      authInterceptor,
      apiResultInterceptor
    ])),
    provideAnimationsAsync(),
    provideNzI18n(zh_CN),
    provideNzIcons([
      CheckCircleOutline,
      DashboardOutline,
      ExportOutline,
      UserOutline,
      LockOutline,
      SafetyCertificateOutline,
      TeamOutline,
      FileTextOutline,
      FolderOutline,
      KeyOutline,
      LoadingOutline,
      MenuFoldOutline,
      MenuUnfoldOutline,
      PlusOutline
    ]),
    importProvidersFrom(NzModalModule, NzMessageModule)
  ]
};
