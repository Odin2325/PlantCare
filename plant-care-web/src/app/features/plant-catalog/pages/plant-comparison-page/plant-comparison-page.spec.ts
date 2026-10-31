import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { afterEach, expect, it, vi } from 'vitest';
import { PlantComparisonPage } from './plant-comparison-page';
const a='10000000-0000-0000-0000-000000000000',b='20000000-0000-0000-0000-000000000000';
afterEach(() => vi.unstubAllGlobals());
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
it('keeps each plant growing and harvest guidance with its own source and omits missing guidance', () => {
  const {fixture,http}=setup(`${a},${b}`);
  http.expectOne('/api/plant-species').flush([
    {id:a,commonName:'Climbing bean',sunlightRequirement:'FullSun',isToxicToPets:true,
      catalogProfile:{careSourceUrl:'https://example.org/bean',reviewedOn:'2026-10-31',photo:null,
        growingNotes:'Provide a climbing frame.',harvestInstructions:'Pick young pods and cook properly.'}},
    {id:b,commonName:'Houseplant',sunlightRequirement:'BrightIndirectLight',isToxicToPets:false,
      catalogProfile:{careSourceUrl:'https://example.org/houseplant',reviewedOn:'2026-10-31',photo:null,
        growingNotes:'Use a drained indoor pot.'}},
  ]);
  fixture.detectChanges();
  const cards=fixture.nativeElement.querySelectorAll('.comparison-card');
  expect(cards[0].textContent).toContain('Provide a climbing frame.');
  expect(cards[0].textContent).toContain('Pick young pods and cook properly.');
  expect(cards[0].querySelector('.source a').getAttribute('href')).toBe('https://example.org/bean');
  expect(cards[1].textContent).toContain('Use a drained indoor pot.');
  expect(cards[1].textContent).not.toContain('Harvest guidance');
  expect(cards[1].textContent).not.toContain('Pick young pods');
  expect(cards[1].querySelector('.source a').getAttribute('href')).toBe('https://example.org/houseplant');
  http.verify();
});
it('copies only available public catalog identifiers in selection order and clears status when selection changes', async () => {
  const {fixture,http,params}=setup(`${b},${a}`);
  params.next(convertToParamMap({compare:`${b},${a}`,q:'private search',category:'fruit',token:'not-for-sharing'}));
  http.expectOne('/api/plant-species').flush([
    {id:a,commonName:'A',sunlightRequirement:'Unknown',defaultWateringIntervalDays:7,isToxicToPets:false},
    {id:b,commonName:'B',sunlightRequirement:'Unknown',defaultWateringIntervalDays:7,isToxicToPets:false},
  ]);
  fixture.detectChanges();
  const link=new URL(fixture.componentInstance.shareUrl());
  expect(link.pathname).toBe('/plants/compare');
  expect([...link.searchParams.entries()]).toEqual([['compare',`${b},${a}`]]);
  const writeText=vi.fn().mockResolvedValue(undefined);vi.stubGlobal('navigator',{clipboard:{writeText}});
  await fixture.componentInstance.copyComparisonLink();fixture.detectChanges();
  expect(writeText).toHaveBeenCalledWith(link.href);
  expect(fixture.nativeElement.textContent).toContain('Comparison link copied.');
  params.next(convertToParamMap({compare:a}));fixture.detectChanges();
  expect(fixture.componentInstance.shareUrl()).toBe('');
  expect(fixture.nativeElement.querySelector('#comparison-share-link')).toBeNull();
  expect(fixture.componentInstance.copyStatus()).toBe('');http.verify();
});
it('offers manual copying when clipboard permission fails and never copies stale unavailable selections', async () => {
  const missing='30000000-0000-0000-0000-000000000000';
  const {fixture,http,params}=setup(`${a},${missing},${b}`);
  http.expectOne('/api/plant-species').flush([
    {id:a,commonName:'A',sunlightRequirement:'Unknown',defaultWateringIntervalDays:7,isToxicToPets:false},
    {id:b,commonName:'B',sunlightRequirement:'Unknown',defaultWateringIntervalDays:7,isToxicToPets:false},
  ]);
  fixture.detectChanges();
  expect(new URL(fixture.componentInstance.shareUrl()).searchParams.get('compare')).toBe(`${a},${b}`);
  let rejectCopy!: (reason: Error) => void;
  const writeText=vi.fn().mockReturnValue(new Promise<void>((_,reject)=>rejectCopy=reject));
  vi.stubGlobal('navigator',{clipboard:{writeText}});
  const pending=fixture.componentInstance.copyComparisonLink();
  await fixture.componentInstance.copyComparisonLink();expect(writeText).toHaveBeenCalledTimes(1);
  rejectCopy(new Error('Permission denied'));await pending;fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Copy the link from the Comparison link field.');
  expect(fixture.nativeElement.querySelector('#comparison-share-link').readOnly).toBe(true);
  params.next(convertToParamMap({compare:a}));
  await fixture.componentInstance.copyComparisonLink();expect(writeText).toHaveBeenCalledTimes(1);http.verify();
});
