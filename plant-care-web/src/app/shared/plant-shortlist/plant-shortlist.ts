import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { parseComparisonIds } from '../plant-comparison';
import { ShortlistService } from '../../core/services/shortlist.service';
import { PlantSpecies } from '../../models/plant-species.model';

@Component({
  selector: 'app-shortlist-notice', standalone: true, imports: [RouterLink],
  template: `
    <section aria-label="Your plant shortlist">
      <h2>Your plant shortlist</h2>
      <p>Keep plants you are considering in a private list. Saving a plant does not add it to My plants or create care reminders.</p>
      @if (shortlist.signedIn()) {
        <p>Your shortlist is saved to your account and can be opened on your other devices.</p>
        @if (shortlist.loading()) { <p role="status">Loading your shortlist…</p> }
        @else if (shortlist.ready()) { <p>{{ shortlist.ids().length }} saved catalog {{ shortlist.ids().length === 1 ? 'plant' : 'plants' }}</p> }
        <a routerLink="/plants" [queryParams]="{shortlisted: '1', compare: comparison() || null}">View full shortlist</a>
        <button type="button" [disabled]="shortlist.loading() || !!shortlist.busy()" (click)="shortlist.refresh()">Refresh shortlist</button>
        @if (shortlist.error()) { <p role="alert">{{ shortlist.error() }}</p> }
        <p role="status">{{ shortlist.message() }}</p>
      } @else {
        <a routerLink="/login" [queryParams]="{returnUrl: '/plants?shortlisted=1'}">Log in to save a shortlist</a>
      }
    </section>
  `,
  styles: `:host{display:block;margin-block:1.5rem;min-width:0}section{padding:1rem;border:1px solid #bbc9be;border-radius:.7rem;background:#f2f7f3;color:#263c2d}h2{margin-top:0;font-size:1.3rem}p{line-height:1.6;overflow-wrap:anywhere}a{color:#235b37}button{font:inherit;padding:.65rem;margin:.5rem;border:1px solid #235b37;border-radius:.4rem;background:white;color:#235b37}a:focus-visible,button:focus-visible{outline:3px solid #235b37;outline-offset:3px}`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShortlistNotice {
  readonly shortlist = inject(ShortlistService);
  readonly comparison = toSignal(inject(ActivatedRoute).queryParamMap.pipe(
    map(params => parseComparisonIds(params.get('compare')).join(','))), { initialValue: '' });
}

@Component({
  selector: 'app-shortlist-button', standalone: true,
  template: `@if (shortlist.signedIn()) {
    <button type="button" [attr.aria-pressed]="shortlist.has(plant().id)"
      [attr.aria-label]="(shortlist.has(plant().id) ? 'Remove ' : 'Save ') + plant().commonName + (shortlist.has(plant().id) ? ' from shortlist' : ' to shortlist')"
      [disabled]="!shortlist.ready() || shortlist.loading() || !!shortlist.busy()" (click)="shortlist.toggle(plant().id)">
      {{ shortlist.busy() === plant().id ? 'Saving…' : shortlist.has(plant().id) ? 'Saved to shortlist' : 'Save to shortlist' }}
    </button>
  }`,
  styles: `:host{display:block;margin-block:.75rem}button{font:inherit;padding:.65rem;border:1px solid #235b37;border-radius:.4rem;background:white;color:#235b37;max-width:100%;overflow-wrap:anywhere}button[aria-pressed=true]{background:#235b37;color:white}button:focus-visible{outline:3px solid #235b37;outline-offset:3px}`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShortlistButton {
  readonly plant = input.required<PlantSpecies>();
  readonly shortlist = inject(ShortlistService);
}
