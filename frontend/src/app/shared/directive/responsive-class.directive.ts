import { Directive, ElementRef, Input, OnDestroy, OnInit, Renderer2 } from '@angular/core';
import { Subscription } from 'rxjs';
import { ResponsiveService, ScreenSize } from '../utils/responsive.service';

@Directive({
  selector: '[appResponsiveClass]',
  standalone: true
})
export class ResponsiveClassDirective implements OnInit, OnDestroy {
  @Input('appResponsiveClass') classMap: Partial<Record<ScreenSize, string>> = {};

  private sub?: Subscription;

  constructor(
    private readonly el: ElementRef<HTMLElement>,
    private readonly renderer: Renderer2,
    private readonly responsive: ResponsiveService
  ) {}

  ngOnInit(): void {
    this.sub = this.responsive.observe().subscribe(size => this.apply(size));
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private apply(size: ScreenSize): void {
    Object.values(this.classMap).forEach(cls => {
      if (cls) this.renderer.removeClass(this.el.nativeElement, cls);
    });
    const cls = this.classMap[size];
    if (cls) this.renderer.addClass(this.el.nativeElement, cls);
  }
}
