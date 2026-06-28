import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { PageQuery } from '../core/models/paged-result.model';

/** 查询参数（兼容 PageQuery 及各模块 Query 接口） */
export type ApiQueryParams = PageQuery | Record<string, string | number | boolean | undefined | null>;

@Injectable({ providedIn: 'root' })
export class ApiHttpService {
  protected readonly http = inject(HttpClient);
  protected readonly baseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  protected url(path: string): string {
    return `${this.baseUrl}${path.startsWith('/') ? path : `/${path}`}`;
  }

  protected toParams(query?: ApiQueryParams): HttpParams {
    let params = new HttpParams();
    if (!query) return params;

    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }

  protected get<T>(path: string, query?: ApiQueryParams): Observable<T> {
    return this.http.get<T>(this.url(path), { params: this.toParams(query) });
  }

  protected getBlob(path: string, query?: ApiQueryParams): Observable<import('@angular/common/http').HttpResponse<Blob>> {
    return this.http.get(this.url(path), {
      params: this.toParams(query),
      responseType: 'blob',
      observe: 'response'
    });
  }

  protected post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(this.url(path), body);
  }

  protected postFormData<T>(path: string, body: FormData): Observable<T> {
    return this.http.post<T>(this.url(path), body);
  }

  protected put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(this.url(path), body);
  }

  protected deleteRequest<T>(path: string): Observable<T> {
    return this.http.delete<T>(this.url(path));
  }
}
