import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { CalendarPage } from './calendar-page';
import { CalendarApiService } from '../../data-access/calendar-api.service';
import { MyPlantsApiService } from '../../../my-plants/data-access/my-plants-api.service';
import { CalendarShare } from '../../models/calendar.model';

const share: CalendarShare = { id: 'shared-calendar', ownerEmail: 'owner@example.test',
  recipientEmail: 'friend@gmail.com', createdAtUtc: '2026-10-23T08:00:00Z', isOwnedByCurrentUser: false };
function setup(query: Record<string, string> = {}, shares: CalendarShare[] = []) {
  const api = {
    getEntries: vi.fn(() => of([])), getShares: vi.fn(() => of(shares)),
    getSharedEntries: vi.fn(() => of([{ id: 'event', userPlantId: 'plant', plantName: 'Fern',
      actionType: 'Watering', startsAtUtc: new Date().toISOString(), kind: 'Scheduled' }])),
    getSubscriptionStatus: () => of({ isActive: false }), getGoogleStatus: () => of({ isConfigured: false }),
    getMicrosoftStatus: () => of({ isConfigured: false }),
    acceptInvitation: vi.fn(() => of(share)),
    createShare: vi.fn(() => of({ ...share, isOwnedByCurrentUser: true, emailNotificationSent: false })),
    sendShareEmail: vi.fn(() => throwError(() => new Error('Unavailable'))),
  };
  TestBed.configureTestingModule({ providers: [
    { provide: Router, useValue: { navigate: vi.fn(() => Promise.resolve(true)) } },
    { provide: CalendarApiService, useValue: api }, { provide: MyPlantsApiService, useValue: {} },
    { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(query) } } },
  ] });
  return { component: TestBed.createComponent(CalendarPage).componentInstance, api };
}
describe('Calendar sharing emails and deep links', () => {
  it('accepts an invitation and clears its token from the current URL', () => {
    const { component, api } = setup({ invitation: 'private-test-token' }, [share]);
    expect(api.acceptInvitation).toHaveBeenCalledWith('private-test-token');
    expect(component.viewedShare()?.id).toBe(share.id);
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith([], expect.objectContaining({ queryParams: { share: share.id }, replaceUrl: true }));
    expect(api.getEntries).not.toHaveBeenCalled();
  });
  it('opens the invited calendar without racing an own-calendar request', () => {
    const { component, api } = setup({ share: share.id }, [share]);
    expect(component.viewedShare()?.id).toBe(share.id);
    expect(component.entries()[0].plantName).toBe('Fern');
    expect(api.getEntries).not.toHaveBeenCalled();
    expect(api.getSharedEntries).toHaveBeenCalledOnce();
  });
  it('shows unavailable access for a revoked or wrong-account link', () => {
    const { component, api } = setup({ share: share.id });
    expect(component.viewedShare()).toBeNull();
    expect(component.sharingMessage()).toContain('unavailable');
    expect(api.getSharedEntries).not.toHaveBeenCalled();
    expect(api.getEntries).toHaveBeenCalledOnce();
  });
  it('reports email failure separately from the persisted access grant', () => {
    const { component } = setup(); component.recipientEmail.set('friend@gmail.com'); component.createShare();
    expect(component.sharingMessage()).toContain('access was shared, but the email could not be sent');
    expect(component.sharingMessage()).not.toContain('invitation email was sent');
    expect(component.sharingBusy()).toBe(false);
  });
  it('keeps access intact after a failed resend', () => {
    const owned = { ...share, isOwnedByCurrentUser: true };
    const { component } = setup({}, [owned]); component.sendShareEmail(owned);
    expect(component.sharingMessage()).toContain('Access is unchanged');
    expect(component.outgoingShares()).toHaveLength(1);
    expect(component.sharingBusy()).toBe(false);
  });
});
