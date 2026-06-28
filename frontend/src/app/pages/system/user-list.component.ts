import { Component, inject, OnInit } from '@angular/core';

import { NzAvatarModule } from 'ng-zorro-antd/avatar';

import { NzButtonModule } from 'ng-zorro-antd/button';

import { NzCardModule } from 'ng-zorro-antd/card';

import { NzIconModule } from 'ng-zorro-antd/icon';

import { NzInputModule } from 'ng-zorro-antd/input';

import { NzMessageService } from 'ng-zorro-antd/message';

import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';

import { NzSpaceModule } from 'ng-zorro-antd/space';

import { NzTableModule, NzTableQueryParams } from 'ng-zorro-antd/table';

import { NzTagModule } from 'ng-zorro-antd/tag';

import { RouterLink } from '@angular/router';

import { FormsModule } from '@angular/forms';

import { finalize } from 'rxjs';

import { ApiBusinessError } from '../../core/models/api-result.model';

import { SysUserDto } from '../../core/models/sys-user.model';

import { SysUserService } from '../../service/sys-user.service';

import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

import { formatDateTime } from '../../shared/utils/date.util';

import { resolveAssetUrl } from '../../shared/utils/asset-url.util';

import { handleExportDownload } from '../../shared/utils/export.util';



@Component({

  selector: 'app-user-list',

  standalone: true,

  imports: [

    FormsModule,

    RouterLink,

    PageHeaderComponent,

    HasPermissionDirective,

    NzCardModule,

    NzTableModule,

    NzButtonModule,

    NzInputModule,

    NzPopconfirmModule,

    NzSpaceModule,

    NzTagModule,

    NzIconModule,

    NzAvatarModule

  ],

  templateUrl: './user-list.component.html',

  styleUrl: './system-pages.scss'

})

export class UserListComponent implements OnInit {

  private readonly userService = inject(SysUserService);

  private readonly message = inject(NzMessageService);



  loading = false;

  exporting = false;

  items: SysUserDto[] = [];

  total = 0;

  pageIndex = 1;

  pageSize = 20;

  keyword = '';



  readonly formatDateTime = formatDateTime;

  readonly resolveAssetUrl = resolveAssetUrl;

  ngOnInit(): void {

    this.load();

  }



  load(): void {

    this.loading = true;

    this.userService.loadUsers({

      pageIndex: this.pageIndex,

      pageSize: this.pageSize,

      keyword: this.keyword || undefined

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

    this.keyword = '';

    this.search();

  }



  exportExcel(): void {

    this.exporting = true;

    this.userService.exportUsers({ keyword: this.keyword || undefined }).pipe(

      finalize(() => (this.exporting = false))

    ).subscribe({

      next: response => {

        void handleExportDownload(response, '用户列表.xlsx')

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



  deleteUser(id: number): void {

    this.userService.deleteUser(id).subscribe({

      next: () => {

        this.message.success('删除成功');

        this.load();

      },

      error: (err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '删除失败')

    });

  }

}


