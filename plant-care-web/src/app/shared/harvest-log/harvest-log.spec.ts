import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { HarvestLog } from './harvest-log';
describe('Harvest log',()=>{
 let http:HttpTestingController;
 beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});http=TestBed.inject(HttpTestingController);});
 afterEach(()=>{http.verify();TestBed.resetTestingModule();});
 const record={id:'record',harvestedOn:'2026-01-01',quantity:2,unit:'items',notes:'First crop',recordedAtUtc:'2026-01-01T12:00:00Z'};
 function setup(active=true){const fixture=TestBed.createComponent(HarvestLog);fixture.componentRef.setInput('plant',{id:'plant',isActive:active});fixture.detectChanges();return {fixture,page:fixture.componentInstance};}
 it('keeps failed drafts, blocks duplicate saves and records exact optional data',()=>{
  const {page}=setup();http.expectOne('/api/my-plants/plant/harvests').flush([]);
  page.form.patchValue({quantity:1.001,unit:'kg',notes:' My crop '});expect(page.form.valid).toBe(true);
  page.save();page.save();const failed=http.expectOne('/api/my-plants/plant/harvests');expect(failed.request.body).toEqual({harvestedOn:page.today,quantity:1.001,unit:'kg',notes:'My crop'});failed.flush({}, {status:503,statusText:'offline'});
  expect(page.form.controls.notes.value).toBe(' My crop ');expect(page.busy()).toBe(false);expect(page.error()).toContain('draft');
  page.save();http.expectOne('/api/my-plants/plant/harvests').flush({...record,harvestedOn:page.today});expect(page.records()).toHaveLength(1);expect(page.form.controls.quantity.value).toBeNull();expect(page.message()).toBe('Harvest recorded.');
 });
 it('validates whole items, weight precision and future dates without issuing writes',()=>{
  const {page}=setup();http.expectOne('/api/my-plants/plant/harvests').flush([]);
  page.form.patchValue({quantity:1.5,unit:'items'});expect(page.form.invalid).toBe(true);page.save();http.expectNone(r=>r.method==='POST');
  page.form.patchValue({quantity:1.0001,unit:'g'});expect(page.form.invalid).toBe(true);
  page.form.patchValue({quantity:null,date:'9999-12-31'});expect(page.form.invalid).toBe(true);
  page.form.patchValue({date:page.today});page.save();const request=http.expectOne('/api/my-plants/plant/harvests');expect(request.request.body.unit).toBeNull();request.flush(record);
 });
 it('requires explicit removal confirmation and retains records after a failed delete',()=>{
  const {page}=setup();http.expectOne('/api/my-plants/plant/harvests').flush([record]);page.remove('record');http.expectNone(r=>r.method==='DELETE');
  page.deleting.set('record');page.remove('record');http.expectOne('/api/my-plants/plant/harvests/record').flush({}, {status:503,statusText:'offline'});expect(page.records()).toHaveLength(1);
  page.remove('record');http.expectOne('/api/my-plants/plant/harvests/record').flush(null);http.expectOne('/api/my-plants/plant/harvests').flush([]);expect(page.records()).toEqual([]);
 });
 it('retries load errors and presents archived records read-only',()=>{
  const {fixture,page}=setup(false);http.expectOne('/api/my-plants/plant/harvests').flush({}, {status:503,statusText:'offline'});expect(page.ready()).toBe(false);
  page.save();http.expectNone(r=>r.method==='POST');page.load();http.expectOne('/api/my-plants/plant/harvests').flush([record]);fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('This plant is archived');expect(fixture.nativeElement.querySelector('form')).toBeNull();page.deleting.set('record');page.remove('record');http.expectNone(r=>r.method==='DELETE');
 });
});
