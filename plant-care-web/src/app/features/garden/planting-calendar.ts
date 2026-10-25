import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserPlant } from '../my-plants/models/user-plant.model';

const actions = [
  ['plannedSowingOn', 'Planned sowing'],
  ['plannedPlantingOutOn', 'Planned planting out'],
  ['expectedHarvestOn', 'Expected harvest'],
] as const;

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
  readonly days = computed(() => {
    const events = this.plants().filter(plant => plant.isActive).flatMap(plant => actions.flatMap(([field, action], order) => {
      const date = plant[field];
      return validPlanDate(date) && date.startsWith(this.month() + '-')
        ? [{ date, action, plantId: plant.id, name: plant.nickname || plant.speciesCommonName, field, order }] : [];
    })).sort((a, b) => a.date.localeCompare(b.date) || a.name.localeCompare(b.name, 'en') || a.order - b.order);
    const dates = [...new Set(events.map(event => event.date))];
    return dates.map(date => ({ date, label: new Intl.DateTimeFormat('en', {
      weekday: 'long', day: 'numeric', month: 'long', timeZone: 'UTC',
    }).format(new Date(date + 'T00:00:00Z')), events: events.filter(event => event.date === date) }));
  });
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
