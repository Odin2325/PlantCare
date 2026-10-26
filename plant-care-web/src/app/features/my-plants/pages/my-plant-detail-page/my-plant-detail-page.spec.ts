import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { MyPlantDetailPage } from './my-plant-detail-page';

describe('MyPlantDetailPage', () => {
  const id = 'a1111111-1111-4111-8111-111111111111';
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id }) } } }],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  for (const nickname of ['Kitchen fern', '']) {
    it(`shows ${nickname ? 'the individual nickname' : 'the species fallback'} and links to its own history`, () => {
      const fixture = TestBed.createComponent(MyPlantDetailPage);
      http.expectOne(`/api/my-plants/${id}`).flush({ id, nickname, speciesCommonName: 'Boston fern',
        plantSpeciesId: 'b1111111-1111-4111-8111-111111111111', tags: [], careSchedules: [], isActive: true });
      fixture.detectChanges();
      http.expectOne(`/api/my-plants/${id}/harvests`).flush([]);
      expect(fixture.nativeElement.querySelector('h1').textContent).toBe(nickname || 'Boston fern');
      expect(fixture.nativeElement.querySelector(`a[href="/my-plants/${id}/care-history"]`)).not.toBeNull();
    });
  }
  it('does not expose plant data when the owner-scoped API rejects the identifier', () => {
    const fixture = TestBed.createComponent(MyPlantDetailPage);
    http.expectOne(`/api/my-plants/${id}`).flush({}, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toBe('This plant could not be found.');
    expect(fixture.nativeElement.querySelector('.schedule')).toBeNull();
    expect(fixture.nativeElement.querySelector('h1').textContent).toBe('Plant details');
  });
});
