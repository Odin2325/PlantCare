import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { CalendarApiService } from '../../data-access/calendar-api.service';
import { CalendarEntry } from '../../models/calendar.model';
import { MyPlantsApiService } from '../../../my-plants/data-access/my-plants-api.service';
import { CareActionType } from '../../../my-plants/models/user-plant.model';

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
  readonly visibleMonth = signal(this.startOfMonth(new Date()));
  readonly entries = signal<CalendarEntry[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedPlantId = signal('all');
  readonly selectedKind = signal('All');
  readonly selectedEntry = signal<CalendarEntry | null>(null);
  readonly completingEntryId = signal<string | null>(null);
  readonly plants = computed(() => Array.from(
    new Map(this.entries().map(entry => [entry.userPlantId, entry.plantName])).entries(),
  ).map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name)));
  readonly filteredEntries = computed(() => this.entries().filter(entry =>
    (this.selectedPlantId() === 'all' || entry.userPlantId === this.selectedPlantId()) &&
    (this.selectedKind() === 'All' || entry.kind === this.selectedKind()),
  ));
  readonly days = computed(() => this.buildDays(this.visibleMonth(), this.filteredEntries()));

  constructor() { this.load(); }

  changeMonth(offset: number): void {
    const current = this.visibleMonth();
    this.visibleMonth.set(new Date(current.getFullYear(), current.getMonth() + offset, 1));
    this.load();
  }

  goToToday(): void { this.visibleMonth.set(this.startOfMonth(new Date())); this.load(); }

  selectEntry(entry: CalendarEntry): void { this.selectedEntry.set(entry); }

  closeDetails(): void { this.selectedEntry.set(null); }

  canComplete(entry: CalendarEntry): boolean {
    return entry.kind === 'Scheduled' && new Date(entry.startsAtUtc).getTime() <= Date.now();
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

  private load(): void {
    const month = this.visibleMonth();
    const from = new Date(month.getFullYear(), month.getMonth(), 1);
    const to = new Date(month.getFullYear(), month.getMonth() + 1, 1);
    this.isLoading.set(true); this.errorMessage.set(null);
    this.api.getEntries(from, to).pipe(
      finalize(() => this.isLoading.set(false)), takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: entries => { this.entries.set(entries); this.selectedEntry.set(null); },
      error: () => this.errorMessage.set('The calendar could not be loaded.'),
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
