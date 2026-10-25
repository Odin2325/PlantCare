import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { CareHistoryPage } from './care-history-page';
import { CareEventHistory } from '../../models/user-plant.model';

describe('CareHistoryPage filters', () => {
  const id = 'a1111111-1111-4111-8111-111111111111';
  let http: HttpTestingController;
  const events: CareEventHistory[] = [
    { id: 'a', careScheduleId: 'watering', actionType: 'Watering', completedAtUtc: new Date(2026, 9, 20, 0, 0).toISOString(), recordedAtUtc: '2026-10-21T12:00:00Z', notes: 'Dry surface' },
    { id: 'b', careScheduleId: 'pruning', actionType: 'Pruning', completedAtUtc: new Date(2026, 9, 20, 23, 59).toISOString(), recordedAtUtc: '2026-10-21T12:00:00Z', notes: 'Removed DRY leaves' },
    { id: 'c', careScheduleId: 'watering', actionType: 'Watering', completedAtUtc: new Date(2026, 9, 21, 0, 0).toISOString(), recordedAtUtc: '2026-10-21T12:00:00Z', notes: null },
  ];
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id }) } } }],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });
  function load(history = events) {
    const fixture = TestBed.createComponent(CareHistoryPage);
    http.expectOne(`/api/my-plants/${id}`).flush({ id, nickname: '', speciesCommonName: 'Boston fern', careSchedules: [] });
    http.expectOne(`/api/my-plants/${id}/care/history?take=100`).flush(history);
    fixture.detectChanges();
    return fixture;
  }
  it('combines action, notes and inclusive device-local completion days without writing or reordering saved entries', () => {
    const fixture = load(); const page = fixture.componentInstance;
    expect(page.filteredHistory().map(e => e.id)).toEqual(['c', 'b', 'a']);
    page.completedFrom.set('2026-10-20'); page.completedThrough.set('2026-10-20');
    expect(page.filteredHistory().map(e => e.id)).toEqual(['b', 'a']);
    page.notesFilter.set('  dRy  '); page.actionFilter.set('Watering');
    expect(page.filteredHistory().map(e => e.id)).toEqual(['a']);
    page.actionFilter.set(''); page.historyOrder.set('oldest');
    expect(page.filteredHistory().map(e => e.id)).toEqual(['a', 'b']);
    expect(page.history()).toEqual(events);
    page.clearHistoryFilters();
    expect(page.filteredHistory().map(e => e.id)).toEqual(['c', 'b', 'a']);
    expect(page.hasHistoryFilters()).toBe(false);
    expect(fixture.nativeElement.querySelector('h1').textContent.trim()).toBe('Boston fern');
  });
  it('distinguishes an empty filter from no history and explains a reversed range', () => {
    const fixture = load(); const page = fixture.componentInstance;
    page.completedFrom.set('2026-10-21'); page.completedThrough.set('2026-10-20'); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('end date');
    expect(fixture.nativeElement.querySelector('.empty-history h3').textContent).toContain('match these filters');
    page.clearHistoryFilters(); page.notesFilter.set('not present'); fixture.detectChanges();
    expect(page.filteredHistory()).toEqual([]);
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain('0 of 3');
    expect(fixture.nativeElement.textContent).toContain('100 most recent');
    page.history.set([]); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.empty-history h3').textContent).toContain('No care recorded yet');
  });
  it('keeps an edit draft visible by disabling view controls until editing finishes', () => {
    const fixture = load(); const page = fixture.componentInstance;
    page.beginEdit(events[0]); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.history-filters').disabled).toBe(true);
    expect(page.editForm.controls.notes.value).toBe('Dry surface');
    page.cancelEdit(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.history-filters').disabled).toBe(false);
    expect(page.history()).toEqual(events);
  });
});
