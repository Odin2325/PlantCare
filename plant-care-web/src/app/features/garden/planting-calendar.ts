import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserPlant } from '../my-plants/models/user-plant.model';

export const plantingTargets = [
  ['plannedSowingOn', 'Planned sowing'],
  ['plannedPlantingOutOn', 'Planned planting out'],
  ['expectedHarvestOn', 'Expected harvest'],
] as const;

type TargetField = typeof plantingTargets[number][0];

// Calendar dates stay as strings. UTC is used only for formatting, never for comparing dates.
export function validPlanDate(value: string | null | undefined): value is string {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value) || value < '0001-01-01') return false;
  const date = new Date(value + 'T00:00:00Z');
  return Number.isFinite(date.getTime()) && date.toISOString().slice(0, 10) === value;
}

@Component({
  selector: 'app-planting-calendar', standalone: true, imports: [RouterLink],
  templateUrl: './planting-calendar.html', styleUrl: './planting-calendar.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantingCalendar {
  readonly plants = input.required<UserPlant[]>();
  readonly month = signal(this.currentMonth());
  readonly monthTitle = computed(() => new Intl.DateTimeFormat('en', {
    month: 'long', year: 'numeric', timeZone: 'UTC',
  }).format(new Date(this.month() + '-01T00:00:00Z')));
  readonly targetOptions = plantingTargets;
  readonly targetFilter = signal<TargetField | ''>('');
  private readonly events = computed(() => {
    const filter = this.targetFilter();
    return this.plants().filter(plant => plant.isActive).flatMap(plant => plantingTargets.flatMap(([field, action], order) => {
      const date = plant[field];
      return (!filter || field === filter) && validPlanDate(date)
        ? [{ date, action, plantId: plant.id, name: plant.nickname?.trim() || plant.speciesCommonName, field, order }] : [];
    })).sort((a, b) => a.date.localeCompare(b.date) || a.name.localeCompare(b.name, 'en') || a.order - b.order);
  });
  private readonly monthsWithTargets = computed(() => [...new Set(this.events().map(event => event.date.slice(0, 7)))]);
  readonly previousTargetMonth = computed(() => this.monthsWithTargets().filter(month => month < this.month()).at(-1));
  readonly nextTargetMonth = computed(() => this.monthsWithTargets().find(month => month > this.month()));
  readonly days = computed(() => {
    const grouped = new Map<string, ReturnType<typeof this.events>>();
    const month = this.month();
    for (const event of this.events()) {
      if (!event.date.startsWith(month + '-')) continue;
      const day = grouped.get(event.date);
      if (day) day.push(event);
      else grouped.set(event.date, [event]);
    }
    const format = new Intl.DateTimeFormat('en', {
      weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC',
    });
    return [...grouped].map(([date, events]) => ({
      date, label: format.format(new Date(date + 'T00:00:00Z')), events,
    }));
  });
  setTargetFilter(value: string): void {
    const target = plantingTargets.find(([field]) => field === value);
    this.targetFilter.set(target?.[0] ?? '');
  }
  setMonth(value: string): void { if (validPlanDate(value + '-01')) this.month.set(value); }
  moveMonth(offset: number): void {
    const [year, month] = this.month().split('-').map(Number);
    const index = (year - 1) * 12 + month - 1 + offset;
    if (index < 0 || index >= 9999 * 12) return;
    this.month.set(`${String(Math.floor(index / 12) + 1).padStart(4, '0')}-${String(index % 12 + 1).padStart(2, '0')}`);
  }
  goToCurrentMonth(): void { this.month.set(this.currentMonth()); }
  private currentMonth(): string {
    const today = new Date();
    return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
  }
}
