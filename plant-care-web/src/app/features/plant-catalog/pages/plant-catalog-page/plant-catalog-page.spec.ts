import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PlantCatalogPage } from './plant-catalog-page';
import { PlantSpeciesApiService } from '../../../../core/services/plant-species-api.service';
import { PlantCategory, PlantSpecies } from '../../../../models/plant-species.model';

describe('Catalog categories', () => {
  const params = new BehaviorSubject(convertToParamMap({}));
  const make = (commonName: string, categories?: PlantCategory[], scientificName = commonName): PlantSpecies => ({
    id: commonName, commonName, scientificName, description: 'Reference plant', sunlightRequirement: 'FullSun',
    sunlightInstructions: 'Sun', defaultWateringIntervalDays: 3, wateringInstructions: 'Check moisture',
    defaultFertilizingIntervalDays: null, fertilizingInstructions: null, soilInstructions: 'Drained soil',
    humidityInstructions: null, minimumTemperatureCelsius: null, maximumTemperatureCelsius: null,
    isToxicToPets: false, catalogProfile: { categories, photo: null, reviewedOn: '2026-10-25', careSourceUrl: 'https://example.org/care' },
  });
  const plants = [make('Fig tree', ['fruit', 'tree']), make('Potato', ['vegetable', 'root-tuber']),
    make('Tomato', ['vegetable'], 'Solanum lycopersicum'),
    make('Cherry tomato (Tiny Tim)', ['vegetable'], "Solanum lycopersicum 'Tiny Tim'"), make('Legacy')];
  plants[3].catalogProfile!.cultivarName = 'Tiny Tim';
  beforeEach(() => {
    params.next(convertToParamMap({}));
    TestBed.configureTestingModule({ providers: [provideRouter([]),
      { provide: ActivatedRoute, useValue: { queryParamMap: params } },
      { provide: PlantSpeciesApiService, useValue: { getAll: () => of(plants) } }],
    });
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });
  afterEach(() => TestBed.resetTestingModule());
  it('supports overlapping garden groups without guessing a legacy category', () => {
    const page = TestBed.createComponent(PlantCatalogPage).componentInstance;
    page.setCategory('tree'); expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Fig tree']);
    page.setCategory('fruit'); expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Fig tree']);
    page.setCategory('root-tuber'); expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Potato']);
    expect(page.plantSpecies()).toEqual(plants);
  });
  it('combines category and exact cultivar search, persists it and retains comparison when cleared', () => {
    const page = TestBed.createComponent(PlantCatalogPage).componentInstance;
    page.setCategory('vegetable'); page.setQuery("Solanum lycopersicum 'Tiny Tim'");
    expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Cherry tomato (Tiny Tim)']);
    page.toggleComparison(plants[0]);
    expect(page.filterQueryParams().category).toBe('vegetable');
    expect(TestBed.inject(Router).navigate).toHaveBeenLastCalledWith([], expect.objectContaining({ queryParams: expect.objectContaining({ category: 'vegetable' }) }));
    page.clearFilters(); expect(page.visibleSpecies()).toHaveLength(5);
    expect(page.comparisonIds()).toEqual(['Fig tree']); expect(page.hasFilters()).toBe(false);
  });
  it('restores valid URL categories and ignores malformed values', () => {
    const page = TestBed.createComponent(PlantCatalogPage).componentInstance;
    params.next(convertToParamMap({ category: 'vegetable', light: 'FullSun', pets: 'unknown' }));
    expect(page.visibleSpecies()).toHaveLength(3);
    params.next(convertToParamMap({ category: 'not-a-category' }));
    expect(page.category()).toBe(''); expect(page.visibleSpecies()).toHaveLength(5);
  });
  it('filters named cultivars across any category and clears kind with the other view controls', () => {
    const page = TestBed.createComponent(PlantCatalogPage).componentInstance;
    params.next(convertToParamMap({ kind: 'cultivar', category: 'vegetable' }));
    expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Cherry tomato (Tiny Tim)']);
    page.setKind('general'); expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Potato', 'Tomato']);
    page.clearFilters(); expect(page.kind()).toBe('');
    params.next(convertToParamMap({ kind: 'unrecognized' })); expect(page.kind()).toBe('');
  });
  it('distinguishes exact cultivar photos from pending and legacy species-reference photos', () => {
    const page = TestBed.createComponent(PlantCatalogPage).componentInstance;
    const photo = { path: '/images/plants/example.jpg', altText: 'Example', title: 'Example',
      credit: 'Photographer', sourceUrl: 'https://example.org/photo', licenseName: 'CC0 1.0',
      licenseUrl: 'https://creativecommons.org/publicdomain/zero/1.0/' };
    const exact = make('Roma', ['vegetable']);
    exact.catalogProfile = { ...exact.catalogProfile!, cultivarName: 'Roma', photo };
    const legacy = make('Legacy cultivar', ['vegetable']);
    legacy.catalogProfile = { ...legacy.catalogProfile!, cultivarName: 'Legacy', photo, photoIsSpeciesReference: true };
    const general = make('General with photo', ['vegetable']);
    general.catalogProfile = { ...general.catalogProfile!, photo };
    page.plantSpecies.set([...plants, exact, legacy, general]);
    page.setCultivarPhoto('available');
    expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Roma']);
    page.setCultivarPhoto('pending');
    expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Cherry tomato (Tiny Tim)', 'Legacy cultivar']);
    page.setKind('general'); expect(page.visibleSpecies()).toEqual([]);
    page.setKind('cultivar'); page.setCategory('fruit'); expect(page.visibleSpecies()).toEqual([]);
    page.setCategory('vegetable'); page.setQuery('Tiny Tim');
    expect(page.visibleSpecies().map(p => p.commonName)).toEqual(['Cherry tomato (Tiny Tim)']);
  });
  it('restores and saves the cultivar photo filter, ignoring unsupported URL and control values', () => {
    const page = TestBed.createComponent(PlantCatalogPage).componentInstance;
    params.next(convertToParamMap({ cultivarPhoto: 'pending', category: 'vegetable', q: 'Tiny Tim' }));
    expect(page.cultivarPhoto()).toBe('pending'); expect(page.visibleSpecies()).toEqual([plants[3]]);
    page.setCultivarPhoto('available');
    expect(TestBed.inject(Router).navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({ cultivarPhoto: 'available', category: 'vegetable', q: 'Tiny Tim' }),
    }));
    params.next(convertToParamMap({ cultivarPhoto: 'unsupported' }));
    expect(page.cultivarPhoto()).toBe(''); expect(page.hasFilters()).toBe(false);
    page.setCultivarPhoto('unsupported'); expect(page.cultivarPhoto()).toBe('');
    page.setCultivarPhoto('pending'); page.toggleComparison(plants[0]); page.clearFilters();
    expect(page.cultivarPhoto()).toBe(''); expect(page.hasFilters()).toBe(false);
    expect(page.comparisonIds()).toEqual(['Fig tree']);
    expect(page.filterQueryParams().cultivarPhoto).toBeNull();
  });
});
