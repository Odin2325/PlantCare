import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, expect, it, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { PlantSpeciesApiService } from '../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../models/plant-species.model';
import { catalogVarietyGroup, PlantRelatives } from './plant-relatives';

const plant = (id: string, scientificName: string | null, cultivarName?: string) => ({ id, commonName: id,
  scientificName, catalogProfile: { cultivarName, photo: null } }) as PlantSpecies;
afterEach(() => TestBed.resetTestingModule());

it('reconciles reviewed historical names and apostrophes while keeping crop groups, unknown identities and hybrids distinct', () => {
  expect(catalogVarietyGroup(plant('a', "Pisum sativum 'Jaguar'", 'Jaguar'))).toBe(catalogVarietyGroup(plant('b', 'Lathyrus oleraceus')));
  expect(catalogVarietyGroup(plant('a', "Daucus carota 'Flyaway'", 'Flyaway'))).toBe(catalogVarietyGroup(plant('b', 'Daucus carota subsp. sativus')));
  expect(catalogVarietyGroup(plant('a', "Malus domestica 'Bramley’s Seedling'", "Bramley's Seedling"))).toBe('malus domestica');
  expect(catalogVarietyGroup(plant('a', "Brassica oleracea (Acephala Group) 'Redbor'", 'Redbor'))).toBe(catalogVarietyGroup(plant('b', 'Brassica oleracea Kale Group')));
  expect(catalogVarietyGroup(plant('a', 'Brassica oleracea Kale Group'))).not.toBe(catalogVarietyGroup(plant('b', 'Brassica oleracea Botrytis Group')));
  expect(catalogVarietyGroup(plant('a', 'Fragaria × ananassa'))).not.toBe(catalogVarietyGroup(plant('b', 'Fragaria vesca')));
  expect(catalogVarietyGroup(plant('a', "Ocimum basilicum var. purpurascens 'Dark Opal'", 'Dark Opal')))
    .toBe(catalogVarietyGroup(plant('b', "Ocimum basilicum 'Genovese'", 'Genovese')));
  expect(catalogVarietyGroup(plant('a', null))).toBeNull();
  expect(catalogVarietyGroup(plant('a', 'Unverified name', 'Claimed cultivar'))).toBeNull();
});

it('shows common-name links to other entries, updates on navigation and isolates retry failure from the main plant', () => {
  const current = plant('Roma', "Solanum lycopersicum 'Roma'", 'Roma');
  const catalog = [current, plant('Tomato', 'Solanum lycopersicum'), plant('Tiny Tim', "Solanum lycopersicum 'Tiny Tim'", 'Tiny Tim'), plant('Pepper', 'Capsicum annuum')];
  const before = JSON.stringify(catalog);
  const getAll = vi.fn().mockReturnValueOnce(throwError(() => new Error('offline'))).mockReturnValue(of(catalog));
  TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: PlantSpeciesApiService, useValue: { getAll } }] });
  const fixture = TestBed.createComponent(PlantRelatives); fixture.componentRef.setInput('plant', current); fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  fixture.componentInstance.load(); fixture.detectChanges();
  expect(fixture.componentInstance.relatives().map(p => p.id)).toEqual(['Tiny Tim', 'Tomato']);
  expect(fixture.nativeElement.querySelector('h3 a').getAttribute('href')).toBe('/plants/Tiny%20Tim');
  const compare = fixture.nativeElement.querySelector('a[aria-label="Compare Roma with Tiny Tim"]');
  expect(compare.textContent).toBe('Compare care');
  expect(new URL(compare.getAttribute('href'), 'https://example.org').searchParams.get('compare')).toBe('Roma,Tiny Tim');
  expect(fixture.nativeElement.textContent).toContain('Cultivar photo pending');
  fixture.componentRef.setInput('plant', catalog[3]); fixture.detectChanges();
  expect(fixture.componentInstance.relatives()).toEqual([]);
  expect(fixture.nativeElement.textContent).toContain('No other reviewed varieties');
  expect(getAll).toHaveBeenCalledTimes(2); expect(JSON.stringify(catalog)).toBe(before);
});
