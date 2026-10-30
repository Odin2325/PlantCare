import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { PlantSpeciesApiService } from '../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../models/plant-species.model';
import { PlantPhoto } from '../plant-photo/plant-photo';

// These aliases reconcile the names already reviewed in this catalog. Keep
// horticultural groups distinct instead of guessing from a common name.
const groupAliases: Record<string, string> = {
  'daucus carota': 'daucus carota subsp. sativus',
  'beta vulgaris': 'beta vulgaris garden beet group',
  'pisum sativum': 'lathyrus oleraceus',
  'raphanus sativus': 'raphanus raphanistrum subsp. sativus',
  'brassica oleracea gongylodes group': 'brassica oleracea kohlrabi group',
  'brassica oleracea acephala group': 'brassica oleracea kale group',
  'capsicum annuum grossum group': 'capsicum annuum',
  'capsicum annuum longum group': 'capsicum annuum',
};

export function catalogVarietyGroup(plant: PlantSpecies): string | null {
  if (!plant.scientificName || !plant.catalogProfile) return null;
  const normalize = (value: string) => value.normalize('NFKC').replace(/[‘’]/g, "'").replace(/\s+/g, ' ').trim().toLowerCase();
  let name = normalize(plant.scientificName);
  const cultivar = plant.catalogProfile.cultivarName;
  if (cultivar) {
    const suffix = ` '${normalize(cultivar)}'`;
    if (!name.endsWith(suffix)) return null;
    name = name.slice(0, -suffix.length);
  }
  name = name.replace(/[()]/g, '').replace(/\s+/g, ' ').trim();
  return groupAliases[name] ?? name;
}

@Component({
  selector: 'app-plant-relatives', standalone: true, imports: [RouterLink, PlantPhoto],
  template: `
    <section aria-label="Related varieties">
      <h2>Explore related varieties</h2>
      <p>Browse this catalog's related cultivars and general entry. Growing habits and care can differ between varieties.</p>
      @if (loading()) { <p role="status">Loading related varieties…</p> }
      @else if (error()) { <p role="alert">Related varieties could not be loaded.</p><button type="button" (click)="load()">Try related varieties again</button> }
      @else {
        <p role="status">{{ relatives().length }} related entries</p>
        <div class="relatives-grid">
          @for (relative of relatives(); track relative.id) {
            <article>
              <h3><a [routerLink]="['/plants', relative.id]" queryParamsHandling="preserve">{{ relative.commonName }}</a></h3>
              <p>{{ relative.catalogProfile?.cultivarName ? 'Cultivar: ' + relative.catalogProfile!.cultivarName : 'General entry' }}</p>
              <app-plant-photo [profile]="relative.catalogProfile" />
            </article>
          } @empty { <p>No other reviewed varieties in this catalog group yet.</p> }
        </div>
      }
    </section>
  `,
  styles: `
    :host{display:block;margin-block:2rem;min-width:0}.relatives-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,230px),1fr));gap:1rem}
    article{padding:1rem;border:1px solid #c2cec5;border-radius:.65rem;min-width:0;overflow-wrap:anywhere}h3{margin-top:0}a{color:#245c37}
    button{font:inherit;padding:.65rem;border:1px solid #245c37;border-radius:.4rem;background:white;color:#245c37}
    a:focus-visible,button:focus-visible{outline:3px solid #245c37;outline-offset:3px}
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantRelatives {
  private readonly api = inject(PlantSpeciesApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly plant = input.required<PlantSpecies>();
  readonly catalog = signal<PlantSpecies[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly relatives = computed(() => {
    const current = this.plant(), group = catalogVarietyGroup(current);
    return group ? this.catalog().filter(p => p.id !== current.id && catalogVarietyGroup(p) === group)
      .sort((a, b) => a.commonName.localeCompare(b.commonName, 'en')) : [];
  });
  constructor() { this.load(); }
  load(): void {
    this.loading.set(true); this.error.set(false);
    this.api.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: catalog => { this.catalog.set(catalog); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }
}
