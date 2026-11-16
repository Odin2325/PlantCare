import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, linkedSignal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { interval } from 'rxjs';
import { UserPlant } from '../my-plants/models/user-plant.model';
import { plantingTargets, validPlanDate } from './planting-calendar';

type WorkKind = 'care' | 'target';
type WorkFilter = WorkKind | 'due-care' | '';
const workPageSize = 8;
export interface GardenWorkEntry {
  id: string;
  plantId: string;
  name: string;
  date: string;
  instant: string | null;
  action: string;
  kind: WorkKind;
  status: string;
}
export function localWorkDate(date: Date): string {
  return `${String(date.getFullYear()).padStart(4, '0')}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function gardenWorkEntries(plants: UserPlant[], now: Date, days: number,
  includePast: boolean, kind: WorkFilter): GardenWorkEntry[] {
  if (!Number.isFinite(now.getTime()) || ![7, 30, 90].includes(days)) return [];
  const today = localWorkDate(now);
  // Calendar arithmetic keeps the range correct across daylight-saving changes.
  const end = new Date(now); end.setDate(end.getDate() + days - 1);
  const last = localWorkDate(end);
  const entries: GardenWorkEntry[] = [];
  for (const plant of plants.filter(p => p.isActive)) {
    const name = plant.nickname?.trim() || plant.speciesCommonName;
    for (const [field, action] of plantingTargets) {
      const date = plant[field];
      if (validPlanDate(date)) entries.push({ id: `${plant.id}:${field}`, plantId: plant.id,
        name, date, instant: null, action, kind: 'target',
        status: date < today ? 'Past target' : date === today ? 'Target today' : 'Upcoming target' });
    }
    for (const schedule of plant.careSchedules) {
      if (!schedule.isEnabled || !schedule.nextDueAtUtc ||
        !Number.isFinite(Date.parse(schedule.nextDueAtUtc)) || schedule.actionType === 'Unknown') continue;
      const date = localWorkDate(new Date(schedule.nextDueAtUtc));
      entries.push({ id: `${plant.id}:${schedule.id}`, plantId: plant.id, name, date,
        instant: schedule.nextDueAtUtc, action: schedule.actionType, kind: 'care',
        status: date < today ? 'Overdue care' : date === today ? 'Care today' : 'Upcoming care' });
    }
  }
  return entries.filter(entry => kind === 'due-care'
    ? entry.kind === 'care' && entry.date <= today
    : entry.date <= last && (includePast || entry.date >= today) && (!kind || entry.kind === kind))
    .sort((a, b) => a.date.localeCompare(b.date) || a.name.localeCompare(b.name, 'en') ||
      a.kind.localeCompare(b.kind) || a.action.localeCompare(b.action, 'en') || a.id.localeCompare(b.id));
}

@Component({
  selector: 'app-upcoming-work', standalone: true, imports: [DatePipe, RouterLink],
  templateUrl: './upcoming-work.html', styleUrl: './upcoming-work.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpcomingWork {
  readonly plants = input.required<UserPlant[]>();
  readonly now = signal(new Date());
  readonly days = signal(30);
  readonly includePast = signal(true);
  readonly kind = signal<WorkFilter>('');
  readonly entries = computed(() => gardenWorkEntries(this.plants(), this.now(), this.days(), this.includePast(), this.kind()));
  private readonly requestedPage = linkedSignal({
    source: () => ({ plants: this.plants(), days: this.days(), includePast: this.includePast(), kind: this.kind() }),
    computation: () => 1,
  });
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.entries().length / workPageSize)));
  readonly page = computed(() => Math.min(this.requestedPage(), this.pageCount()));
  readonly pageStart = computed(() => (this.page() - 1) * workPageSize);
  readonly pageEnd = computed(() => Math.min(this.pageStart() + workPageSize, this.entries().length));
  readonly groups = computed(() => {
    const groups = new Map<string, GardenWorkEntry[]>();
    for (const entry of this.entries().slice(this.pageStart(), this.pageEnd())) {
      const group = groups.get(entry.date);
      if (group) group.push(entry); else groups.set(entry.date, [entry]);
    }
    return [...groups].map(([date, entries]) => ({ date, entries }));
  });
  constructor() {
    interval(60_000).pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe(() => this.now.set(new Date()));
  }
  changePage(offset: number): void {
    this.requestedPage.set(Math.max(1, Math.min(this.pageCount(), this.page() + offset)));
  }
  setDays(value: string): void { if (['7', '30', '90'].includes(value)) this.days.set(Number(value)); }
  setKind(value: string): void {
    if (value === '' || value === 'care' || value === 'target' || value === 'due-care') this.kind.set(value);
  }
}
