import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { CalendarApiService } from '../../data-access/calendar-api.service';
import { CalendarEntry, CalendarShare } from '../../models/calendar.model';
import { MyPlantsApiService } from '../../../my-plants/data-access/my-plants-api.service';
import { CareActionType } from '../../../my-plants/models/user-plant.model';
import { ActivatedRoute } from '@angular/router';
import { ExternalCalendarStatus } from '../../models/calendar.model';

interface CalendarDay { date: Date; isCurrentMonth: boolean; entries: CalendarEntry[]; }

@Component({
  selector: 'app-calendar-page', standalone: true, imports: [DatePipe, FormsModule],
  templateUrl: './calendar-page.html', styleUrl: './calendar-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarPage {
  private readonly api = inject(CalendarApiService);
  private readonly myPlantsApi = inject(MyPlantsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  readonly visibleMonth = signal(this.startOfMonth(new Date()));
  readonly entries = signal<CalendarEntry[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedPlantId = signal('all');
  readonly selectedKind = signal('All');
  readonly selectedEntry = signal<CalendarEntry | null>(null);
  readonly completingEntryId = signal<string | null>(null);
  readonly subscriptionActive = signal(false);
  readonly subscriptionCreatedAt = signal<string | null>(null);
  readonly subscriptionUrl = signal<string | null>(null);
  readonly subscriptionBusy = signal(false);
  readonly subscriptionMessage = signal<string | null>(null);
  readonly shares = signal<CalendarShare[]>([]);
  readonly viewedShare = signal<CalendarShare | null>(null);
  readonly recipientEmail = signal('');
  readonly sharingBusy = signal(false);
  readonly sharingMessage = signal<string | null>(null);
  readonly googleStatus = signal<ExternalCalendarStatus | null>(null);
  readonly googleBusy = signal(false);
  readonly googleMessage = signal<string | null>(null);
  readonly incomingShares = computed(() => this.shares().filter(share => !share.isOwnedByCurrentUser));
  readonly outgoingShares = computed(() => this.shares().filter(share => share.isOwnedByCurrentUser));
  readonly plants = computed(() => Array.from(
    new Map(this.entries().map(entry => [entry.userPlantId, entry.plantName])).entries(),
  ).map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name)));
  readonly filteredEntries = computed(() => this.entries().filter(entry =>
    (this.selectedPlantId() === 'all' || entry.userPlantId === this.selectedPlantId()) &&
    (this.selectedKind() === 'All' || entry.kind === this.selectedKind()),
  ));
  readonly days = computed(() => this.buildDays(this.visibleMonth(), this.filteredEntries()));

  constructor() {
    this.load();
    this.loadSubscriptionStatus();
    this.loadShares();
    this.loadGoogleStatus();

    const googleResult = this.route.snapshot.queryParamMap.get('google');
    if (googleResult === 'connected') this.googleMessage.set('Google Calendar connected. Synchronize now to export upcoming care events.');
    if (googleResult === 'cancelled') this.googleMessage.set('Google Calendar connection was cancelled.');
    if (googleResult === 'error') this.googleMessage.set('Google Calendar could not be connected. Please try again.');
  }

  changeMonth(offset: number): void {
    const current = this.visibleMonth();
    this.visibleMonth.set(new Date(current.getFullYear(), current.getMonth() + offset, 1));
    this.load();
  }

  goToToday(): void { this.visibleMonth.set(this.startOfMonth(new Date())); this.load(); }

  selectEntry(entry: CalendarEntry): void { this.selectedEntry.set(entry); }

  closeDetails(): void { this.selectedEntry.set(null); }

  canComplete(entry: CalendarEntry): boolean {
    return this.viewedShare() === null && entry.kind === 'Scheduled' && new Date(entry.startsAtUtc).getTime() <= Date.now();
  }

  completeCare(entry: CalendarEntry): void {
    if (!this.canComplete(entry) || this.completingEntryId() !== null) return;
    this.completingEntryId.set(entry.id); this.errorMessage.set(null);
    this.myPlantsApi.completeCareAction(entry.userPlantId, entry.actionType as CareActionType).pipe(
      finalize(() => this.completingEntryId.set(null)), takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => { this.selectedEntry.set(null); this.load(); },
      error: () => this.errorMessage.set('The care action could not be completed.'),
    });
  }

  createSubscription(): void {
    if (this.subscriptionBusy()) return;
    this.subscriptionBusy.set(true); this.subscriptionMessage.set(null);
    this.api.createSubscription().pipe(
      finalize(() => this.subscriptionBusy.set(false)), takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => { this.subscriptionActive.set(true); this.subscriptionCreatedAt.set(result.createdAtUtc); this.subscriptionUrl.set(result.subscriptionUrl); },
      error: () => this.subscriptionMessage.set('The subscription link could not be created.'),
    });
  }

  revokeSubscription(): void {
    if (this.subscriptionBusy()) return;
    this.subscriptionBusy.set(true); this.subscriptionMessage.set(null);
    this.api.revokeSubscription().pipe(
      finalize(() => this.subscriptionBusy.set(false)), takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => { this.subscriptionActive.set(false); this.subscriptionCreatedAt.set(null); this.subscriptionUrl.set(null); this.subscriptionMessage.set('The subscription link has been revoked.'); },
      error: () => this.subscriptionMessage.set('The subscription link could not be revoked.'),
    });
  }

  async copySubscriptionUrl(): Promise<void> {
    const url = this.subscriptionUrl(); if (!url) return;
    try { await navigator.clipboard.writeText(url); this.subscriptionMessage.set('Subscription link copied.'); }
    catch { this.subscriptionMessage.set('Copy failed. Select and copy the link manually.'); }
  }

  viewSharedCalendar(share: CalendarShare): void { this.viewedShare.set(share); this.selectedEntry.set(null); this.load(); }
  viewOwnCalendar(): void { this.viewedShare.set(null); this.selectedEntry.set(null); this.load(); }

  createShare(): void {
    const email = this.recipientEmail().trim(); if (!email || this.sharingBusy()) return;
    this.sharingBusy.set(true); this.sharingMessage.set(null);
    this.api.createShare(email).pipe(finalize(() => this.sharingBusy.set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.recipientEmail.set(''); this.sharingMessage.set('Calendar shared.'); this.loadShares(); },
      error: () => this.sharingMessage.set('The calendar could not be shared. Check that the account exists and does not already have access.'),
    });
  }

  revokeShare(share: CalendarShare): void {
    if (this.sharingBusy()) return;
    this.sharingBusy.set(true); this.sharingMessage.set(null);
    this.api.revokeShare(share.id).pipe(finalize(() => this.sharingBusy.set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.sharingMessage.set('Shared access revoked.'); this.loadShares(); },
      error: () => this.sharingMessage.set('Shared access could not be revoked.'),
    });
  }

  connectGoogle(): void {
    if (this.googleBusy()) return;
    this.googleBusy.set(true);
    this.googleMessage.set(null);
    this.api.connectGoogle().pipe(
      finalize(() => this.googleBusy.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => window.location.assign(result.authorizationUrl),
      error: () => this.googleMessage.set('Google Calendar authorization could not be started.'),
    });
  }

  syncGoogle(): void {
    if (this.googleBusy()) return;
    this.googleBusy.set(true);
    this.googleMessage.set(null);
    this.api.syncGoogle().pipe(
      finalize(() => this.googleBusy.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => {
        this.googleMessage.set(`Synchronization complete: ${result.created} created, ${result.updated} updated, ${result.deleted} removed.`);
        this.loadGoogleStatus();
      },
      error: () => this.googleMessage.set('Google Calendar synchronization failed. Reconnect the account if access was revoked.'),
    });
  }

  disconnectGoogle(): void {
    if (this.googleBusy()) return;
    this.googleBusy.set(true);
    this.googleMessage.set(null);
    this.api.disconnectGoogle().pipe(
      finalize(() => this.googleBusy.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.googleMessage.set('Google Calendar disconnected. Existing exported events remain in Google.');
        this.loadGoogleStatus();
      },
      error: () => this.googleMessage.set('Google Calendar could not be disconnected.'),
    });
  }

  private load(): void {
    const month = this.visibleMonth();
    const from = new Date(month.getFullYear(), month.getMonth(), 1);
    const to = new Date(month.getFullYear(), month.getMonth() + 1, 1);
    this.isLoading.set(true); this.errorMessage.set(null);
    const request = this.viewedShare()
      ? this.api.getSharedEntries(this.viewedShare()!.id, from, to)
      : this.api.getEntries(from, to);
    request.pipe(
      finalize(() => this.isLoading.set(false)), takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: entries => { this.entries.set(entries); this.selectedEntry.set(null); },
      error: () => this.errorMessage.set('The calendar could not be loaded.'),
    });
  }

  private loadSubscriptionStatus(): void {
    this.api.getSubscriptionStatus().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: status => { this.subscriptionActive.set(status.isActive); this.subscriptionCreatedAt.set(status.createdAtUtc); },
      error: () => this.subscriptionMessage.set('Calendar subscription status could not be loaded.'),
    });
  }

  private loadShares(): void {
    this.api.getShares().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: shares => this.shares.set(shares),
      error: () => this.sharingMessage.set('Shared calendars could not be loaded.'),
    });
  }

  private loadGoogleStatus(): void {
    this.api.getGoogleStatus().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: status => this.googleStatus.set(status),
      error: () => this.googleMessage.set('Google Calendar status could not be loaded.'),
    });
  }

  private buildDays(month: Date, entries: CalendarEntry[]): CalendarDay[] {
    const first = new Date(month.getFullYear(), month.getMonth(), 1);
    const gridStart = new Date(first); gridStart.setDate(first.getDate() - first.getDay());
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(gridStart); date.setDate(gridStart.getDate() + index);
      return { date, isCurrentMonth: date.getMonth() === month.getMonth(), entries: entries.filter(entry => {
        const eventDate = new Date(entry.startsAtUtc);
        return eventDate.getFullYear() === date.getFullYear() && eventDate.getMonth() === date.getMonth() && eventDate.getDate() === date.getDate();
      }) };
    });
  }

  private startOfMonth(date: Date): Date { return new Date(date.getFullYear(), date.getMonth(), 1); }
}
