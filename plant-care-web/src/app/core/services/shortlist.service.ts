import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { AuthService } from '../auth/services/auth.service';
import { environment } from '../../../environments/environment';

interface ShortlistItem { plantSpeciesId: string; savedAtUtc: string; }

@Injectable({ providedIn: 'root' })
export class ShortlistService {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/api/shortlist`;
  private readonly savedIds = signal<string[]>([]);
  private readonly stateOwner = signal<string | null>(null);
  private readonly loaded = signal(false);
  // Hide the former account's list synchronously, before the auth-change effect
  // has a chance to cancel requests and load the new account's data.
  readonly ids = computed(() => this.stateOwner() === this.auth.currentUser()?.id ? this.savedIds() : []);
  readonly signedIn = computed(() => Boolean(this.auth.currentUser()));
  readonly loading = signal(false);
  readonly ready = computed(() => Boolean(this.auth.currentUser()?.id) &&
    this.stateOwner() === this.auth.currentUser()?.id && this.loaded());
  readonly busy = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal('');
  private generation = 0;
  private loadRequest?: Subscription;
  private changeRequest?: Subscription;

  constructor() {
    effect(onCleanup => {
      this.auth.currentUser();
      untracked(() => {
        this.generation++;
        this.savedIds.set([]); this.loaded.set(false); this.loading.set(false);
        this.stateOwner.set(this.auth.currentUser()?.id ?? null);
        this.busy.set(null); this.error.set(null); this.message.set('');
        this.refresh();
      });
      onCleanup(() => { this.loadRequest?.unsubscribe(); this.changeRequest?.unsubscribe(); });
    });
  }

  has(id: string): boolean { return this.ids().includes(id); }

  refresh(): void {
    const owner = this.auth.currentUser()?.id, generation = this.generation;
    if (!owner || this.busy()) return;
    this.loadRequest?.unsubscribe(); this.loading.set(true); this.loaded.set(false);
    this.error.set(null); this.message.set('');
    this.loadRequest = this.http.get<ShortlistItem[]>(this.endpoint).subscribe({
      next: items => {
        if (!this.isCurrent(owner, generation)) return;
        this.savedIds.set([...new Set(items.map(item => item.plantSpeciesId))]);
        this.loaded.set(true); this.loading.set(false);
      },
      error: () => {
        if (!this.isCurrent(owner, generation)) return;
        this.loading.set(false); this.error.set('Your shortlist could not be loaded. Refresh it, or sign in again if your session has expired.');
      },
    });
  }

  toggle(id: string): void {
    const owner = this.auth.currentUser()?.id, generation = this.generation;
    if (!owner || !this.ready() || this.loading() || this.busy()) return;
    const removing = this.has(id);
    this.busy.set(id); this.error.set(null); this.message.set('');
    const url = `${this.endpoint}/${encodeURIComponent(id)}`;
    this.changeRequest = (removing ? this.http.delete<void>(url) : this.http.put<void>(url, {})).subscribe({
      next: () => {
        if (!this.isCurrent(owner, generation)) return;
        this.savedIds.update(ids => removing ? ids.filter(value => value !== id) : [...new Set([...ids, id])]);
        this.busy.set(null); this.message.set(removing ? 'Plant removed from your shortlist.' : 'Plant saved to your shortlist.');
      },
      error: () => {
        if (!this.isCurrent(owner, generation)) return;
        this.busy.set(null); this.error.set('The shortlist change could not be confirmed. Refresh your shortlist before trying again.');
        this.loaded.set(false);
      },
    });
  }

  private isCurrent(owner: string, generation: number): boolean {
    return this.auth.currentUser()?.id === owner && this.generation === generation;
  }
}
