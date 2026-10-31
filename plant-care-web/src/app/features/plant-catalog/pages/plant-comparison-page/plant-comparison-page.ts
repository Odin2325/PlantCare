import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PlantSpeciesApiService } from '../../../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../../../models/plant-species.model';
import { PlantPhoto } from '../../../../shared/plant-photo/plant-photo';
import { parseComparisonIds } from '../../../../shared/plant-comparison';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-plant-comparison-page', standalone: true, imports: [RouterLink, PlantPhoto],
  templateUrl: './plant-comparison-page.html', styleUrl: './plant-comparison-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantComparisonPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(PlantSpeciesApiService);
  private readonly document = inject(DOCUMENT);
  readonly ids = signal<string[]>([]);
  readonly catalog = signal<PlantSpecies[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly plants = computed(() => this.ids().map(id => this.catalog().find(p => p.id === id))
    .filter((p): p is PlantSpecies => Boolean(p)));
  readonly unavailable = computed(() => !this.loading() && !this.error() && this.plants().length < this.ids().length);
  readonly shareUrl = computed(() => {
    if (this.loading() || this.error() || this.plants().length < 2) return '';
    const path = this.router.serializeUrl(this.router.createUrlTree(['/plants/compare'], {
      queryParams: { compare: this.plants().map(plant => plant.id).join(',') },
    }));
    return new URL(path, this.document.baseURI).href;
  });
  readonly copyStatus = signal('');
  readonly copying = signal(false);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      this.ids.set(parseComparisonIds(params.get('compare')));
      this.copyStatus.set('');
    });
    this.reload();
  }
  reload(): void {
    this.loading.set(true); this.error.set(false); this.copyStatus.set('');
    this.api.getAll().subscribe({next: plants => {this.catalog.set(plants);this.loading.set(false);},
      error: () => {this.error.set(true);this.loading.set(false);}});
  }
  remove(id: string): void {
    const ids = this.ids().filter(value => value !== id);
    void this.router.navigate([], {relativeTo: this.route, queryParamsHandling: 'merge',
      queryParams: {compare: ids.length ? ids.join(',') : null}, replaceUrl: true});
  }
  async copyComparisonLink(): Promise<void> {
    const url = this.shareUrl();
    if (!url || this.copying()) return;
    this.copying.set(true); this.copyStatus.set('');
    try {
      if (!navigator.clipboard?.writeText) throw new Error('Clipboard unavailable');
      await navigator.clipboard.writeText(url);
      if (this.shareUrl() === url) this.copyStatus.set('Comparison link copied.');
    } catch {
      if (this.shareUrl() === url) this.copyStatus.set('Copy the link from the Comparison link field.');
    } finally { this.copying.set(false); }
  }
  lightLabel(value: string): string { return value === 'Unknown' ? 'Not specified' : value.replace(/([a-z])([A-Z])/g, '$1 $2'); }
}
