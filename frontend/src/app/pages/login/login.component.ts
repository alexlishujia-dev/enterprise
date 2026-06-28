import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { finalize } from 'rxjs';
import { ApiBusinessError } from '../../core/models/api-result.model';
import { AuthService } from '../../service/auth.service';
import { APP_CONSTANTS } from '../../core/config/app.constants';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    NzFormModule,
    NzInputModule,
    NzButtonModule,
    NzSpinModule,
    NzIconModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly message = inject(NzMessageService);

  readonly appTitle = APP_CONSTANTS.appTitle;
  loading = false;

  form = this.fb.nonNullable.group({
    userName: ['admin', [Validators.required, Validators.maxLength(64)]],
    password: ['Admin@123', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.auth.login(this.form.getRawValue()).pipe(
      finalize(() => (this.loading = false))
    ).subscribe({
      next: () => {
        this.message.success('登录成功');
        void this.router.navigate(['/dashboard']);
      },
      error: (err: unknown) => {
        const msg = err instanceof ApiBusinessError ? err.message : '登录失败';
        this.message.error(msg);
      }
    });
  }
}
