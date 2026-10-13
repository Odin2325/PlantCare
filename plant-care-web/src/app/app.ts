import {
  ChangeDetectionStrategy,
  Component,
  afterNextRender,
  Injector,
  inject,
  signal,
} from '@angular/core';
import {
  Router,
  NavigationEnd,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter, finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService } from './core/auth/services/auth.service';
import { PwaUpdateService } from './core/pwa/pwa-update.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  readonly authService = inject(AuthService);
  readonly pwa = inject(PwaUpdateService);

  private readonly router = inject(Router);

  readonly isLoggingOut = signal(false);

  constructor() {
    const injector = inject(Injector);
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntilDestroyed(),
    ).subscribe(() => afterNextRender(() => {
      const main = document.getElementById('main-content');
      const target = main?.querySelector('h1') ?? main;
      if (target instanceof HTMLElement) { target.tabIndex = -1; target.focus(); }
    }, { injector }));
  }

  skipToContent(event: Event): void {
    event.preventDefault();
    document.getElementById('main-content')?.focus();
  }

  logout(): void {
    if (this.isLoggingOut()) {
      return;
    }

    this.isLoggingOut.set(true);

    this.authService
      .logout()
      .pipe(
        finalize(() => {
          this.isLoggingOut.set(false);
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigate(['/login']);
        },
        error: (error: unknown) => {
          console.error('Logout failed.', error);
        },
      });
  }
}
