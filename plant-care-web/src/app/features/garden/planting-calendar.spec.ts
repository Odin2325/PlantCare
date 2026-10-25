import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, describe, expect, it } from 'vitest';
import { PlantingCalendar, validPlanDate } from './planting-calendar';
import { UserPlant } from '../my-plants/models/user-plant.model';

describe('Planting calendar', () => {
  afterEach(() => TestBed.resetTestingModule());
  function setup(plants: Partial<UserPlant>[]) {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(PlantingCalendar);
    fixture.componentRef.setInput('plants', plants);
    return { fixture, calendar: fixture.componentInstance };
  }
  it('groups all three targets, uses nicknames and links to details without changing data', () => {
    const plants = [{ id: 'a', nickname: 'Balcony crop', speciesCommonName: 'Tomato', isActive: true,
      plannedSowingOn: '2028-02-29', plannedPlantingOutOn: '2028-02-29', expectedHarvestOn: '2028-03-01' },
    { id: 'b', nickname: '', speciesCommonName: 'Fig', isActive: true, expectedHarvestOn: '2028-02-29' },
    { id: 'old', isActive: false, expectedHarvestOn: '2028-02-29' }];
    const before = JSON.stringify(plants);
    const { calendar, fixture } = setup(plants); calendar.setMonth('2028-02'); fixture.detectChanges();
    expect(calendar.days()).toHaveLength(1);
    expect(calendar.days()[0].events.map(event => event.name)).toEqual(['Balcony crop', 'Balcony crop', 'Fig']);
    expect(fixture.nativeElement.querySelectorAll('a')).toHaveLength(3);
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('/my-plants/a');
    expect(fixture.nativeElement.textContent).toContain('Expected harvest');
    expect(JSON.stringify(plants)).toBe(before);
    fixture.componentRef.setInput('plants', []); fixture.detectChanges();
    expect(calendar.days()).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('No planting dates in this month');
  });
  it('navigates year boundaries and rejects malformed months and impossible dates', () => {
    const { calendar } = setup([{ id: 'a', isActive: true, plannedSowingOn: '2027-02-29',
      expectedHarvestOn: '2027-02-28T23:00:00Z' }]);
    calendar.setMonth('2027-02'); expect(calendar.days()).toEqual([]);
    calendar.setMonth('2027-12'); calendar.moveMonth(1); expect(calendar.month()).toBe('2028-01');
    calendar.moveMonth(-1); expect(calendar.month()).toBe('2027-12');
    for (const value of ['', '2028-13', '2028-2', '0000-01']) calendar.setMonth(value);
    expect(calendar.month()).toBe('2027-12');
    calendar.setMonth('0001-01'); calendar.moveMonth(-1); expect(calendar.month()).toBe('0001-01');
    calendar.setMonth('9999-12'); calendar.moveMonth(1); expect(calendar.month()).toBe('9999-12');
    expect(validPlanDate('2028-02-29')).toBe(true); expect(validPlanDate('2027-04-31')).toBe(false);
    calendar.goToCurrentMonth(); expect(calendar.month()).toMatch(/^\d{4}-\d{2}$/);
  });
});
