import { Directive, Input, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from '../../service/auth.service';

@Directive({
  selector: '[appHasPermission]',
  standalone: true
})
export class HasPermissionDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly auth = inject(AuthService);

  private permissionCode = '';
  private hasView = false;

  @Input()
  set appHasPermission(code: string) {
    this.permissionCode = code;
    this.updateView();
  }

  constructor() {
    effect(() => {
      this.auth.user();
      this.updateView();
    });
  }

  private updateView(): void {
    const allowed = !this.permissionCode || this.auth.hasPermission(this.permissionCode);
    if (allowed && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!allowed && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }
}
