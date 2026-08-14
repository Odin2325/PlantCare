import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from './core/auth/services/auth.service';

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

  private readonly router = inject(Router);

  readonly isLoggingOut = signal(false);

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
