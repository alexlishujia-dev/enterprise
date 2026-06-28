import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { FileApi } from '../api/file.api';

@Injectable({ providedIn: 'root' })
export class FileService {
  private readonly fileApi = inject(FileApi);

  uploadAvatar(file: File): Observable<string> {
    return this.fileApi.uploadAvatar(file);
  }
}
