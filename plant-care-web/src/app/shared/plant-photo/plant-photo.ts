import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { PlantCatalogPhoto, PlantCatalogProfile } from '../../models/plant-species.model';

@Component({
  selector: 'app-plant-photo', standalone: true,
  template: `
    <figure [class.detail]="detail()">
      @if (selectedPhoto(); as p) {
        @if (failedPath() !== p.path) {
          <img [src]="p.path" [alt]="p.altText" width="600" height="400"
               loading="lazy" decoding="async" (error)="failedPath.set(p.path)">
        } @else { <div class="placeholder" role="img" aria-label="Plant photo unavailable"><span aria-hidden="true">🌿</span>Photo unavailable</div> }
        <figcaption>@if (p.title) { {{ p.title }} · } Photo: <a [href]="p.sourceUrl" target="_blank" rel="noopener noreferrer">{{ p.credit }}</a>
          · <a [href]="p.licenseUrl" target="_blank" rel="noopener noreferrer">{{ p.licenseName }}</a></figcaption>
      } @else { <div class="placeholder" role="img" aria-label="No plant photo available"><span aria-hidden="true">🌿</span>No photo yet</div> }
    </figure>
  `,
  styles: `
    :host{display:block;min-width:0}figure{margin:0 0 1rem}img,.placeholder{display:block;width:100%;height:180px;border-radius:.65rem;background:#f0f5f0;object-fit:contain}
    .detail img,.detail .placeholder{height:320px}.placeholder{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:.5rem;color:#526356}.placeholder span{font-size:2.5rem}
    figcaption{font-size:.75rem;line-height:1.5;color:#46564a;margin-top:.4rem;overflow-wrap:anywhere}a{color:#235b37}a:focus-visible{outline:3px solid #235b37;outline-offset:2px}
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantPhoto {
  readonly profile = input<PlantCatalogProfile | null | undefined>(null);
  readonly photo = input<PlantCatalogPhoto | null | undefined>(null);
  readonly detail = input(false);
  readonly selectedPhoto = computed(() => this.photo() ?? this.profile()?.photo ?? null);
  readonly failedPath = signal<string | null>(null);
}
