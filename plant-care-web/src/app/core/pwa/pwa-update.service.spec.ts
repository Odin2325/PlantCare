import { TestBed } from '@angular/core/testing';
import { SwUpdate, VersionEvent } from '@angular/service-worker';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PwaUpdateService } from './pwa-update.service';

describe('PWA update lifecycle', () => {
  let versions: Subject<VersionEvent>;
  let unrecoverable: Subject<unknown>;
  let check: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    versions = new Subject(); unrecoverable = new Subject();
    check = vi.fn(() => Promise.resolve(false));
    vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(true);
    TestBed.configureTestingModule({ providers: [{ provide: SwUpdate, useValue: { isEnabled: true, versionUpdates: versions, unrecoverable, checkForUpdate: check } }] });
  });
  afterEach(() => { TestBed.resetTestingModule(); vi.restoreAllMocks(); });
  it('offers only ready versions and exposes unrecoverable state', () => {
    const service = TestBed.inject(PwaUpdateService);
    versions.next({ type: 'VERSION_DETECTED', version: { hash: 'new' } });
    expect(service.updateAvailable()).toBe(false);
    versions.next({ type: 'VERSION_READY', currentVersion: { hash: 'old' }, latestVersion: { hash: 'new' } });
    expect(service.updateAvailable()).toBe(true);
    unrecoverable.next({ reason: 'missing asset' });
    expect(service.recoveryRequired()).toBe(true);
  });
  it('checks on reconnect, coalesces checks and retries after failure', async () => {
    check.mockRejectedValueOnce(new Error('offline'));
    const service = TestBed.inject(PwaUpdateService);
    window.dispatchEvent(new Event('offline'));
    expect(service.isOnline()).toBe(false);
    window.dispatchEvent(new Event('online')); window.dispatchEvent(new Event('online'));
    expect(check).toHaveBeenCalledTimes(1);
    await Promise.resolve(); await Promise.resolve(); await Promise.resolve();
    window.dispatchEvent(new Event('online'));
    expect(check).toHaveBeenCalledTimes(2);
    expect(service.isOnline()).toBe(true);
  });
  it('removes browser listeners and subscriptions when destroyed', () => {
    const service = TestBed.inject(PwaUpdateService);
    TestBed.resetTestingModule();
    window.dispatchEvent(new Event('online'));
    unrecoverable.next({ reason: 'ignored after destruction' });
    expect(check).not.toHaveBeenCalled(); expect(service.recoveryRequired()).toBe(false);
  });
  it('reconciles missed network events when returning to a visible tab', () => {
    const service = TestBed.inject(PwaUpdateService);
    window.dispatchEvent(new Event('offline'));
    vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible');
    document.dispatchEvent(new Event('visibilitychange'));
    expect(service.isOnline()).toBe(true); expect(check).toHaveBeenCalledTimes(1);
  });
});
