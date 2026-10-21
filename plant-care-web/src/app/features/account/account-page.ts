import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DestroyRef } from '@angular/core';
import { AuthService } from '../../core/auth/services/auth.service';

@Component({
  standalone: true, imports: [FormsModule, RouterLink],
  templateUrl: './account-page.html', styleUrl: './account-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountPage {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  password = '';
  confirmation = '';
  readonly busy = signal(false);
  readonly message = signal('');
  readonly failed = signal(false);

  exportData(): void {
    if (this.busy() || !this.password) return;
    const password = this.password;
    this.password = ''; this.begin();
    this.http.post('/api/account/export', { password }, { responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
      .subscribe({ next: data => {
        const url = URL.createObjectURL(data);
        const link = document.createElement('a');
        link.href = url; link.download = 'plantcare-account.json'; link.click();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        this.message.set('Export downloaded. Store it privately; it contains your personal plant and account data.');
      }, error: () => this.error('Export failed. Check your current password, or wait and try again. Sign in again if your session expired.') });
  }

  deleteAccount(): void {
    if (this.busy() || !this.password || this.confirmation !== 'DELETE') return;
    const password = this.password;
    this.password = ''; this.begin();
    this.http.post('/api/account/delete', { password, confirmation: this.confirmation })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
      .subscribe({ next: () => {
        this.auth.clearDeletedAccount();
        void this.router.navigateByUrl('/login?accountDeleted=true');
      }, error: (error: HttpErrorResponse) => this.error(error.status === 409
        ? 'Disconnect external calendars on the Calendar page before deleting your account.'
        : 'Deletion failed. Check your current password, or wait and try again. Sign in again if your session expired.') });
  }

  private begin(): void { this.busy.set(true); this.failed.set(false); this.message.set(''); }
  private error(message: string): void { this.failed.set(true); this.message.set(message); }
}
