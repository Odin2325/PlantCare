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
        @if (!photo() && profile()?.photoIsSpeciesReference) {
          <p class="reference-note">Species reference photo; this specimen is not verified as the named cultivar.</p>
        }
      } @else if (cultivarPending()) {
        <div class="placeholder" role="img" [attr.aria-label]="'Cultivar photo pending: ' + profile()!.cultivarName">
          <span aria-hidden="true">🌿</span>Cultivar photo pending<strong>{{ profile()!.cultivarName }}</strong>
        </div>
      } @else { <div class="placeholder" role="img" aria-label="No plant photo available"><span aria-hidden="true">🌿</span>No photo yet</div> }
    </figure>
  `,
  styles: `
    :host{display:block;min-width:0}figure{margin:0 0 1rem}img,.placeholder{display:block;width:100%;height:180px;border-radius:.65rem;background:var(--color-tint);object-fit:contain}
    .detail img,.detail .placeholder{height:320px}.placeholder{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:.5rem;color:var(--color-muted)}.placeholder span{font-size:2.5rem}
    figcaption{font-size:.75rem;line-height:1.5;color:var(--color-muted);margin-top:.4rem;overflow-wrap:anywhere}a{color:var(--color-primary)}
    .reference-note{font-size:.875rem;line-height:1.5;color:var(--color-muted);margin:.4rem 0 0}
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantPhoto {
  readonly profile = input<PlantCatalogProfile | null | undefined>(null);
  readonly photo = input<PlantCatalogPhoto | null | undefined>(null);
  readonly detail = input(false);
  readonly selectedPhoto = computed(() => this.photo() ??
    (this.profile()?.cultivarName && this.profile()?.photoIsSpeciesReference ? null : this.profile()?.photo ?? null));
  readonly cultivarPending = computed(() => !this.photo() && !!this.profile()?.cultivarName && !this.selectedPhoto());
  readonly failedPath = signal<string | null>(null);
}
