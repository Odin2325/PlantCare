import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MyPlantsApiService } from '../my-plants/data-access/my-plants-api.service';
import { CareSchedule, UserPlant } from '../my-plants/models/user-plant.model';
import { PlantCategory } from '../../models/plant-species.model';
import { plantCategoryOptions } from '../../shared/plant-categories';
import { PlantPhoto } from '../../shared/plant-photo/plant-photo';
import { normalizePlantSearch } from '../../shared/plant-search';
import { HarvestOverview } from './harvest-overview';
import { PlantingCalendar } from './planting-calendar';

@Component({
  selector: 'app-garden-page', standalone: true, imports: [DatePipe, RouterLink, PlantPhoto, PlantingCalendar, HarvestOverview],
  templateUrl: './garden-page.html', styleUrl: './garden-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GardenPage {
  private readonly api = inject(MyPlantsApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly plants = signal<UserPlant[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly view = signal<'plants' | 'harvests'>('plants');
  readonly query = signal('');
  readonly category = signal<PlantCategory | ''>('');
  readonly planFilter = signal<'planned' | 'unplanned' | ''>('');
  hasPlan(plant: UserPlant): boolean { return Boolean(plant.plannedSowingOn || plant.plannedPlantingOutOn || plant.expectedHarvestOn); }
  readonly categories = plantCategoryOptions.filter(option => option.value !== 'houseplant');
  readonly gardenPlants = computed(() => this.plants().filter(plant => plant.isActive &&
    plant.speciesCatalogProfile?.categories?.some(category => category !== 'houseplant')));
  readonly visiblePlants = computed(() => {
    const query = normalizePlantSearch(this.query());
    return this.gardenPlants().filter(plant =>
      (!this.planFilter() || (this.planFilter() === 'planned' ? this.hasPlan(plant) : !this.hasPlan(plant))) &&
      (!this.category() || plant.speciesCatalogProfile?.categories?.includes(this.category() as PlantCategory)) &&
      (!query || normalizePlantSearch([plant.nickname, plant.speciesCommonName, plant.speciesScientificName,
        plant.location, ...plant.tags].join(' ')).includes(query)))
      .sort((a, b) => (a.nickname || a.speciesCommonName).localeCompare(b.nickname || b.speciesCommonName, 'en'));
  });
  readonly locations = computed(() => {
    const groups = new Map<string, UserPlant[]>();
    for (const plant of this.visiblePlants()) {
      const location = plant.location?.trim() || 'Location not set';
      groups.set(location, [...(groups.get(location) ?? []), plant]);
    }
    return [...groups].sort(([a], [b]) => a.localeCompare(b, 'en')).map(([name, plants]) => ({ name, plants }));
  });
  constructor() { this.load(); }
  load(): void {
    this.loading.set(true); this.error.set(false);
    this.api.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: plants => { this.plants.set(plants); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }
  setCategory(value: PlantCategory | ''): void {
    this.category.set(this.categories.some(option => option.value === value) ? value : '');
  }
  clearFilters(): void { this.query.set(''); this.category.set(''); this.planFilter.set(''); }
  nextCare(plant: UserPlant): CareSchedule | undefined {
    return plant.careSchedules.filter(schedule => schedule.isEnabled && schedule.nextDueAtUtc &&
      Number.isFinite(Date.parse(schedule.nextDueAtUtc)))
      .sort((a, b) => Date.parse(a.nextDueAtUtc!) - Date.parse(b.nextDueAtUtc!))[0];
  }
}
