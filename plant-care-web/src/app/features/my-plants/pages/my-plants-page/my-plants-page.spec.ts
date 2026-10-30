import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { afterEach, expect, it, vi } from 'vitest';
import { MyPlantsPage } from './my-plants-page';

it('finds an owned plant by an alternative name while preserving its nickname and collection filters', () => {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
  const fixture = TestBed.createComponent(MyPlantsPage);
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/my-plants').flush([
    { id: 'a', nickname: 'Desk companion', speciesCommonName: 'Golden pothos', speciesScientificName: 'Epipremnum aureum',
      location: 'Office', notes: null, tags: ['favorite'], careSchedules: [],
      speciesCatalogProfile: { careSourceUrl: 'https://example.org/care', reviewedOn: '2026-10-22', photo: null,
        commonNameAliases: ["Devil's ivy"] } },
    { id: 'b', nickname: 'Kitchen fern', speciesCommonName: 'Fern', speciesScientificName: null,
      location: 'Kitchen', notes: null, tags: [], careSchedules: [] },
  ]);
  fixture.componentInstance.searchTerm.set('Devils ivy');
  expect(fixture.componentInstance.filteredPlants().map(p => p.nickname)).toEqual(['Desk companion']);
  fixture.componentInstance.locationFilter.set('Kitchen');
  expect(fixture.componentInstance.filteredPlants()).toEqual([]);
  fixture.componentInstance.locationFilter.set('Office');
  fixture.componentInstance.tagFilter.set('favorite');
  expect(fixture.componentInstance.filteredPlants().map(p => p.id)).toEqual(['a']);
  fixture.componentInstance.searchTerm.set('Epipremnum aureum');
  expect(fixture.componentInstance.filteredPlants().map(p => p.nickname)).toEqual(['Desk companion']);
  fixture.componentInstance.searchTerm.set('Kitchen fern'); fixture.componentInstance.locationFilter.set('');
  fixture.componentInstance.tagFilter.set('');
  expect(fixture.componentInstance.filteredPlants().map(p => p.id)).toEqual(['b']);
  http.verify();
});


// Calendar days use the browser's local zone, matching the visible care badges.
afterEach(() => { TestBed.resetTestingModule(); vi.useRealTimers(); });
function care(due: Date | null, enabled = true) {
  return { id: 'schedule', actionType: 'Watering', intervalDays: 7, lastCompletedAtUtc: null,
    nextDueAtUtc: due?.toISOString() ?? null, isEnabled: enabled, scheduleMode: 'Interval',
    weekDays: 0, preferredTimeLocal: null, timeZoneId: null };
}
function owned(id: string, schedules: ReturnType<typeof care>[]) {
  return { id, nickname: id, speciesCommonName: 'Test plant', speciesScientificName: null,
    location: 'Office', notes: null, tags: ['green'], createdAtUtc: '2026-01-01T00:00:00Z',
    careSchedules: schedules };
}
function collection(plants: ReturnType<typeof owned>[]) {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
  const fixture = TestBed.createComponent(MyPlantsPage);
  const http = TestBed.inject(HttpTestingController); http.expectOne('/api/my-plants').flush(plants);
  return { fixture, page: fixture.componentInstance, http };
}
it('prioritizes due care across multiple schedules and excludes paused, undated and invalid dates', () => {
  vi.useFakeTimers(); vi.setSystemTime(new Date(2026, 0, 15, 12));
  const yesterday = new Date(2026, 0, 14, 23), today = new Date(2026, 0, 15, 23), future = new Date(2026, 0, 17, 9);
  const { fixture, page, http } = collection([
    owned('Mixed', [care(future), care(yesterday)]), owned('Today', [care(today)]),
    owned('Future', [care(future)]), owned('Paused', [care(yesterday, false)]),
    owned('Undated', [care(null)]), owned('None', []),
  ]);
  page.setCareStatusFilter('needs-care'); expect(page.filteredPlants().map(p => p.nickname)).toEqual(['Mixed', 'Today']);
  page.setCareStatusFilter('upcoming'); expect(page.filteredPlants().map(p => p.nickname)).toEqual(['Future']);
  page.setCareStatusFilter('undated'); expect(page.filteredPlants().map(p => p.nickname)).toEqual(['None', 'Paused', 'Undated']);
  expect(page.getPlantCareStatus({ ...page.userPlants()[0], careSchedules: [{ ...page.userPlants()[0].careSchedules[0], nextDueAtUtc: 'invalid' }] })).toBe('undated');
  http.verify(); fixture.destroy();
});
it('combines care status with collection filters and clears only view controls without writing schedules', () => {
  vi.useFakeTimers(); vi.setSystemTime(new Date(2026, 0, 15, 12));
  const { fixture, page, http } = collection([owned('Desk', [care(new Date(2026, 0, 15, 9))]), owned('Shelf', [care(new Date(2026, 0, 16, 9))])]);
  const before = JSON.stringify(page.userPlants()); page.setCareStatusFilter('needs-care');
  page.setSearchTerm('Desk'); page.setLocationFilter('Office'); page.setTagFilter('green');
  expect(page.filteredPlants().map(p => p.nickname)).toEqual(['Desk']);
  page.setLocationFilter('Kitchen'); expect(page.filteredPlants()).toEqual([]);
  page.clearFilters(); expect(page.hasFilters()).toBe(false); expect(page.filteredPlants()).toHaveLength(2);
  page.setCareStatusFilter('invalid'); expect(page.careStatusFilter()).toBe('');
  expect(JSON.stringify(page.userPlants())).toBe(before); http.verify(); fixture.destroy();
});
it('refreshes filtered care and badges across a local midnight and tears down its clock subscription', () => {
  vi.useFakeTimers(); vi.setSystemTime(new Date(2026, 0, 15, 23, 59, 30));
  const { fixture, page, http } = collection([owned('Tomorrow', [care(new Date(2026, 0, 16, 9))])]);
  page.setCareStatusFilter('needs-care'); expect(page.filteredPlants()).toEqual([]);
  vi.advanceTimersByTime(60_000); expect(page.filteredPlants().map(p => p.nickname)).toEqual(['Tomorrow']);
  expect(page.getCareStatusText(page.userPlants()[0].careSchedules[0])).toBe('Due today');
  http.verify(); fixture.destroy(); expect(vi.getTimerCount()).toBe(0);
});

