import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PlantSpeciesApiService } from '../../../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../../../models/plant-species.model';
import { PlantPhoto } from '../../../../shared/plant-photo/plant-photo';
import { parseComparisonIds } from '../../../../shared/plant-comparison';

@Component({
  selector: 'app-plant-comparison-page', standalone: true, imports: [RouterLink, PlantPhoto],
  templateUrl: './plant-comparison-page.html', styleUrl: './plant-comparison-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantComparisonPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(PlantSpeciesApiService);
  readonly ids = signal<string[]>([]);
  readonly catalog = signal<PlantSpecies[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly plants = computed(() => this.ids().map(id => this.catalog().find(p => p.id === id))
    .filter((p): p is PlantSpecies => Boolean(p)));
  readonly unavailable = computed(() => !this.loading() && !this.error() && this.plants().length < this.ids().length);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => this.ids.set(parseComparisonIds(params.get('compare'))));
    this.reload();
  }
  reload(): void {
    this.loading.set(true); this.error.set(false);
    this.api.getAll().subscribe({next: plants => {this.catalog.set(plants);this.loading.set(false);},
      error: () => {this.error.set(true);this.loading.set(false);}});
  }
  remove(id: string): void {
    const ids = this.ids().filter(value => value !== id);
    void this.router.navigate([], {relativeTo: this.route, queryParamsHandling: 'merge',
      queryParams: {compare: ids.length ? ids.join(',') : null}, replaceUrl: true});
  }
  lightLabel(value: string): string { return value === 'Unknown' ? 'Not specified' : value.replace(/([a-z])([A-Z])/g, '$1 $2'); }
}
