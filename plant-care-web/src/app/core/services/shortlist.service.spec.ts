import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, expect, it } from 'vitest';
import { AuthService } from '../auth/services/auth.service';
import { CurrentUser } from '../auth/models/auth.models';
import { ShortlistService } from './shortlist.service';

const user = signal<CurrentUser | null>(null);
let service: ShortlistService, http: HttpTestingController;
beforeEach(() => {
  user.set(null);
  TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting(),
    {provide:AuthService,useValue:{currentUser:user}}]});
  service=TestBed.inject(ShortlistService);http=TestBed.inject(HttpTestingController);TestBed.tick();
});
afterEach(() => {http.verify({ignoreCancelled:true});TestBed.resetTestingModule();});
function login(id='a', ids: string[] = []) {
  user.set({id,email:id+'@example.test'});TestBed.tick();
  http.expectOne('/api/shortlist').flush(ids.map(plantSpeciesId=>({plantSpeciesId,savedAtUtc:'2026-10-31T12:00:00Z'})));
}
it('does not load anonymously and discards private state on logout and account changes', () => {
  http.expectNone('/api/shortlist');login('a',['plant-a']);expect(service.has('plant-a')).toBe(true);
  user.set({id:'b',email:'b@example.test'});
  expect(service.ids()).toEqual([]);expect(service.ready()).toBe(false);
  TestBed.tick();http.expectOne('/api/shortlist').flush([{plantSpeciesId:'plant-b',savedAtUtc:'2026-10-31T12:00:00Z'}]);
  expect(service.ids()).toEqual(['plant-b']);
  user.set(null);expect(service.ids()).toEqual([]);expect(service.ready()).toBe(false);TestBed.tick();
});
it('waits for confirmation, suppresses duplicate writes and refreshes after ambiguous failures', () => {
  login();service.toggle('plant');service.toggle('plant');
  const save=http.expectOne('/api/shortlist/plant');expect(save.request.method).toBe('PUT');
  expect(save.request.body).toEqual({});expect(service.ids()).toEqual([]);
  service.refresh();http.expectNone('/api/shortlist');
  save.flush(null,{status:204,statusText:'No Content'});expect(service.ids()).toEqual(['plant']);
  service.toggle('plant');const remove=http.expectOne('/api/shortlist/plant');expect(remove.request.method).toBe('DELETE');
  remove.flush(null,{status:503,statusText:'Unavailable'});
  expect(service.ids()).toEqual(['plant']);expect(service.ready()).toBe(false);expect(service.error()).toContain('could not be confirmed');
  service.toggle('plant');http.expectNone('/api/shortlist/plant');
  service.refresh();http.expectOne('/api/shortlist').flush([]);expect(service.ids()).toEqual([]);expect(service.ready()).toBe(true);
});
it('cancels an old account mutation and never merges its shortlist into a new account', () => {
  login('a');service.toggle('private-a');const pending=http.expectOne('/api/shortlist/private-a');
  user.set({id:'b',email:'b@example.test'});TestBed.tick();
  expect(pending.cancelled).toBe(true);expect(service.ids()).toEqual([]);expect(service.busy()).toBeNull();
  http.expectOne('/api/shortlist').flush([{plantSpeciesId:'private-b',savedAtUtc:'2026-10-31T12:00:00Z'}]);
  expect(service.ids()).toEqual(['private-b']);
});
it('keeps controls unavailable after a failed load until a successful refresh', () => {
  user.set({id:'a',email:'a@example.test'});TestBed.tick();
  http.expectOne('/api/shortlist').flush(null,{status:503,statusText:'Unavailable'});
  expect(service.loading()).toBe(false);expect(service.ready()).toBe(false);expect(service.error()).toContain('could not be loaded');
  service.toggle('plant');http.expectNone('/api/shortlist/plant');service.refresh();http.expectOne('/api/shortlist').flush([]);
  expect(service.ready()).toBe(true);expect(service.error()).toBeNull();
});
