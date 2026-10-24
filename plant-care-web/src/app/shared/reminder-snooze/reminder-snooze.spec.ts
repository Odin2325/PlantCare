import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject } from 'rxjs';
import { afterEach, expect, it, vi } from 'vitest';
import { ReminderSnooze } from './reminder-snooze';
import { NotificationsApiService } from '../../features/notifications/data-access/notifications-api.service';
import { CareNotification } from '../../features/notifications/models/notification.model';

afterEach(() => TestBed.resetTestingModule());
const original: CareNotification = { id: 'original', userPlantId: 'plant', plantName: 'Kitchen fern',
  actionType: 'Watering', dueAtUtc: '2026-10-24T08:00:00Z', createdAtUtc: '2026-10-24T08:00:00Z', readAtUtc: null, canSnooze: true };
function create() {
  const pending = new Subject<CareNotification>(); const snooze = vi.fn(() => pending);
  TestBed.configureTestingModule({ providers: [{ provide: NotificationsApiService, useValue: { snooze } }] });
  const fixture = TestBed.createComponent(ReminderSnooze);
  fixture.componentRef.setInput('notification', original); fixture.detectChanges();
  return { fixture, pending, snooze };
}
it('waits for server success, blocks double submits and replaces controls with the saved local date', () => {
  const { fixture, pending, snooze } = create();
  fixture.componentInstance.snooze(24); fixture.componentInstance.snooze(24); fixture.detectChanges();
  expect(snooze).toHaveBeenCalledExactlyOnceWith('original', 24);
  expect(fixture.nativeElement.textContent).not.toContain('Reminder snoozed until');
  pending.next({ ...original, id: 'follow-up', snoozedUntilUtc: '2026-10-25T10:00:00Z', canSnooze: false });
  pending.complete(); fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Reminder snoozed until');
  expect(fixture.nativeElement.textContent).toContain('Care is still due.');
  expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(0);
});
it('does not claim a saved snooze on network failure or a stale request', () => {
  const { fixture, pending } = create(); fixture.componentInstance.snooze(1);
  pending.error(new HttpErrorResponse({ status: 409 })); fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('This reminder changed.');
  expect(fixture.nativeElement.textContent).not.toContain('Reminder snoozed until');
  expect(fixture.componentInstance.busy()).toBe(false);
});
it('offers no action for obsolete or completed care', () => {
  const { fixture } = create(); fixture.componentRef.setInput('notification', { ...original, canSnooze: false }); fixture.detectChanges();
  expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(0);
});
