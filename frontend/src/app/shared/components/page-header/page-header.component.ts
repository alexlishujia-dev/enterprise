import { Component, Input } from '@angular/core';
import { NzPageHeaderModule } from 'ng-zorro-antd/page-header';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [NzPageHeaderModule],
  template: `
    <nz-page-header [nzTitle]="title" [nzSubtitle]="subtitle">
      <ng-content></ng-content>
    </nz-page-header>
  `
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle = '';
}
