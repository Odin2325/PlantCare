import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { expect, it } from 'vitest';
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
