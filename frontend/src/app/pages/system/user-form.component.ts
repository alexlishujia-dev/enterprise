import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpaceModule } from 'ng-zorro-antd/space';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzUploadFile, NzUploadModule } from 'ng-zorro-antd/upload';
import { finalize } from 'rxjs';
import { ApiBusinessError } from '../../core/models/api-result.model';
import { FileService } from '../../service/file.service';
import { SysUserService } from '../../service/sys-user.service';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { resolveAssetUrl } from '../../shared/utils/asset-url.util';
import { optionalEmailValidator } from '../../shared/utils/form.validators';

@Component({
  selector: 'app-user-form',
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
    NzSwitchModule,
    NzUploadModule,
    NzAvatarModule,
    NzIconModule
  ],
  templateUrl: './user-form.component.html',
  styleUrl: './system-pages.scss'
})
export class UserFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(SysUserService);
  private readonly fileService = inject(FileService);
  private readonly message = inject(NzMessageService);
  private readonly fb = inject(FormBuilder);

  isEdit = false;
  userId: number | null = null;
  loading = false;
  saving = false;
  uploadingAvatar = false;
  avatarUrl: string | undefined;
  avatarFileList: NzUploadFile[] = [];

  form = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.maxLength(64)]],
    password: [''],
    displayName: [''],
    email: ['', optionalEmailValidator],
    isActive: [true]
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit = true;
      this.userId = Number(idParam);
      this.form.controls.userName.disable();
      this.form.controls.password.setValidators([Validators.minLength(6)]);
      this.loadUser(this.userId);
    } else {
      this.form.controls.password.setValidators([Validators.required, Validators.minLength(6)]);
    }
  }

  get pageTitle(): string {
    return this.isEdit ? '编辑用户' : '新建用户';
  }

  loadUser(id: number): void {
    this.loading = true;
    this.userService.getUserById(id).pipe(
      finalize(() => (this.loading = false))
    ).subscribe({
      next: user => {
        if (!user) {
          this.message.error('用户不存在');
          void this.router.navigate(['/system/users']);
          return;
        }

        this.setAvatar(user.avatarUrl);
        this.form.reset({
          userName: user.userName,
          password: '',
          displayName: user.displayName ?? '',
          email: user.email ?? '',
          isActive: user.isActive
        });
      },
      error: (err: unknown) => {
        this.message.error(err instanceof ApiBusinessError ? err.message : '加载失败');
        void this.router.navigate(['/system/users']);
      }
    });
  }

  beforeAvatarUpload = (file: NzUploadFile): boolean => {
    const rawFile = (file.originFileObj ?? file) as File;
    if (!rawFile?.type?.startsWith('image/')) {
      this.message.error('只能上传图片文件');
      return false;
    }

    if (rawFile.size / 1024 / 1024 >= 2) {
      this.message.error('头像大小不能超过 2MB');
      return false;
    }

    this.uploadingAvatar = true;
    this.fileService.uploadAvatar(rawFile).pipe(
      finalize(() => (this.uploadingAvatar = false))
    ).subscribe({
      next: url => {
        this.setAvatar(url);
        this.message.success('头像上传成功');
      },
      error: (err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '头像上传失败')
    });

    return false;
  };

  removeAvatar = (): boolean => {
    this.avatarUrl = undefined;
    this.avatarFileList = [];
    return true;
  };

  submit(): void {
    this.form.controls.password.updateValueAndValidity();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.message.warning('请完善必填项');
      return;
    }

    this.saving = true;
    const raw = this.form.getRawValue();
    const displayName = raw.displayName?.trim();
    const email = raw.email?.trim();
    const password = raw.password?.trim();

    const request$ = this.isEdit && this.userId != null
      ? this.userService.updateUser(this.userId, {
          displayName: displayName || undefined,
          email: email || undefined,
          isActive: raw.isActive,
          password: password || undefined,
          avatarUrl: this.avatarUrl
        })
      : this.userService.createUser({
          userName: raw.userName.trim(),
          password: raw.password,
          displayName: displayName || undefined,
          email: email || undefined,
          avatarUrl: this.avatarUrl
        });

    request$.pipe(finalize(() => (this.saving = false))).subscribe({
      next: () => {
        this.message.success(this.isEdit ? '更新成功' : '创建成功');
        void this.router.navigate(['/system/users']);
      },
      error: (err: unknown) => this.message.error(err instanceof ApiBusinessError ? err.message : '保存失败')
    });
  }

  private setAvatar(url?: string | null): void {
    this.avatarUrl = url || undefined;
    if (!url) {
      this.avatarFileList = [];
      return;
    }

    const displayUrl = resolveAssetUrl(url);
    this.avatarFileList = [{
      uid: '-1',
      name: 'avatar',
      status: 'done',
      url: displayUrl,
      thumbUrl: displayUrl
    }];
  }
}
