import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { CareNotification } from '../../features/notifications/models/notification.model';
import { NotificationsApiService } from '../../features/notifications/data-access/notifications-api.service';

@Component({
  selector: 'app-reminder-snooze', standalone: true, imports: [DatePipe],
  template: `
    @if (current().snoozedUntilUtc; as until) {
      <p role="status">Reminder snoozed until {{ until | date:'medium' }}. Care is still due.</p>
    } @else if (current().canSnooze) {
      <div class="snooze-controls" role="group" [attr.aria-label]="'Snooze ' + current().actionType + ' reminder for ' + current().plantName">
        <span>Remind me in</span>
        @for (choice of choices; track choice.hours) {
          <button type="button" [disabled]="busy()" (click)="snooze(choice.hours)">{{ choice.label }}</button>
        }
      </div>
    }
    @if (busy()) { <p role="status">Saving reminder…</p> }
    @if (error()) { <p role="alert">{{ error() }}</p> }
  `,
  styles: `:host{display:block;min-width:0}.snooze-controls{display:flex;flex-wrap:wrap;gap:.5rem;align-items:center}button{font:inherit;padding:.5rem .75rem;border:1px solid #377a4b;border-radius:.5rem;background:white;color:#235b37;cursor:pointer}button:focus-visible{outline:3px solid #235b37;outline-offset:2px}button:disabled{opacity:.6;cursor:wait}p{overflow-wrap:anywhere;line-height:1.5}`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReminderSnooze {
  private readonly api = inject(NotificationsApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly notification = input.required<CareNotification>();
  readonly snoozed = output<void>();
  private readonly saved = signal<CareNotification | null>(null);
  readonly current = computed(() => this.saved() ?? this.notification());
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly choices = [{hours: 1, label: '1 hour'}, {hours: 24, label: '24 hours'}, {hours: 72, label: '3 days'}];

  constructor() { effect(() => { this.notification(); this.saved.set(null); }); }

  snooze(hours: number): void {
    if (this.busy() || !this.current().canSnooze) return;
    this.busy.set(true); this.error.set(null);
    this.api.snooze(this.current().id, hours).pipe(finalize(() => this.busy.set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: notification => { this.saved.set(notification); this.snoozed.emit(); },
      error: (error: HttpErrorResponse) => this.error.set(error.status === 409
        ? 'This reminder changed. Reload to see current care.'
        : 'The reminder could not be snoozed. Check your connection and try again.'),
    });
  }
}
