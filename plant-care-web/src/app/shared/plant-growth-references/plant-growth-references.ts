import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PlantCatalogProfile } from '../../models/plant-species.model';
import { PlantPhoto } from '../plant-photo/plant-photo';

@Component({
  selector: 'app-plant-growth-references', standalone: true,
  imports: [PlantPhoto],
  template: `
    @if (profile()?.growthReferences?.length) {
      <section aria-labelledby="growth-reference-heading">
        <h2 id="growth-reference-heading">Growth references</h2>
        <p>These photos show different specimens, not a before-and-after record of one plant.
          Growth varies with conditions; use these examples to recognize features, not to judge
          your plant's age or diagnose its health.</p>
        <div class="references">
          @for (reference of profile()!.growthReferences!; track reference.photo.path) {
            <article>
              <h3>{{ reference.title }}</h3>
              <app-plant-photo [photo]="reference.photo" [detail]="true" />
              <p>{{ reference.description }}</p>
            </article>
          }
        </div>
        <p class="source"><a [href]="profile()!.careSourceUrl" target="_blank" rel="noopener noreferrer">Growth reference source</a>
          · Reviewed {{ profile()!.reviewedOn }}</p>
      </section>
    }
  `,
  styles: `
    :host{display:block;min-width:0}section{margin:2rem 0;color:#263c2d}h2{font-size:1.5rem}p{line-height:1.6}
    .references{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,280px),1fr));gap:1.5rem}
    article{min-width:0;padding:1rem;border:1px solid #d6e3d7;border-radius:.75rem;background:#f8fbf8}
    h3{margin-top:0;font-size:1.1rem}.source{font-size:.875rem}a{color:#235b37}a:focus-visible{outline:3px solid #235b37;outline-offset:3px}
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantGrowthReferences {
  readonly profile = input<PlantCatalogProfile | null | undefined>(null);
}
