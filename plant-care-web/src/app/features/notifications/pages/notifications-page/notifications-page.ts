import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { NotificationsApiService } from '../../data-access/notifications-api.service';
import { CareNotification } from '../../models/notification.model';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './notifications-page.html',
  styleUrl: './notifications-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage {
  private readonly api = inject(NotificationsApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly notifications = signal<CareNotification[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor() { this.load(); }

  markRead(notification: CareNotification): void {
    if (notification.readAtUtc) return;
    this.api.markRead(notification.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.notifications.update(items => items.map(item =>
        item.id === notification.id ? { ...item, readAtUtc: new Date().toISOString() } : item)),
      error: () => this.errorMessage.set('The notification could not be marked as read.'),
    });
  }

  private load(): void {
    this.api.getNotifications().pipe(
      finalize(() => this.isLoading.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: notifications => this.notifications.set(notifications),
      error: () => this.errorMessage.set('Notifications could not be loaded.'),
    });
  }
}
