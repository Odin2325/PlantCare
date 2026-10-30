import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { interval } from 'rxjs';
import { UserPlant } from '../my-plants/models/user-plant.model';
import { validPlanDate } from './planting-calendar';

type WorkKind = 'care' | 'target';
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
const targets = [
  ['plannedSowingOn', 'Planned sowing'],
  ['plannedPlantingOutOn', 'Planned planting out'],
  ['expectedHarvestOn', 'Expected harvest'],
] as const;

export function localWorkDate(date: Date): string {
  return `${String(date.getFullYear()).padStart(4, '0')}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function gardenWorkEntries(plants: UserPlant[], now: Date, days: number,
  includePast: boolean, kind: WorkKind | ''): GardenWorkEntry[] {
  if (!Number.isFinite(now.getTime()) || ![7, 30, 90].includes(days)) return [];
  const today = localWorkDate(now);
  // Calendar arithmetic keeps the range correct across daylight-saving changes.
  const end = new Date(now); end.setDate(end.getDate() + days - 1);
  const last = localWorkDate(end);
  const entries: GardenWorkEntry[] = [];
  for (const plant of plants.filter(p => p.isActive)) {
    const name = plant.nickname?.trim() || plant.speciesCommonName;
    for (const [field, action] of targets) {
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
  return entries.filter(e => e.date <= last && (includePast || e.date >= today) && (!kind || e.kind === kind))
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
  readonly kind = signal<WorkKind | ''>('');
  readonly entries = computed(() => gardenWorkEntries(this.plants(), this.now(), this.days(), this.includePast(), this.kind()));
  readonly groups = computed(() => {
    const groups = new Map<string, GardenWorkEntry[]>();
    for (const entry of this.entries()) {
      const group = groups.get(entry.date);
      if (group) group.push(entry); else groups.set(entry.date, [entry]);
    }
    return [...groups].map(([date, entries]) => ({ date, entries }));
  });
  constructor() {
    interval(60_000).pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe(() => this.now.set(new Date()));
  }
  setDays(value: string): void { if (['7', '30', '90'].includes(value)) this.days.set(Number(value)); }
  setKind(value: string): void { if (['', 'care', 'target'].includes(value)) this.kind.set(value as WorkKind | ''); }
}
