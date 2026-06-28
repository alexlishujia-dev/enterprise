import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTableModule, NzTableQueryParams } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { finalize } from 'rxjs';
import { ApiBusinessError } from '../../core/models/api-result.model';
import { SysLogDto } from '../../core/models/sys-log.model';
import { SysLogService } from '../../service/sys-log.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { formatDateTime, truncate } from '../../shared/utils/date.util';
import { handleExportDownload } from '../../shared/utils/export.util';

@Component({
  selector: 'app-log-list',
  standalone: true,
  imports: [
    FormsModule,
    PageHeaderComponent,
    HasPermissionDirective,
    NzCardModule,
    NzTableModule,
    NzButtonModule,
    NzInputModule,
    NzSpaceModule,
    NzTagModule,
    NzIconModule
  ],
  templateUrl: './log-list.component.html'
})
export class LogListComponent implements OnInit {
  private readonly logService = inject(SysLogService);
  private readonly message = inject(NzMessageService);

  loading = false;
  exporting = false;
  items: SysLogDto[] = [];
  total = 0;
  pageIndex = 1;
  pageSize = 20;
  module = '';

  readonly formatDateTime = formatDateTime;
  readonly truncate = truncate;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.logService.loadLogs({
      pageIndex: this.pageIndex,
      pageSize: this.pageSize,
      module: this.module || undefined
    }).pipe(finalize(() => (this.loading = false))).subscribe({
      next: res => {
        this.items = res.items;
        this.total = res.total;
      },
      error: (err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '加载失败')
    });
  }

  search(): void {
    this.pageIndex = 1;
    this.load();
  }

  resetSearch(): void {
    this.module = '';
    this.search();
  }

  exportExcel(): void {
    this.exporting = true;
    this.logService.exportLogs({ module: this.module || undefined }).pipe(
      finalize(() => (this.exporting = false))
    ).subscribe({
      next: response => {
        void handleExportDownload(response, '操作日志.xlsx')
          .then(() => this.message.success('导出成功'))
          .catch((err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '导出失败'));
      },
      error: (err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '导出失败')
    });
  }

  onQueryParamsChange(params: NzTableQueryParams): void {
    this.pageIndex = params.pageIndex;
    this.pageSize = params.pageSize;
    this.load();
  }

  statusColor(code: number): string {
    if (code >= 500) return 'error';
    if (code >= 400) return 'warning';
    return 'success';
  }
}
