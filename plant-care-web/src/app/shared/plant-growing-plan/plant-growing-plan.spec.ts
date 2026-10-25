import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { PlantGrowingPlan } from './plant-growing-plan';
import { UserPlant } from '../../features/my-plants/models/user-plant.model';

describe('Planting plan editor', () => {
  let http: HttpTestingController;
  const plant = { id: 'owned', isActive: true, plannedSowingOn: '2027-03-01', plannedPlantingOutOn: null, expectedHarvestOn: '2027-08-01' } as UserPlant;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });
  function editor(value = plant) {
    const fixture = TestBed.createComponent(PlantGrowingPlan); fixture.componentRef.setInput('plant', value); fixture.detectChanges(); return fixture;
  }
  it('validates partial dates and preserves entered values after a failed save for retry', () => {
    const fixture = editor(); const component = fixture.componentInstance;
    component.form.controls.harvest.setValue('2027-02-01'); component.save();
    expect(component.form.hasError('dateOrder')).toBe(true); http.expectNone('/api/my-plants/owned/growing-plan');
    component.form.controls.harvest.setValue('2027-08-02'); component.save(); expect(component.saving()).toBe(true);
    const request = http.expectOne('/api/my-plants/owned/growing-plan'); expect(request.request.body).toEqual({ plannedSowingOn: '2027-03-01', plannedPlantingOutOn: null, expectedHarvestOn: '2027-08-02' });
    request.flush({}, { status: 503, statusText: 'Unavailable' });
    expect(component.form.controls.harvest.value).toBe('2027-08-02'); expect(component.error()).toContain('could not be saved');
    component.save(); http.expectOne('/api/my-plants/owned/growing-plan').flush({ ...plant, expectedHarvestOn: '2027-08-02' });
    expect(component.message()).toBe('Planting plan saved.'); expect(component.saving()).toBe(false);
    component.form.controls.harvest.setValue('2027-08-03'); expect(component.message()).toBe('');
  });
  it('only removes saved dates after an explicit save and prevents duplicate requests', () => {
    const fixture = editor(); const component = fixture.componentInstance;
    component.clearDates(); http.expectNone('/api/my-plants/owned/growing-plan');
    component.save(); component.save(); const request = http.expectOne('/api/my-plants/owned/growing-plan');
    expect(request.request.body).toEqual({ plannedSowingOn: null, plannedPlantingOutOn: null, expectedHarvestOn: null });
    request.flush({ ...plant, plannedSowingOn: null, expectedHarvestOn: null });
  });
  it('shows archived dates without editing or sending a request', () => {
    const fixture = editor({ ...plant, isActive: false }); fixture.componentInstance.save();
    expect(fixture.nativeElement.textContent).toContain('Aug 1, 2027'); expect(fixture.nativeElement.querySelector('form')).toBeNull();
    http.expectNone('/api/my-plants/owned/growing-plan');
  });
});
