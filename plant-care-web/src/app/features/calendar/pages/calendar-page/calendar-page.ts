import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { CalendarApiService } from '../../data-access/calendar-api.service';
import { CalendarEntry } from '../../models/calendar.model';

interface CalendarDay { date: Date; isCurrentMonth: boolean; entries: CalendarEntry[]; }

@Component({
  selector: 'app-calendar-page', standalone: true, imports: [DatePipe],
  templateUrl: './calendar-page.html', styleUrl: './calendar-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarPage {
  private readonly api = inject(CalendarApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly visibleMonth = signal(this.startOfMonth(new Date()));
  readonly entries = signal<CalendarEntry[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly days = computed(() => this.buildDays(this.visibleMonth(), this.entries()));

  constructor() { this.load(); }

  changeMonth(offset: number): void {
    const current = this.visibleMonth();
    this.visibleMonth.set(new Date(current.getFullYear(), current.getMonth() + offset, 1));
    this.load();
  }

  goToToday(): void { this.visibleMonth.set(this.startOfMonth(new Date())); this.load(); }

  private load(): void {
    const month = this.visibleMonth();
    const from = new Date(month.getFullYear(), month.getMonth(), 1);
    const to = new Date(month.getFullYear(), month.getMonth() + 1, 1);
    this.isLoading.set(true); this.errorMessage.set(null);
    this.api.getEntries(from, to).pipe(
      finalize(() => this.isLoading.set(false)), takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: entries => this.entries.set(entries),
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
