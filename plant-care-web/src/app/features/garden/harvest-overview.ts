import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { MyPlantsApiService } from '../my-plants/data-access/my-plants-api.service';
import { UserPlant } from '../my-plants/models/user-plant.model';
import { HarvestOverview as Overview } from '../my-plants/models/harvest-overview.model';
import { validPlanDate } from './planting-calendar';

export function sumHarvestQuantities(values: string[]): string {
  const total = values.reduce((sum, value) => {
    const [whole, fraction = ''] = value.split('.');
    return sum + BigInt(whole) * 1000n + BigInt(fraction.padEnd(3, '0'));
  }, 0n);
  const fraction = (total % 1000n).toString().padStart(3, '0').replace(/0+$/, '');
  return `${total / 1000n}${fraction ? '.' + fraction : ''}`;
}

@Component({
  selector: 'app-harvest-overview', standalone: true, imports: [RouterLink],
  templateUrl: './harvest-overview.html', styleUrl: './harvest-overview.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HarvestOverview {
  private readonly api = inject(MyPlantsApiService);
  private request?: Subscription;
  readonly plants = input.required<UserPlant[]>();
  readonly from = signal(this.today().slice(0, 4) + '-01-01');
  readonly to = signal(this.today());
  readonly data = signal<Overview | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly ids = computed(() => new Set(this.plants().map(p => p.id)));
  readonly summaries = computed(() => this.data()?.plants.filter(p => this.ids().has(p.userPlantId)) ?? []);
  readonly count = computed(() => this.summaries().reduce((n, p) => n + p.recordCount, 0));
  readonly unquantified = computed(() => this.summaries().reduce((n, p) => n + p.unquantifiedCount, 0));
  readonly quantities = computed(() => ['items', 'g', 'kg'].flatMap(unit => {
    const values = this.summaries().flatMap(p => p.quantities[unit] ? [p.quantities[unit]] : []);
    return values.length ? [{ unit, quantity: sumHarvestQuantities(values) }] : [];
  }));
  readonly rows = computed(() => this.data()?.rows.filter(r => this.ids().has(r.userPlantId)) ?? []);
  constructor() { inject(DestroyRef).onDestroy(() => this.request?.unsubscribe()); this.load(); }
  load(): void {
    this.request?.unsubscribe(); this.data.set(null); this.error.set(''); this.loading.set(false);
    const from = this.from(), to = this.to();
    if (!validPlanDate(from) || !validPlanDate(to) || from > to ||
      (Date.parse(to + 'T00:00:00Z') - Date.parse(from + 'T00:00:00Z')) / 86400000 > 365) {
      this.error.set('Choose a valid date range of at most 366 days.'); return;
    }
    this.loading.set(true);
    this.request = this.api.getHarvestOverview(from, to).subscribe({
      next: data => { this.data.set(data); this.loading.set(false); },
      error: () => { this.error.set('Your harvest overview could not be loaded. Please try again.'); this.loading.set(false); },
    });
  }
  thisYear(): void { this.to.set(this.today()); this.from.set(this.today().slice(0, 4) + '-01-01'); this.load(); }
  name(id: string): string { const plant = this.plants().find(p => p.id === id); return plant?.nickname || plant?.speciesCommonName || 'Plant'; }
  date(value: string): string { return new Intl.DateTimeFormat('en', { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' }).format(new Date(value + 'T00:00:00Z')); }
  private today(): string { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }
}
