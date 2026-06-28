import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { finalize, forkJoin } from 'rxjs';
import { ApiBusinessError } from '../../core/models/api-result.model';
import { SysRoleDto } from '../../core/models/sys-role.model';
import { SysRoleService } from '../../service/sys-role.service';
import { SysUserService } from '../../service/sys-user.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-user-assign-roles',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    NzCardModule,
    NzCheckboxModule,
    NzButtonModule,
    NzSpaceModule,
    NzSpinModule
  ],
  templateUrl: './user-assign-roles.component.html',
  styleUrl: './system-pages.scss'
})
export class UserAssignRolesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(SysUserService);
  private readonly roleService = inject(SysRoleService);
  private readonly message = inject(NzMessageService);

  userId = 0;
  userName = '';
  loading = false;
  saving = false;
  allRoles: SysRoleDto[] = [];
  selectedRoleIds: number[] = [];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      void this.router.navigate(['/system/users']);
      return;
    }

    this.userId = id;
    this.load();
  }

  load(): void {
    this.loading = true;
    forkJoin({
      user: this.userService.getUserById(this.userId),
      all: this.roleService.loadRoles({ pageIndex: 1, pageSize: 200 }),
      userRoles: this.roleService.getUserRoles(this.userId)
    }).pipe(finalize(() => (this.loading = false))).subscribe({
      next: ({ user, all, userRoles }) => {
        this.userName = user.userName;
        this.allRoles = all.items;
        this.selectedRoleIds = userRoles.map(r => r.id);
      },
      error: (err: unknown) => {
        this.message.error(err instanceof ApiBusinessError ? err.message : '加载失败');
        void this.router.navigate(['/system/users']);
      }
    });
  }

  isRoleSelected(roleId: number): boolean {
    return this.selectedRoleIds.includes(roleId);
  }

  toggleRole(roleId: number, checked: boolean): void {
    if (checked) {
      if (!this.selectedRoleIds.includes(roleId)) {
        this.selectedRoleIds = [...this.selectedRoleIds, roleId];
      }
    } else {
      this.selectedRoleIds = this.selectedRoleIds.filter(id => id !== roleId);
    }
  }

  submit(): void {
    this.saving = true;
    this.roleService.assignUserRoles(this.userId, { roleIds: this.selectedRoleIds }).pipe(
      finalize(() => (this.saving = false))
    ).subscribe({
      next: () => {
        this.message.success('角色分配成功');
        void this.router.navigate(['/system/users']);
      },
      error: (err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '分配失败')
    });
  }
}
