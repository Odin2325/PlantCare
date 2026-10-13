import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate } from '@angular/service-worker';

@Injectable({ providedIn: 'root' })
export class PwaUpdateService {
  private readonly updates = inject(SwUpdate);
  private readonly destroyRef = inject(DestroyRef);
  private checking = false;
  readonly updateAvailable = signal(false);
  readonly isOnline = signal(navigator.onLine);
  readonly recoveryRequired = signal(false);

  constructor() {
    const online = () => { this.isOnline.set(true); this.checkForUpdate(); };
    const offline = () => this.isOnline.set(false);
    const visible = () => {
      if (document.visibilityState === 'visible') {
        this.isOnline.set(navigator.onLine);
        this.checkForUpdate();
      }
    };
    window.addEventListener('online', online);
    window.addEventListener('offline', offline);
    document.addEventListener('visibilitychange', visible);
    this.destroyRef.onDestroy(() => {
      window.removeEventListener('online', online);
      window.removeEventListener('offline', offline);
      document.removeEventListener('visibilitychange', visible);
    });
    if (this.updates.isEnabled) {
      this.updates.versionUpdates.pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(event => { if (event.type === 'VERSION_READY') this.updateAvailable.set(true); });
      this.updates.unrecoverable.pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.recoveryRequired.set(true));
    }
  }

  reloadForUpdate(): void {
    if (!this.isOnline()) return;
    // A full navigation switches versions atomically. Activating before reload
    // can mix the old document with newly versioned lazy chunks.
    document.location.reload();
  }

  private checkForUpdate(): void {
    if (!this.updates.isEnabled || !this.isOnline() || this.checking) return;
    this.checking = true;
    // Keep the current version on a failed background check. A later foreground
    // or reconnect event retries without reloading unfinished user work.
    void this.updates.checkForUpdate().catch(() => undefined)
      .finally(() => { this.checking = false; });
  }
}
