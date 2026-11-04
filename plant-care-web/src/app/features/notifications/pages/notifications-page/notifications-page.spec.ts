import { TestBed } from '@angular/core/testing';
import { SwPush } from '@angular/service-worker';
import { Observable, Subject, of, throwError } from 'rxjs';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { NotificationsPage } from './notifications-page';
import { NotificationsApiService } from '../../data-access/notifications-api.service';

beforeEach(() => {
  vi.stubGlobal('Notification', { permission: 'default' });
  vi.stubGlobal('PushManager', class {});
  vi.stubGlobal('navigator', Object.create(navigator, { serviceWorker: { value: {} } }));
});
afterEach(() => { TestBed.resetTestingModule(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });
function create(enabled: boolean, subscription: Observable<PushSubscription | null>) {
  TestBed.configureTestingModule({ providers: [
    { provide: NotificationsApiService, useValue: { getNotifications: () => of([]), getPreferences: () => of(null), getPushConfig: () => of({ isEnabled: enabled, publicKey: 'public-fixture-key' }) } },
    { provide: SwPush, useValue: { isEnabled: true, subscription } },
  ] });
  return TestBed.createComponent(NotificationsPage);
}
it('does not query browser subscriptions when the server disables push', () => {
  const queried = vi.fn();
  const fixture = create(false, new Observable(() => { queried(); throw new Error('must not query'); }));
  expect(queried).not.toHaveBeenCalled();
  expect(fixture.componentInstance.pushAvailable()).toBe(false);
  expect(fixture.componentInstance.pushEnabled()).toBe(false);
});
it('handles a browser subscription failure without losing in-app reminders', () => {
  const fixture = create(true, throwError(() => new DOMException('Subscription unavailable', 'AbortError')));
  expect(fixture.componentInstance.pushEnabled()).toBe(false);
  expect(fixture.componentInstance.pushMessage()).toContain('In-app reminders are still available');
  expect(fixture.componentInstance.errorMessage()).toBeNull();
});
it('cancels a pending subscription lookup when the page is destroyed', () => {
  const subscription = new Subject<PushSubscription | null>();
  const fixture = create(true, subscription);
  expect(subscription.observed).toBe(true);
  fixture.destroy();
  expect(subscription.observed).toBe(false);
});

it('keeps in-app reminders available without the browser Notification API', () => {
  vi.stubGlobal('Notification', undefined);
  const queried = vi.fn();
  const fixture = create(true, new Observable(() => { queried(); }));
  expect(queried).not.toHaveBeenCalled();
  expect(fixture.componentInstance.pushAvailable()).toBe(false);
  expect(fixture.componentInstance.errorMessage()).toBeNull();
});

it('does not offer push when PushManager is missing', () => {
  vi.stubGlobal('PushManager', undefined);
  const queried = vi.fn();
  const fixture = create(true, new Observable(() => { queried(); }));
  expect(queried).not.toHaveBeenCalled();
  expect(fixture.componentInstance.pushAvailable()).toBe(false);
});
