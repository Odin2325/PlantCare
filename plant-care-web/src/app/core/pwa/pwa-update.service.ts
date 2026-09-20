import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PwaUpdateService {
  private readonly updates = inject(SwUpdate);
  readonly updateAvailable = signal(false);
  readonly isOnline = signal(navigator.onLine);

  constructor() {
    window.addEventListener('online', () => this.isOnline.set(true));
    window.addEventListener('offline', () => this.isOnline.set(false));
    if (this.updates.isEnabled) {
      this.updates.versionUpdates.pipe(
        filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'),
      ).subscribe(() => this.updateAvailable.set(true));
    }
  }

  async activateUpdate(): Promise<void> {
    await this.updates.activateUpdate();
    document.location.reload();
  }
}
