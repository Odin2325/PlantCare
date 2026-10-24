import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { expect, it } from 'vitest';
import { PlantComparisonPage } from './plant-comparison-page';
const a='10000000-0000-0000-0000-000000000000',b='20000000-0000-0000-0000-000000000000';
function setup(compare: string) {
  const params=new BehaviorSubject(convertToParamMap({compare}));
  TestBed.configureTestingModule({providers:[provideRouter([]),provideHttpClient(),provideHttpClientTesting(),
    {provide:ActivatedRoute,useValue:{queryParamMap:params}}]});
  const fixture=TestBed.createComponent(PlantComparisonPage);return {fixture,http:TestBed.inject(HttpTestingController),params};
}
it('preserves selection order and distinguishes unknown pet assessment from a sourced non-toxic classification', () => {
  const {fixture,http}=setup(`${b},${a}`);
  http.expectOne('/api/plant-species').flush([
    {id:a,commonName:'Unknown plant',sunlightRequirement:'Unknown',defaultWateringIntervalDays:7,isToxicToPets:false},
    {id:b,commonName:'Reviewed plant',sunlightRequirement:'BrightIndirectLight',defaultWateringIntervalDays:5,isToxicToPets:false,
      catalogProfile:{careSourceUrl:'https://example.org/care',reviewedOn:'2026-10-24',nonToxicToCatsAndDogsSourceUrl:'https://example.org/pets',photo:null}},
  ]);
  fixture.detectChanges();expect(fixture.componentInstance.plants().map(p=>p.id)).toEqual([b,a]);
  expect(fixture.nativeElement.textContent).toContain('Not assessed for this comparison');
  expect(fixture.nativeElement.textContent).toContain('Reviewed as non-toxic to cats and dogs');
  expect(fixture.nativeElement.textContent).toContain('Not specified');http.verify();
});
it('shows unavailable selections and a recoverable API failure without silently displaying an empty success', () => {
  const {fixture,http}=setup(a);http.expectOne('/api/plant-species').flush(null,{status:503,statusText:'Unavailable'});
  fixture.detectChanges();expect(fixture.nativeElement.textContent).toContain('could not be loaded');
  fixture.componentInstance.reload();http.expectOne('/api/plant-species').flush([]);fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('no longer available');
  expect(fixture.nativeElement.textContent).toContain('Choose at least two plants');http.verify();
});
