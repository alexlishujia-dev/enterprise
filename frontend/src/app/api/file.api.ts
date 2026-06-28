import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiHttpService } from './api-http.service';

@Injectable({ providedIn: 'root' })
export class FileApi extends ApiHttpService {
  uploadAvatar(file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);
    return this.postFormData<string>('/File/avatar', formData);
  }
}
