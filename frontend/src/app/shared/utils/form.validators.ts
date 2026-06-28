import { AbstractControl, ValidationErrors, Validators } from '@angular/forms';

/** 邮箱可选，有值时才校验格式 */
export function optionalEmailValidator(control: AbstractControl): ValidationErrors | null {
  const value = (control.value as string | null | undefined)?.trim();
  if (!value) {
    return null;
  }

  return Validators.email(control);
}
