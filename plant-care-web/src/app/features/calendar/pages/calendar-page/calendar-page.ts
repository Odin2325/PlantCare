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
type CalendarView = 'Month' | 'Week' | 'Agenda';

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
  readonly visibleDate = signal(new Date());
  readonly calendarView = signal<CalendarView>(this.loadSavedView());
  readonly entries = signal<CalendarEntry[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedPlantId = signal('all');
  readonly selectedKind = signal('All');
  readonly selectedAction = signal('All');
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
  readonly microsoftStatus = signal<ExternalCalendarStatus | null>(null);
  readonly microsoftBusy = signal(false);
  readonly microsoftMessage = signal<string | null>(null);
  readonly incomingShares = computed(() => this.shares().filter(share => !share.isOwnedByCurrentUser));
  readonly outgoingShares = computed(() => this.shares().filter(share => share.isOwnedByCurrentUser));
  readonly plants = computed(() => Array.from(
    new Map(this.entries().map(entry => [entry.userPlantId, entry.plantName])).entries(),
  ).map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name)));
  readonly actions = computed(() => Array.from(
    new Set(this.entries().map(entry => entry.actionType)),
  ).sort((a, b) => a.localeCompare(b)));
  readonly filteredEntries = computed(() => this.entries().filter(entry =>
    (this.selectedPlantId() === 'all' || entry.userPlantId === this.selectedPlantId()) &&
    (this.selectedKind() === 'All' || entry.kind === this.selectedKind()) &&
    (this.selectedAction() === 'All' || entry.actionType === this.selectedAction()),
  ));
  readonly monthDays = computed(() => this.buildMonthDays(
    this.visibleDate(),
    this.filteredEntries(),
  ));
  readonly weekDays = computed(() => this.buildRangeDays(
    this.startOfWeek(this.visibleDate()),
    7,
    this.filteredEntries(),
    this.visibleDate().getMonth(),
  ));
  readonly agendaDays = computed(() => {
    const month = this.visibleDate();
    const start = this.startOfMonth(month);
    const end = new Date(start.getFullYear(), start.getMonth() + 1, 1);
    return this.buildRangeDays(
      start,
      Math.round((end.getTime() - start.getTime()) / 86_400_000),
      this.filteredEntries(),
      month.getMonth(),
    ).filter(day => day.entries.length > 0);
  });
  readonly periodTitle = computed(() => {
    const date = this.visibleDate();
    if (this.calendarView() !== 'Week')
      return new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' }).format(date);

    const start = this.startOfWeek(date);
    const end = new Date(start);
    end.setDate(start.getDate() + 6);
    const startText = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(start);
    const endText = new Intl.DateTimeFormat(undefined, {
      month: start.getMonth() === end.getMonth() ? undefined : 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(end);
    return `${startText} – ${endText}`;
  });

  constructor() {
    this.load();
    this.loadSubscriptionStatus();
    this.loadShares();
    this.loadGoogleStatus();
    this.loadMicrosoftStatus();

    const googleResult = this.route.snapshot.queryParamMap.get('google');
    if (googleResult === 'connected') this.googleMessage.set('Google Calendar connected. Synchronize now to export upcoming care events.');
    if (googleResult === 'cancelled') this.googleMessage.set('Google Calendar connection was cancelled.');
    if (googleResult === 'error') this.googleMessage.set('Google Calendar could not be connected. Please try again.');
    const microsoftResult = this.route.snapshot.queryParamMap.get('microsoft');
    if (microsoftResult === 'connected') this.microsoftMessage.set('Microsoft Calendar connected. Synchronize now to export upcoming care events.');
    if (microsoftResult === 'cancelled') this.microsoftMessage.set('Microsoft Calendar connection was cancelled.');
    if (microsoftResult === 'error') this.microsoftMessage.set('Microsoft Calendar could not be connected. Please try again.');
  }

  changePeriod(offset: number): void {
    const current = this.visibleDate();
    const next = new Date(current);
    if (this.calendarView() === 'Week') next.setDate(current.getDate() + (offset * 7));
    else next.setMonth(current.getMonth() + offset, 1);
    this.visibleDate.set(next);
    this.load();
  }

  goToToday(): void { this.visibleDate.set(new Date()); this.load(); }

  setCalendarView(view: CalendarView): void {
    if (this.calendarView() === view) return;
    this.calendarView.set(view);
    localStorage.setItem('plantcare.calendarView', view);
    this.selectedEntry.set(null);
    this.load();
  }

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

  connectMicrosoft(): void {
    if (this.microsoftBusy()) return;
    this.microsoftBusy.set(true);
    this.microsoftMessage.set(null);
    this.api.connectMicrosoft().pipe(
      finalize(() => this.microsoftBusy.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => window.location.assign(result.authorizationUrl),
      error: () => this.microsoftMessage.set('Microsoft Calendar authorization could not be started.'),
    });
  }

  syncMicrosoft(): void {
    if (this.microsoftBusy()) return;
    this.microsoftBusy.set(true);
    this.microsoftMessage.set(null);
    this.api.syncMicrosoft().pipe(
      finalize(() => this.microsoftBusy.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: result => {
        this.microsoftMessage.set(`Synchronization complete: ${result.created} created, ${result.updated} updated, ${result.deleted} removed.`);
        this.loadMicrosoftStatus();
      },
      error: () => this.microsoftMessage.set('Microsoft Calendar synchronization failed. Reconnect the account if access was revoked.'),
    });
  }

  disconnectMicrosoft(): void {
    if (this.microsoftBusy()) return;
    this.microsoftBusy.set(true);
    this.microsoftMessage.set(null);
    this.api.disconnectMicrosoft().pipe(
      finalize(() => this.microsoftBusy.set(false)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.microsoftMessage.set('Microsoft Calendar disconnected. Existing exported events remain in Microsoft Calendar.');
        this.loadMicrosoftStatus();
      },
      error: () => this.microsoftMessage.set('Microsoft Calendar could not be disconnected.'),
    });
  }

  private load(): void {
    const { from, to } = this.getVisibleRange();
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

  private loadMicrosoftStatus(): void {
    this.api.getMicrosoftStatus().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: status => this.microsoftStatus.set(status),
      error: () => this.microsoftMessage.set('Microsoft Calendar status could not be loaded.'),
    });
  }

  private getVisibleRange(): { from: Date; to: Date } {
    const visible = this.visibleDate();
    if (this.calendarView() === 'Week') {
      const from = this.startOfWeek(visible);
      const to = new Date(from); to.setDate(from.getDate() + 7);
      return { from, to };
    }

    const monthStart = this.startOfMonth(visible);
    if (this.calendarView() === 'Agenda')
      return { from: monthStart, to: new Date(visible.getFullYear(), visible.getMonth() + 1, 1) };

    const from = this.startOfWeek(monthStart);
    const to = new Date(from); to.setDate(from.getDate() + 42);
    return { from, to };
  }

  private buildMonthDays(month: Date, entries: CalendarEntry[]): CalendarDay[] {
    return this.buildRangeDays(
      this.startOfWeek(this.startOfMonth(month)),
      42,
      entries,
      month.getMonth(),
    );
  }

  private buildRangeDays(
    start: Date,
    count: number,
    entries: CalendarEntry[],
    currentMonth: number,
  ): CalendarDay[] {
    return Array.from({ length: count }, (_, index) => {
      const date = new Date(start); date.setDate(start.getDate() + index);
      return { date, isCurrentMonth: date.getMonth() === currentMonth, entries: entries.filter(entry => {
        const eventDate = new Date(entry.startsAtUtc);
        return eventDate.getFullYear() === date.getFullYear() && eventDate.getMonth() === date.getMonth() && eventDate.getDate() === date.getDate();
      }) };
    });
  }

  private startOfMonth(date: Date): Date { return new Date(date.getFullYear(), date.getMonth(), 1); }

  private startOfWeek(date: Date): Date {
    const start = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const daysSinceMonday = (start.getDay() + 6) % 7;
    start.setDate(start.getDate() - daysSinceMonday);
    return start;
  }

  private loadSavedView(): CalendarView {
    const saved = localStorage.getItem('plantcare.calendarView');
    return saved === 'Week' || saved === 'Agenda' ? saved : 'Month';
  }
}
