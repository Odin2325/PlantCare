import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { HarvestOverview, sumHarvestQuantities } from './harvest-overview';
import { HarvestOverview as Overview } from '../my-plants/models/harvest-overview.model';
import { MyPlantsApiService } from '../my-plants/data-access/my-plants-api.service';
import { UserPlant } from '../my-plants/models/user-plant.model';

describe('Garden harvest overview', () => {
  const result: Overview = { from: '2026-01-01', to: '2026-10-26', rows: [
    { id: 'r', userPlantId: 'a', harvestedOn: '2026-10-01', quantity: 1.001, unit: 'kg', notes: null }],
    plants: [ { userPlantId: 'a', recordCount: 102, unquantifiedCount: 1, quantities: { kg: '1.001', g: '3.000' } },
      { userPlantId: 'b', recordCount: 1, unquantifiedCount: 0, quantities: { kg: '2.002' } } ] };
  function setup() {
    const getHarvestOverview = vi.fn().mockReturnValue(of(result));
    TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: MyPlantsApiService, useValue: { getHarvestOverview } }] });
    const fixture = TestBed.createComponent(HarvestOverview);
    fixture.componentRef.setInput('plants', [{ id: 'a', nickname: 'My crop' }, { id: 'b', speciesCommonName: 'Fig' }] as unknown as UserPlant[]);
    return { fixture, page: fixture.componentInstance, getHarvestOverview };
  }
  afterEach(() => { TestBed.resetTestingModule(); vi.useRealTimers(); });
  it('totals every matching record by unit, even outside the recent row limit, and follows plant filters', () => {
    const { page, fixture } = setup();
    expect(page.count()).toBe(103); expect(page.unquantified()).toBe(1);
    expect(page.quantities()).toEqual([{ unit: 'g', quantity: '3' }, { unit: 'kg', quantity: '3.003' }]);
    expect(page.name('a')).toBe('My crop'); expect(page.name('b')).toBe('Fig');
    fixture.componentRef.setInput('plants', [{ id: 'b', speciesCommonName: 'Fig' }] as unknown as UserPlant[]);
    expect(page.count()).toBe(1); expect(page.rows()).toEqual([]);
    expect(page.quantities()).toEqual([{ unit: 'kg', quantity: '2.002' }]);
    expect(sumHarvestQuantities(['9007199254740.001', '0.002'])).toBe('9007199254740.003');
  });
  it('rejects invalid, reversed and excessive date ranges before requesting data', () => {
    const { page, getHarvestOverview } = setup();
    for (const [from, to] of [['2026-02-30', '2026-03-01'], ['2026-02-02', '2026-02-01'], ['2025-01-01', '2026-01-02']]) {
      page.from.set(from); page.to.set(to); page.load(); expect(page.error()).toContain('valid date range'); expect(page.data()).toBeNull();
    }
    expect(getHarvestOverview).toHaveBeenCalledTimes(1);
    page.from.set('2024-01-01'); page.to.set('2024-12-31'); page.load(); expect(getHarvestOverview).toHaveBeenCalledTimes(2);
    expect(page.date('2026-03-01')).toBe('Mar 1, 2026');
  });
  it('applies quick local date ranges across month boundaries and leap years', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2025, 2, 15, 0, 30));
    const { fixture, getHarvestOverview } = setup();
    fixture.detectChanges();
    const shortcuts = fixture.nativeElement.querySelector('[aria-label="Quick harvest date ranges"]') as HTMLElement;
    const click = (label: string) => Array.from(shortcuts.querySelectorAll('button'))
      .find(button => button.textContent?.trim() === label)!.click();

    click('Last 30 days');
    expect(getHarvestOverview).toHaveBeenLastCalledWith('2025-02-14', '2025-03-15');
    click('Last year');
    expect(getHarvestOverview).toHaveBeenLastCalledWith('2024-01-01', '2024-12-31');
    click('This year');
    expect(getHarvestOverview).toHaveBeenLastCalledWith('2025-01-01', '2025-03-15');
  });

  it('pages recent records without requesting data again or changing the totals', () => {
    const { fixture, page, getHarvestOverview } = setup();
    page.data.set({ ...result, rows: Array.from({ length: 10 }, (_, index) => ({
      ...result.rows[0], id: String(index), userPlantId: index === 9 ? 'b' : 'a', notes: 'Harvest ' + (index + 1),
    })) });
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const pager = element.querySelector('nav[aria-label="Recent harvest pages"]')!;
    const [previous, next] = pager.querySelectorAll('button');
    const notes = () => Array.from(element.querySelectorAll('.records p')).map(note => note.textContent);

    expect(previous.disabled).toBe(true);
    expect(notes()).toHaveLength(8);
    next.click();
    fixture.detectChanges();
    expect(notes()).toEqual(['Harvest 9', 'Harvest 10']);
    expect(next.disabled).toBe(true);
    expect(page.count()).toBe(103);
    expect(getHarvestOverview).toHaveBeenCalledTimes(1);

    fixture.componentRef.setInput('plants', [{ id: 'b', speciesCommonName: 'Fig' }]);
    fixture.detectChanges();
    expect(element.querySelector('nav[aria-label="Recent harvest pages"]')).toBeNull();
    expect(notes()).toEqual(['Harvest 10']);
    expect(page.count()).toBe(1);
  });

  it('retries failed reads and cancels older responses when applying dates', () => {
    const { page, getHarvestOverview } = setup();
    getHarvestOverview.mockReturnValueOnce(throwError(() => new Error('offline')));
    page.load(); expect(page.error()).toContain('could not be loaded'); expect(page.data()).toBeNull();
    const pending = new Subject<Overview>(); getHarvestOverview.mockReturnValueOnce(pending);
    page.load(); expect(page.loading()).toBe(true); page.thisYear();
    pending.next({ ...result, plants: [] }); expect(page.data()).toEqual(result); expect(page.error()).toBe('');
  });
  it('groups full monthly summaries, includes empty months and applies plant filters without rounding', () => {
    const { page, fixture } = setup();
    page.data.set({ ...result, from: '2025-12-31', to: '2026-03-01', months: [
      { userPlantId: 'a', month: '2025-12', recordCount: 101, unquantifiedCount: 1, quantities: { kg: '1.001' } },
      { userPlantId: 'b', month: '2025-12', recordCount: 1, unquantifiedCount: 0, quantities: { kg: '2.002', g: '3.000' } },
      { userPlantId: 'a', month: '2026-03', recordCount: 1, unquantifiedCount: 0, quantities: { items: '2.000' } },
    ] });
    expect(page.months().map(m => [m.month, m.count])).toEqual([['2025-12', 102], ['2026-01', 0], ['2026-02', 0], ['2026-03', 1]]);
    expect(page.months()[0].quantities).toBe('3 g · 3.003 kg');
    expect(page.months()[0].title).toBe('December 2025');
    expect(page.plantTrends().map(p => page.name(p.userPlantId))).toEqual(['Fig', 'My crop']);
    fixture.componentRef.setInput('plants', [{ id: 'a', nickname: 'My crop' }] as unknown as UserPlant[]);
    expect(page.months()[0].count).toBe(101); expect(page.months()[0].quantities).toBe('1.001 kg');
    page.data.set({ ...result, from: '9999-12-01', to: '9999-12-31', months: [] });
    expect(page.months()).toHaveLength(1); expect(page.months()[0].month).toBe('9999-12');
    page.data.set(result); expect(page.months()).toEqual([]);
  });

});

