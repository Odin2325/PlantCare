import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { NotificationsApiService } from '../../data-access/notifications-api.service';
import { CareNotification, NotificationPreference } from '../../models/notification.model';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';

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
  private readonly swPush = inject(SwPush);
  readonly notifications = signal<CareNotification[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly pushAvailable = signal(false);
  readonly pushEnabled = signal(false);
  readonly pushBusy = signal(false);
  readonly pushMessage = signal<string | null>(null);
  readonly preferences = signal<NotificationPreference | null>(null);
  readonly preferencesBusy = signal(false);
  readonly preferencesMessage = signal<string | null>(null);
  private pushPublicKey = '';

  constructor() { this.load(); this.loadPushStatus(); this.loadPreferences(); }

  setPreference(
    key: Exclude<keyof NotificationPreference, 'reminderLeadTimeHours'>,
    value: boolean,
  ): void {
    this.preferences.update(current => current ? { ...current, [key]: value } : current);
  }

  setLeadTime(value: number): void {
    this.preferences.update(current => current ? { ...current, reminderLeadTimeHours: value } : current);
  }

  savePreferences(): void {
    const preferences = this.preferences();
    if (!preferences || this.preferencesBusy()) return;
    this.preferencesBusy.set(true);
    this.preferencesMessage.set(null);
    this.api.updatePreferences(preferences).pipe(
      finalize(() => this.preferencesBusy.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: saved => {
        this.preferences.set(saved);
        this.preferencesMessage.set('Notification preferences saved.');
        this.load();
      },
      error: () => this.preferencesMessage.set('Notification preferences could not be saved.'),
    });
  }

  markRead(notification: CareNotification): void {
    if (notification.readAtUtc) return;
    this.api.markRead(notification.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.notifications.update(items => items.map(item =>
        item.id === notification.id ? { ...item, readAtUtc: new Date().toISOString() } : item)),
      error: () => this.errorMessage.set('The notification could not be marked as read.'),
    });
  }

  async enablePush(): Promise<void> {
    if (!this.pushAvailable() || this.pushBusy()) return;
    this.pushBusy.set(true); this.pushMessage.set(null);
    try {
      const browserSubscription = await this.swPush.requestSubscription({ serverPublicKey: this.pushPublicKey });
      const json = browserSubscription.toJSON();
      const saved = await firstValueFrom(this.api.savePushSubscription({ endpoint: browserSubscription.endpoint, p256dh: json.keys?.['p256dh'] ?? '', auth: json.keys?.['auth'] ?? '' }));
      localStorage.setItem('plantcare.pushSubscriptionId', saved.id); this.pushEnabled.set(true); this.pushMessage.set('Push reminders are enabled on this device.');
    } catch { this.pushMessage.set(Notification.permission === 'denied' ? 'Notifications are blocked in your browser settings.' : 'Push reminders could not be enabled.'); }
    finally { this.pushBusy.set(false); }
  }

  async disablePush(): Promise<void> {
    if (this.pushBusy()) return;
    this.pushBusy.set(true); this.pushMessage.set(null);
    try {
      const subscription = await firstValueFrom(this.swPush.subscription);
      if (subscription) await subscription.unsubscribe();
      const id = localStorage.getItem('plantcare.pushSubscriptionId');
      if (id) await firstValueFrom(this.api.removePushSubscription(id));
      localStorage.removeItem('plantcare.pushSubscriptionId'); this.pushEnabled.set(false); this.pushMessage.set('Push reminders are disabled on this device.');
    } catch { this.pushMessage.set('Push reminders could not be disabled completely.'); }
    finally { this.pushBusy.set(false); }
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

  private loadPushStatus(): void {
    this.api.getPushConfig().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: async config => {
        this.pushPublicKey = config.publicKey; this.pushAvailable.set(config.isEnabled && this.swPush.isEnabled);
        if (this.swPush.isEnabled) this.pushEnabled.set((await firstValueFrom(this.swPush.subscription)) !== null);
      },
      error: () => this.pushMessage.set('Push notification settings could not be loaded.'),
    });
  }

  private loadPreferences(): void {
    this.api.getPreferences().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: preferences => this.preferences.set(preferences),
      error: () => this.preferencesMessage.set('Notification preferences could not be loaded.'),
    });
  }
}
