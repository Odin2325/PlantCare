import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { PlantPhoto } from '../../../../shared/plant-photo/plant-photo';
import { normalizePlantSearch } from '../../../../shared/plant-search';
import { HttpErrorResponse } from '@angular/common/http';
import { PlantSpecies, SunlightRequirement } from '../../../../models/plant-species.model';
import { PlantSpeciesApiService } from '../../../../core/services/plant-species-api.service';

@Component({
  selector: 'app-plant-catalog-page',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PlantPhoto,
  ],
  templateUrl: './plant-catalog-page.html',
  styleUrl: './plant-catalog-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantCatalogPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly plantSpeciesApi =
    inject(PlantSpeciesApiService);

  readonly plantSpecies = signal<PlantSpecies[]>([]);
  readonly query = signal('');
  readonly light = signal<SunlightRequirement | ''>('');
  readonly sort = signal<'name-asc' | 'name-desc'>('name-asc');
  readonly lightOptions: { value: SunlightRequirement; label: string }[] = [
    { value: 'LowLight', label: 'Low light' },
    { value: 'MediumIndirectLight', label: 'Medium indirect light' },
    { value: 'BrightIndirectLight', label: 'Bright indirect light' },
    { value: 'PartialSun', label: 'Partial sun' },
    { value: 'FullSun', label: 'Full sun' },
    { value: 'Unknown', label: 'Not specified' },
  ];
  readonly filterQueryParams = computed(() => ({
    q: this.query() || null,
    light: this.light() || null,
    sort: this.sort() === 'name-desc' ? 'name-desc' : null,
  }));
  readonly hasFilters = computed(() => Boolean(this.query() || this.light() || this.sort() !== 'name-asc'));
  readonly visibleSpecies = computed(() => {
    const query = normalizePlantSearch(this.query());
    return this.plantSpecies().filter(plant =>
      (!this.light() || plant.sunlightRequirement === this.light()) &&
      (!query || normalizePlantSearch([plant.commonName, plant.scientificName, plant.description,
        ...(plant.catalogProfile?.commonNameAliases ?? [])].join(' ')).includes(query)))
      .sort((a, b) => a.commonName.localeCompare(b.commonName, 'en', { sensitivity: 'base' }) *
        (this.sort() === 'name-desc' ? -1 : 1));
  });
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      this.query.set((params.get('q') ?? '').slice(0, 100));
      const light = params.get('light');
      this.light.set(this.lightOptions.find(option => option.value === light)?.value ?? '');
      this.sort.set(params.get('sort') === 'name-desc' ? 'name-desc' : 'name-asc');
    });
    this.loadPlantSpecies();
  }

  setQuery(value: string): void {
    this.query.set(value.slice(0, 100));
    this.saveFilters();
  }

  setLight(value: string): void {
    this.light.set(this.lightOptions.find(option => option.value === value)?.value ?? '');
    this.saveFilters();
  }

  setSort(value: string): void {
    this.sort.set(value === 'name-desc' ? 'name-desc' : 'name-asc');
    this.saveFilters();
  }

  clearFilters(): void {
    this.query.set('');
    this.light.set('');
    this.sort.set('name-asc');
    this.saveFilters();
  }

  private saveFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route, queryParams: this.filterQueryParams(), replaceUrl: true,
    });
  }

  reload(): void {
    this.loadPlantSpecies();
  }

  formatEnumName(value: string): string {
    return value.replace(
      /([a-z])([A-Z])/g,
      '$1 $2',
    );
  }

  private loadPlantSpecies(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.plantSpeciesApi.getAll().subscribe({
      next: (plantSpecies) => {
        this.plantSpecies.set(plantSpecies);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Unable to load the plant catalog.', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          url: error.url,
          error: error.error,
        });

        this.errorMessage.set(
          `The plant catalog could not be loaded. HTTP status: ${error.status}`,
        );

        this.isLoading.set(false);
      },
    });
  }
}
