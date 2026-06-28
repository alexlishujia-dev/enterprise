import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { finalize } from 'rxjs';
import { ApiBusinessError } from '../../core/models/api-result.model';
import { SysRoleService } from '../../service/sys-role.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-role-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    NzCardModule,
    NzFormModule,
    NzInputModule,
    NzButtonModule,
    NzSpaceModule,
    NzSwitchModule
  ],
  templateUrl: './role-form.component.html',
  styleUrl: './system-pages.scss'
})
export class RoleFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly roleService = inject(SysRoleService);
  private readonly message = inject(NzMessageService);
  private readonly fb = inject(FormBuilder);

  isEdit = false;
  roleId: number | null = null;
  loading = false;
  saving = false;

  form = this.fb.nonNullable.group({
    roleCode: ['', [Validators.required, Validators.maxLength(64)]],
    roleName: ['', [Validators.required, Validators.maxLength(64)]],
    description: [''],
    isActive: [true]
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit = true;
      this.roleId = Number(idParam);
      this.form.controls.roleCode.disable();
      this.loadRole(this.roleId);
    }
  }

  get pageTitle(): string {
    return this.isEdit ? '编辑角色' : '新建角色';
  }

  loadRole(id: number): void {
    this.loading = true;
    this.roleService.getRoleById(id).pipe(
      finalize(() => (this.loading = false))
    ).subscribe({
      next: role => {
        this.form.reset({
          roleCode: role.roleCode,
          roleName: role.roleName,
          description: role.description ?? '',
          isActive: role.isActive
        });
      },
      error: (err: unknown) => {
        this.message.error(err instanceof ApiBusinessError ? err.message : '加载失败');
        void this.router.navigate(['/system/roles']);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.message.warning('请完善必填项');
      return;
    }

    this.saving = true;
    const raw = this.form.getRawValue();
    const request$ = this.isEdit && this.roleId != null
      ? this.roleService.updateRole(this.roleId, {
          roleName: raw.roleName,
          description: raw.description,
          isActive: raw.isActive
        })
      : this.roleService.createRole({
          roleCode: raw.roleCode,
          roleName: raw.roleName,
          description: raw.description
        });

    request$.pipe(finalize(() => (this.saving = false))).subscribe({
      next: () => {
        this.message.success(this.isEdit ? '更新成功' : '创建成功');
        void this.router.navigate(['/system/roles']);
      },
      error: (err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '保存失败')
    });
  }
}
