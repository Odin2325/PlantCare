import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CareSchedule, UserPlant } from '../my-plants/models/user-plant.model';
import { gardenWorkEntries, localWorkDate, UpcomingWork } from './upcoming-work';

const plant = (overrides: Partial<UserPlant> = {}) => ({ id: 'a', nickname: 'Balcony crop',
  speciesCommonName: 'Tomato', isActive: true, careSchedules: [], ...overrides }) as UserPlant;
const care = (date: string | null, overrides: Partial<CareSchedule> = {}) => ({ id: 'water',
  actionType: 'Watering', nextDueAtUtc: date, isEnabled: true, ...overrides }) as CareSchedule;
const date = (day: number, hour = 12) => new Date(2026, 2, day, hour);

describe('Upcoming garden work', () => {
  afterEach(() => { TestBed.resetTestingModule(); vi.useRealTimers(); });
  function setup(plants: UserPlant[]) {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(UpcomingWork);
    fixture.componentRef.setInput('plants', plants);
    fixture.componentInstance.now.set(date(28));
    return { fixture, page: fixture.componentInstance };
  }
  it('combines each saved target and next active care occurrence, using nicknames without writing or expanding recurrence', () => {
    const plants = [plant({ plannedSowingOn: '2026-03-28', expectedHarvestOn: '2026-04-03',
      careSchedules: [care(date(27).toISOString()), care(date(28).toISOString(), { id: 'feed', actionType: 'Fertilizing' }),
        care(date(28).toISOString(), { id: 'paused', isEnabled: false }), care('invalid', { id: 'bad' }), care(null, { id: 'none' })] }),
      plant({ id: 'b', nickname: '', speciesCommonName: 'Fig tree', plannedPlantingOutOn: '2026-03-28' }),
      plant({ id: 'old', isActive: false, expectedHarvestOn: '2026-03-28' })];
    const before = JSON.stringify(plants);
    const entries = gardenWorkEntries(plants, date(28), 7, true, '');
    expect(entries).toHaveLength(5);
    expect(entries[0].status).toBe('Overdue care');
    expect(entries.find(e => e.action === 'Expected harvest')?.date).toBe('2026-04-03');
    expect(entries.find(e => e.plantId === 'b')?.name).toBe('Fig tree');
    expect(entries.filter(e => e.kind === 'care')).toHaveLength(2);
    expect(entries.find(e => e.action === 'Planned sowing')?.instant).toBeNull();
    expect(JSON.stringify(plants)).toBe(before);
  });
  it('uses inclusive local calendar ranges across DST, keeps target dates literal and rejects impossible targets', () => {
    const lastDay = date(3); lastDay.setMonth(3);
    const nextDay = new Date(lastDay); nextDay.setDate(nextDay.getDate() + 1);
    const plants = [plant({ plannedSowingOn: '2026-02-30', expectedHarvestOn: '2026-04-03',
      careSchedules: [care(lastDay.toISOString()), care(nextDay.toISOString(), { id: 'beyond' })] })];
    const entries = gardenWorkEntries(plants, date(28), 7, false, '');
    expect(entries.map(e => e.date)).toEqual(['2026-04-03', '2026-04-03']);
    expect(entries.some(e => e.action === 'Planned sowing')).toBe(false);
    expect(gardenWorkEntries(plants, date(28), 8, false, '')).toEqual([]);
    expect(gardenWorkEntries(plants, new Date('invalid'), 7, false, '')).toEqual([]);
  });
  it('filters kinds and past items, labels past targets separately and renders literal dates with detail links', () => {
    const { fixture, page } = setup([plant({ plannedSowingOn: '2026-03-27', expectedHarvestOn: '2026-03-28',
      careSchedules: [care(date(27).toISOString())] })]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Saturday, March 28, 2026');
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('/my-plants/a');
    expect(page.entries().map(e => e.status)).toContain('Past target');
    page.setKind('target'); expect(page.entries()).toHaveLength(2);
    page.includePast.set(false); expect(page.entries()).toHaveLength(1);
    page.setKind('care'); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No work items match');
    page.setDays('90'); page.setDays('365'); expect(page.days()).toBe(90);
    page.setKind('invalid'); expect(page.kind()).toBe('care');
    fixture.componentRef.setInput('plants', []); expect(page.entries()).toEqual([]);
  });
  it('relabels dates at local midnight and disposes its clock when the view closes', () => {
    vi.useFakeTimers(); vi.setSystemTime(new Date(2026, 9, 24, 23, 59, 30));
    const { fixture, page } = setup([plant({ plannedSowingOn: '2026-10-24' })]);
    page.now.set(new Date()); expect(page.entries()[0].status).toBe('Target today');
    vi.advanceTimersByTime(60_000); expect(page.entries()[0].status).toBe('Past target');
    expect(localWorkDate(page.now())).toBe('2026-10-25');
    fixture.destroy(); const stopped = page.now().getTime();
    vi.advanceTimersByTime(120_000); expect(page.now().getTime()).toBe(stopped);
  });
});
