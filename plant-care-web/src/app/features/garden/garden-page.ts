import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { MyPlantsApiService } from '../my-plants/data-access/my-plants-api.service';
import { CareSchedule, UserPlant } from '../my-plants/models/user-plant.model';
import { PlantCategory } from '../../models/plant-species.model';
import { plantCategoryOptions } from '../../shared/plant-categories';
import { PlantPhoto } from '../../shared/plant-photo/plant-photo';
import { normalizePlantSearch } from '../../shared/plant-search';
import { HarvestOverview } from './harvest-overview';
import { PlantingCalendar } from './planting-calendar';
import { UpcomingWork } from './upcoming-work';

const locationPrefix = 'location:';
const locationKey = (plant: UserPlant): string => `${locationPrefix}${plant.location?.trim() ?? ''}`;
const locationName = (key: string): string => key.slice(locationPrefix.length) || 'Location not set';

@Component({
  selector: 'app-garden-page', standalone: true, imports: [DatePipe, RouterLink, PlantPhoto, PlantingCalendar, HarvestOverview, UpcomingWork],
  templateUrl: './garden-page.html', styleUrl: './garden-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GardenPage {
  private readonly api = inject(MyPlantsApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly plants = signal<UserPlant[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly view = toSignal(inject(ActivatedRoute).queryParamMap.pipe(map(params => {
    const view = params.get('view');
    return view === 'upcoming' || view === 'harvests' ? view : 'plants';
  })), { initialValue: 'plants' });
  readonly query = signal('');
  readonly category = signal<PlantCategory | ''>('');
  readonly locationFilter = signal('');
  readonly planFilter = signal<'planned' | 'unplanned' | ''>('');
  hasPlan(plant: UserPlant): boolean { return Boolean(plant.plannedSowingOn || plant.plannedPlantingOutOn || plant.expectedHarvestOn); }
  readonly categories = plantCategoryOptions.filter(option => option.value !== 'houseplant');
  readonly gardenPlants = computed(() => this.plants().filter(plant => plant.isActive &&
    plant.speciesCatalogProfile?.categories?.some(category => category !== 'houseplant')));
  readonly locationOptions = computed(() => {
    const locations = new Set(this.gardenPlants().map(locationKey));
    return [...locations].map(key => ({ key, name: key === locationPrefix ? 'No saved location' : locationName(key) }))
      .sort((a, b) => a.name.localeCompare(b.name, 'en'));
  });
  readonly visiblePlants = computed(() => {
    const query = normalizePlantSearch(this.query());
    const location = this.locationFilter();
    return this.gardenPlants().filter(plant =>
      (!location || locationKey(plant) === location) &&
      (!this.planFilter() || (this.planFilter() === 'planned' ? this.hasPlan(plant) : !this.hasPlan(plant))) &&
      (!this.category() || plant.speciesCatalogProfile?.categories?.includes(this.category() as PlantCategory)) &&
      (!query || normalizePlantSearch([plant.nickname, plant.speciesCommonName, plant.speciesScientificName,
        plant.location, ...plant.tags].join(' ')).includes(query)))
      .sort((a, b) => (a.nickname || a.speciesCommonName).localeCompare(b.nickname || b.speciesCommonName, 'en'));
  });
  readonly locations = computed(() => {
    const groups = new Map<string, UserPlant[]>();
    for (const plant of this.visiblePlants()) {
      const key = locationKey(plant);
      const group = groups.get(key);
      if (group) group.push(plant);
      else groups.set(key, [plant]);
    }
    return [...groups].map(([key, plants]) => ({ key, name: locationName(key), plants }))
      .sort((a, b) => a.name.localeCompare(b.name, 'en'));
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
  clearFilters(): void {
    this.query.set('');
    this.category.set('');
    this.planFilter.set('');
    this.locationFilter.set('');
  }
  nextCare(plant: UserPlant): CareSchedule | undefined {
    return plant.careSchedules.filter(schedule => schedule.isEnabled && schedule.nextDueAtUtc &&
      Number.isFinite(Date.parse(schedule.nextDueAtUtc)))
      .sort((a, b) => Date.parse(a.nextDueAtUtc!) - Date.parse(b.nextDueAtUtc!))[0];
  }
}
