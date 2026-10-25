import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { GardenPage } from './garden-page';
import { MyPlantsApiService } from '../my-plants/data-access/my-plants-api.service';
import { CareSchedule, UserPlant } from '../my-plants/models/user-plant.model';

describe('Garden collection', () => {
  const make = (name: string, categories?: string[], location: string | null = null) => ({
    id: name, nickname: name, speciesCommonName: name, speciesScientificName: name, location,
    tags: [], isActive: true, careSchedules: [], speciesCatalogProfile: { categories },
  }) as unknown as UserPlant;
  function setup(plants: UserPlant[]) {
    const getAll = vi.fn().mockReturnValue(of(plants));
    TestBed.configureTestingModule({ providers: [provideRouter([]),
      { provide: MyPlantsApiService, useValue: { getAll } }] });
    return { page: TestBed.createComponent(GardenPage).componentInstance, getAll };
  }
  afterEach(() => TestBed.resetTestingModule());
  it('groups active crops by trimmed location and excludes houseplants, legacy and archived plants', () => {
    const archived = { ...make('Old crop', ['vegetable']), isActive: false };
    const { page } = setup([make('Tomato', ['vegetable'], ' Balcony '), make('Fig', ['fruit', 'tree']),
      make('Monstera', ['houseplant']), make('Legacy'), archived]);
    expect(page.gardenPlants()).toHaveLength(2);
    expect(page.locations().map(group => group.name)).toEqual(['Balcony', 'Location not set']);
    page.query.set('balcony'); expect(page.visiblePlants().map(p => p.nickname)).toEqual(['Tomato']);
    page.setCategory('tree'); expect(page.visiblePlants()).toEqual([]);
    page.clearFilters(); expect(page.visiblePlants()).toHaveLength(2);
    page.setCategory('houseplant'); expect(page.category()).toBe('');
  });
  it('finds tags and scientific names and chooses the earliest enabled valid care date without changing schedules', () => {
    const plant = make('Sunny', ['vegetable']); plant.tags = ['Kitchen']; plant.speciesScientificName = 'Solanum lycopersicum';
    plant.careSchedules = [
      { id: 'later', isEnabled: true, nextDueAtUtc: '2027-02-01T08:00:00Z' },
      { id: 'paused', isEnabled: false, nextDueAtUtc: '2027-01-01T08:00:00Z' },
      { id: 'invalid', isEnabled: true, nextDueAtUtc: 'invalid' },
      { id: 'first', isEnabled: true, nextDueAtUtc: '2027-01-10T08:00:00Z' },
    ] as CareSchedule[];
    const { page } = setup([plant]); const original = [...plant.careSchedules];
    page.query.set('KITCHEN'); expect(page.visiblePlants()).toEqual([plant]);
    page.query.set('lycopersicum'); expect(page.visiblePlants()).toEqual([plant]);
    expect(page.nextCare(plant)?.id).toBe('first'); expect(plant.careSchedules).toEqual(original);
    expect(page.nextCare(make('No schedule', ['tree']))).toBeUndefined();
  });
  it('shows load failure and can retry without writing collection data', () => {
    const { page, getAll } = setup([]);
    getAll.mockReturnValueOnce(throwError(() => new Error('offline'))).mockReturnValueOnce(of([make('Fig', ['tree'])]));
    page.load(); expect(page.error()).toBe(true); expect(page.loading()).toBe(false);
    page.load(); expect(page.error()).toBe(false); expect(page.gardenPlants()).toHaveLength(1);
    expect(getAll).toHaveBeenCalledTimes(3);
  });
});
