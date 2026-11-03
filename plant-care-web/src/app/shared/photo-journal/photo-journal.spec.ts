import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { PhotoJournal, JournalEntry } from './photo-journal';
import { AuthService } from '../../core/auth/services/auth.service';
import { UserPlant } from '../../features/my-plants/models/user-plant.model';
import { PhotoDraftState } from '../../core/services/photo-draft-state';

const user=signal<{id:string;email:string}|null>(null);
const plant={id:'10000000-0000-4000-8000-000000000001',nickname:'My tomato',isActive:true} as UserPlant;
const base='/api/my-plants/'+plant.id+'/photos';
const item:JournalEntry={id:'20000000-0000-4000-8000-000000000001',userPlantId:plant.id,photographedOn:'2026-01-01',notes:'Private note',createdAtUtc:'2026-01-01T12:00:00Z',version:'v1'};
let fixture:ComponentFixture<PhotoJournal>,http:HttpTestingController;
let createDescriptor:PropertyDescriptor|undefined,revokeDescriptor:PropertyDescriptor|undefined;
beforeEach(()=>{
  user.set({id:'a',email:'a@example.test'});
  createDescriptor=Object.getOwnPropertyDescriptor(URL,'createObjectURL');revokeDescriptor=Object.getOwnPropertyDescriptor(URL,'revokeObjectURL');
  let id=0;Object.defineProperty(URL,'createObjectURL',{configurable:true,value:vi.fn(()=>'blob:photo-'+ ++id)});
  Object.defineProperty(URL,'revokeObjectURL',{configurable:true,value:vi.fn()});
  vi.spyOn(navigator,'onLine','get').mockReturnValue(true);
  TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting(),{provide:AuthService,useValue:{currentUser:user}}]});
  fixture=TestBed.createComponent(PhotoJournal);fixture.componentRef.setInput('plant',plant);http=TestBed.inject(HttpTestingController);fixture.detectChanges();
});
afterEach(()=>{
  http.verify({ignoreCancelled:true});fixture.destroy();TestBed.resetTestingModule();vi.restoreAllMocks();
  if(createDescriptor)Object.defineProperty(URL,'createObjectURL',createDescriptor);else delete (URL as unknown as Record<string,unknown>)['createObjectURL'];
  if(revokeDescriptor)Object.defineProperty(URL,'revokeObjectURL',revokeDescriptor);else delete (URL as unknown as Record<string,unknown>)['revokeObjectURL'];
});
function loaded(items:JournalEntry[]=[],enabled=true){
  http.expectOne(base+'/capabilities').flush({uploadsEnabled:enabled,canUpload:enabled,maximumFileBytes:8388608});
  http.expectOne(base+'?oldestFirst=false').flush(items);fixture.detectChanges();
}
function choose(){fixture.componentInstance.select({target:{files:[new File(['synthetic'],'private-camera-name.png',{type:'image/png'})],value:'selected'}} as unknown as Event);fixture.detectChanges();}
it('hides old account data immediately, cancels responses and releases private object URLs',()=>{
  loaded([item]);http.expectOne(base+'/'+item.id+'/thumbnail').flush(new Blob(['jpeg'],{type:'image/jpeg'}));choose();
  const component=fixture.componentInstance;component.view(item);const image=http.expectOne(base+'/'+item.id+'/image');
  expect(component.entries()).toHaveLength(1);user.set({id:'b',email:'b@example.test'});
  expect(component.entries()).toEqual([]);expect(component.preview()).toBe('');TestBed.tick();
  expect(image.cancelled).toBe(true);expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:photo-1');expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:photo-2');
  loaded();expect(component.hasDraft()).toBe(false);
});
it('keeps an uncertain upload locked and retries the identical request only after reconciliation',()=>{
  loaded();choose();const component=fixture.componentInstance;component.form.controls.notes.setValue('A note');component.save();component.save();
  const first=http.expectOne(base);expect(first.request.method).toBe('POST');const body=first.request.body as FormData;
  const id=body.get('requestId');expect((body.get('file') as File).name).toBe('photo');
  expect(component.entries()).toEqual([]);first.flush({}, {status:503,statusText:'Unavailable'});
  expect(component.uncertain()).toBe(true);expect(component.form.disabled).toBe(true);component.save();http.expectNone(base);
  component.reconcile();http.expectOne(base+'/requests/'+id).flush({}, {status:404,statusText:'Not found'});
  expect(component.retryAllowed()).toBe(true);component.save();const retry=http.expectOne(base);
  expect((retry.request.body as FormData).get('requestId')).toBe(id);expect((retry.request.body as FormData).get('notes')).toBe('A note');
  retry.flush(item);loaded([item]);http.expectOne(base+'/'+item.id+'/thumbnail').flush(new Blob(['jpeg'],{type:'image/jpeg'}));
  expect(component.hasDraft()).toBe(false);expect(component.entries()).toHaveLength(1);expect(URL.revokeObjectURL).toHaveBeenCalled();
});
it('preserves the draft across same-plant updates and prevents navigation without confirmation',()=>{
  loaded();choose();fixture.componentRef.setInput('plant',{...plant,location:'New room'});fixture.detectChanges();http.expectNone(base+'/capabilities');
  const drafts=TestBed.inject(PhotoDraftState);expect(drafts.pending()).toBe(true);
  vi.spyOn(window,'confirm').mockReturnValue(false);expect(drafts.confirmLeave()).toBe(false);
  expect(fixture.componentInstance.preview()).not.toBe('');
});
it('explains a rejected image and prevents repeatedly submitting it unchanged',()=>{
  loaded();choose();const component=fixture.componentInstance;component.save();
  const request=http.expectOne(base),id=(request.request.body as FormData).get('requestId');
  request.flush({}, {status:422,statusText:'Rejected'});expect(component.error()).toContain('smaller supported photo');
  component.reconcile();http.expectOne(base+'/requests/'+id).flush({}, {status:404,statusText:'Not found'});
  expect(component.retryAllowed()).toBe(false);expect(component.message()).toContain('Discard this draft');
});
it('does not upload when disabled or offline and rejects unsupported source files',()=>{
  loaded([],false);const component=fixture.componentInstance;choose();component.save();http.expectNone(base);
  expect(component.hasFile()).toBe(false);component.capabilities.set({uploadsEnabled:true,canUpload:true,maximumFileBytes:8388608});
  component.select({target:{files:[new File(['svg'],'image.svg',{type:'image/svg+xml'})],value:''}} as unknown as Event);
  expect(component.error()).toContain('JPEG');component.disconnected();component.save();http.expectNone(base);
});
it('requires refresh after uncertain deletion and keeps failed edits as a draft',()=>{
  loaded([item]);http.expectOne(base+'/'+item.id+'/thumbnail').flush(new Blob(['jpeg'],{type:'image/jpeg'}));const component=fixture.componentInstance;
  component.remove(item.id);http.expectOne(base+'/'+item.id).flush({}, {status:503,statusText:'Unavailable'});
  expect(component.needsRefresh()).toBe(true);expect(component.entries()).toHaveLength(1);component.remove(item.id);http.expectNone(base+'/'+item.id);
  component.load();loaded([item]);http.expectOne(base+'/'+item.id+'/thumbnail').flush(new Blob(['jpeg'],{type:'image/jpeg'}));
  component.beginEdit(item);component.editForm.controls.notes.setValue('Unsaved correction');component.saveEdit();
  http.expectOne(base+'/'+item.id).flush({}, {status:409,statusText:'Conflict'});
  expect(component.editConflict()).toBe(true);expect(component.editForm.controls.notes.value).toBe('Unsaved correction');
});
