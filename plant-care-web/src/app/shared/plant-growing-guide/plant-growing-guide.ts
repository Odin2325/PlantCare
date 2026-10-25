import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { PlantCatalogProfile } from '../../models/plant-species.model';
import { plantCategoryLabel } from '../plant-categories';

@Component({
  selector: 'app-plant-growing-guide', standalone: true,
  template: `
    @if (profile(); as item) {
      @if (item.growingNotes || item.harvestInstructions) {
        <section aria-labelledby="growing-guide-heading">
          <h2 id="growing-guide-heading">{{ item.harvestInstructions ? 'Growing and harvest' : 'Growing guide' }}</h2>
          @if (item.categories?.length) { <p>Categories: @for (category of item.categories; track category; let last = $last) { {{ categoryLabel(category) }}{{ last ? '' : ' ·' }} }</p> }
          @if (item.cultivarName) { <p><strong>Cultivar:</strong> {{ item.cultivarName }}</p> }
          @if (item.growingNotes) { <h3>Growing notes</h3><p>{{ item.growingNotes }}</p> }
          @if (item.harvestInstructions) { <h3>Harvest guidance</h3><p>{{ item.harvestInstructions }}</p> }
          <p>Use your local weather, frost risk and seed or nursery label to plan planting.
            Check soil moisture and rainfall before watering. A food-crop category is not a safety assessment of every plant part.</p>
          <a [href]="item.careSourceUrl" target="_blank" rel="noopener noreferrer">Growing guide source</a>
          <span> · Reviewed {{ item.reviewedOn }}</span>
        </section>
      }
    }
  `,
  styles: `:host{display:block;min-width:0}section{margin:2rem 0;padding:1.25rem;border:1px solid #bbc9be;border-radius:.75rem;background:#f2f7f3;color:#263c2d}p{line-height:1.6;overflow-wrap:anywhere}h2{margin-top:0}h3{font-size:1.1rem}a{color:#235b37}a:focus-visible{outline:3px solid #235b37;outline-offset:3px}`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantGrowingGuide {
  readonly profile = input<PlantCatalogProfile | null | undefined>(null);
  readonly categoryLabel = plantCategoryLabel;
}
