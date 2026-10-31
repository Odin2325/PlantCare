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
import { parseComparisonIds } from '../../../../shared/plant-comparison';
import { HttpErrorResponse } from '@angular/common/http';
import { PlantCategory, PlantSpecies, SunlightRequirement } from '../../../../models/plant-species.model';
import { parsePlantCategory, plantCategoryLabel, plantCategoryOptions } from '../../../../shared/plant-categories';
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
  readonly category = signal<PlantCategory | ''>('');
  readonly kind = signal<'cultivar' | 'general' | ''>('');
  readonly categoryOptions = plantCategoryOptions;
  readonly categoryLabel = plantCategoryLabel;
  readonly light = signal<SunlightRequirement | ''>('');
  readonly pets = signal<'non-toxic' | 'warning' | 'unknown' | ''>('');
  readonly growth = signal<'available' | ''>('');
  readonly cultivarPhoto = signal<'available' | 'pending' | ''>('');
  readonly sort = signal<'name-asc' | 'name-desc'>('name-asc');
  readonly comparisonIds = signal<string[]>([]);
  readonly comparisonPlants = computed(() => this.comparisonIds().map(id => this.plantSpecies().find(p => p.id === id))
    .filter((p): p is PlantSpecies => Boolean(p)));
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
    category: this.category() || null,
    kind: this.kind() || null,
    light: this.light() || null,
    pets: this.pets() || null,
    growth: this.growth() || null,
    cultivarPhoto: this.cultivarPhoto() || null,
    sort: this.sort() === 'name-desc' ? 'name-desc' : null,
    compare: this.comparisonIds().length ? this.comparisonIds().join(',') : null,
  }));
  readonly hasFilters = computed(() => Boolean(this.query() || this.category() || this.kind() || this.light() || this.pets() || this.growth() || this.cultivarPhoto() || this.sort() !== 'name-asc'));
  readonly visibleSpecies = computed(() => {
    const query = normalizePlantSearch(this.query());
    // An exact scientific name identifies a species, rather than other plants
    // whose descriptions mention it. Common-name searches retain ambiguity.
    const exactScientific = query ? this.plantSpecies().filter(plant => normalizePlantSearch(plant.scientificName ?? '') === query) : [];
    return (exactScientific.length ? exactScientific : this.plantSpecies()).filter(plant =>
      (!this.category() || Boolean(plant.catalogProfile?.categories?.includes(this.category() as PlantCategory))) &&
      (!this.kind() || (this.kind() === 'cultivar' ? Boolean(plant.catalogProfile?.cultivarName) : !plant.catalogProfile?.cultivarName)) &&
      (!this.light() || plant.sunlightRequirement === this.light()) &&
      (!this.growth() || Boolean(plant.catalogProfile?.growthReferences?.length)) &&
      (!this.cultivarPhoto() || Boolean(plant.catalogProfile?.cultivarName) &&
        ((this.cultivarPhoto() === 'available') === Boolean(plant.catalogProfile?.photo && !plant.catalogProfile?.photoIsSpeciesReference))) &&
      (!this.pets() || (this.pets() === 'warning' ? plant.isToxicToPets :
        this.pets() === 'unknown' ? !plant.isToxicToPets && !plant.catalogProfile?.nonToxicToCatsAndDogsSourceUrl :
        !plant.isToxicToPets && Boolean(plant.catalogProfile?.nonToxicToCatsAndDogsSourceUrl))) &&
      (!query || normalizePlantSearch([plant.commonName, plant.scientificName, plant.description,
        plant.catalogProfile?.cultivarName,
        ...(plant.catalogProfile?.commonNameAliases ?? [])].join(' ')).includes(query)))
      .sort((a, b) => a.commonName.localeCompare(b.commonName, 'en', { sensitivity: 'base' }) *
        (this.sort() === 'name-desc' ? -1 : 1));
  });
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      this.query.set((params.get('q') ?? '').slice(0, 100));
      this.category.set(parsePlantCategory(params.get('category')));
      const kind = params.get('kind');
      this.kind.set(kind === 'cultivar' || kind === 'general' ? kind : '');
      const light = params.get('light');
      this.light.set(this.lightOptions.find(option => option.value === light)?.value ?? '');
      const pets = params.get('pets');
      this.pets.set(pets === 'non-toxic' || pets === 'warning' || pets === 'unknown' ? pets : '');
      this.growth.set(params.get('growth') === 'available' ? 'available' : '');
      const photo = params.get('cultivarPhoto');
      this.cultivarPhoto.set(photo === 'available' || photo === 'pending' ? photo : '');
      this.sort.set(params.get('sort') === 'name-desc' ? 'name-desc' : 'name-asc');
      this.comparisonIds.set(parseComparisonIds(params.get('compare')));
    });
    this.loadPlantSpecies();
  }

  setQuery(value: string): void {
    this.query.set(value.slice(0, 100));
    this.saveFilters();
  }

  setCategory(value: string): void {
    this.category.set(parsePlantCategory(value));
    this.saveFilters();
  }

  categoryQueryParams(value: PlantCategory | '') {
    return { ...this.filterQueryParams(), category: value || null };
  }

  setKind(value: string): void {
    this.kind.set(value === 'cultivar' || value === 'general' ? value : '');
    this.saveFilters();
  }

  toggleComparison(plant: PlantSpecies): void {
    const ids = this.comparisonPlants().map(p => p.id);
    if (ids.includes(plant.id)) this.comparisonIds.set(ids.filter(id => id !== plant.id));
    else if (ids.length < 3) this.comparisonIds.set([...ids, plant.id]);
    this.saveFilters();
  }

  clearComparison(): void { this.comparisonIds.set([]); this.saveFilters(); }

  setLight(value: string): void {
    this.light.set(this.lightOptions.find(option => option.value === value)?.value ?? '');
    this.saveFilters();
  }

  setSort(value: string): void {
    this.sort.set(value === 'name-desc' ? 'name-desc' : 'name-asc');
    this.saveFilters();
  }

  setPets(value: string): void {
    this.pets.set(value === 'non-toxic' || value === 'warning' || value === 'unknown' ? value : '');
    this.saveFilters();
  }

  clearFilters(): void {
    this.query.set('');
    this.category.set('');
    this.kind.set('');
    this.light.set('');
    this.pets.set('');
    this.growth.set('');
    this.cultivarPhoto.set('');
    this.sort.set('name-asc');
    this.saveFilters();
  }

  setGrowth(value: string): void {
    this.growth.set(value === 'available' ? value : '');
    this.saveFilters();
  }

  setCultivarPhoto(value: string): void {
    this.cultivarPhoto.set(value === 'available' || value === 'pending' ? value : '');
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