it('previews without writing, cancels, invalidates edited drafts and applies only the reviewed request', () => {
  const { page, http, fixture } = collection([owned('a', [care(new Date('2026-11-07T09:00:00Z'))])]);
  const plant = page.userPlants()[0], schedule = plant.careSchedules[0];
  page.saveSchedule(plant, schedule);
  const preview = http.expectOne('/api/my-plants/a/care/Watering/schedule/preview');
  expect(preview.request.method).toBe('POST'); preview.flush({ ...schedule, intervalDays: 14 });
  expect(page.userPlants()[0].careSchedules[0].intervalDays).toBe(7);
  http.expectNone(r => r.method === 'PUT'); page.cancelSchedulePreview();
  page.applySchedulePreview(); http.expectNone(r => r.method === 'PUT');
  page.saveSchedule(plant,schedule); http.expectOne('/api/my-plants/a/care/Watering/schedule/preview').flush(schedule);
  page.setScheduleModeDraft(plant,schedule,'Weekdays'); expect(page.schedulePreview()).toBeNull();
  page.setScheduleModeDraft(plant,schedule,'Interval'); page.saveSchedule(plant,schedule);
  const again=http.expectOne('/api/my-plants/a/care/Watering/schedule/preview');const request=again.request.body;again.flush(schedule);
  page.applySchedulePreview();const apply=http.expectOne('/api/my-plants/a/care/Watering/schedule');
  expect(apply.request.method).toBe('PUT');expect(apply.request.body).toEqual(request);apply.flush(schedule);
  expect(page.schedulePreview()).toBeNull();http.verify();fixture.destroy();
});
it('keeps failed drafts and rejects a preview after care changes in the collection', () => {
  const { page,http,fixture }=collection([owned('a',[care(new Date('2026-11-07T09:00:00Z'))])]);
  const plant=page.userPlants()[0],schedule=plant.careSchedules[0];
  page.setIntervalDraft(plant,schedule,{target:{value:'14'}} as unknown as Event);page.saveSchedule(plant,schedule);
  http.expectOne('/api/my-plants/a/care/Watering/schedule/preview').flush({}, {status:503,statusText:'Unavailable'});
  expect(page.getIntervalDraft(plant,schedule)).toBe(14);expect(page.schedulePreview()).toBeNull();
  page.saveSchedule(plant,schedule);http.expectOne('/api/my-plants/a/care/Watering/schedule/preview').flush({...schedule,intervalDays:14});
  page.userPlants.set([{...plant,careSchedules:[{...schedule,lastCompletedAtUtc:'2026-10-30T09:00:00Z'}]}]);
  page.applySchedulePreview();http.expectNone(r=>r.method==='PUT');expect(page.careActionError()).toContain('Care has changed');
  http.verify();fixture.destroy();
});
