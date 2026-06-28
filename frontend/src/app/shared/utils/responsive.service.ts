import { Injectable, signal } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, shareReplay, startWith } from 'rxjs/operators';

export type ScreenSize = 'mobile' | 'tablet' | 'desktop';

@Injectable({ providedIn: 'root' })
export class ResponsiveService {
  private readonly breakpoint$ = new BehaviorSubject<ScreenSize>(this.detect());

  readonly screenSize = signal<ScreenSize>(this.detect());
  readonly isMobile$ = this.breakpoint$.pipe(
    map(size => size === 'mobile'),
    shareReplay(1)
  );

  constructor() {
    if (typeof window !== 'undefined') {
      window.addEventListener('resize', () => this.update());
      this.update();
    }
  }

  observe(): Observable<ScreenSize> {
    return this.breakpoint$.asObservable().pipe(startWith(this.detect()));
  }

  isMobile(): boolean {
    return this.detect() === 'mobile';
  }

  private update(): void {
    const size = this.detect();
    this.screenSize.set(size);
    this.breakpoint$.next(size);
  }

  private detect(): ScreenSize {
    if (typeof window === 'undefined') return 'desktop';
    const width = window.innerWidth;
    if (width < 768) return 'mobile';
    if (width < 1200) return 'tablet';
    return 'desktop';
  }
}
