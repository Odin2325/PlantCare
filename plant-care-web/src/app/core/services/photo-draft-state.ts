import { Injectable, inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';

@Injectable({providedIn:'root'})
export class PhotoDraftState {
  private readonly checks = new Set<() => boolean>();
  reloadApproved = false;
  register(check: () => boolean): () => void { this.checks.add(check); return () => { this.checks.delete(check); }; }
  pending(): boolean { return [...this.checks].some(check => check()); }
  confirmLeave(): boolean {
    return !this.pending() || window.confirm('Leave this photo draft? Unsaved changes will be lost. An uncertain upload may still finish.');
  }
}
export const photoDraftGuard: CanDeactivateFn<unknown> = () => inject(PhotoDraftState).confirmLeave();
