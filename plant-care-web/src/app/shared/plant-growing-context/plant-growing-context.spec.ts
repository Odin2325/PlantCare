import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PlantGrowingContext, seasonalCareGuidance } from './plant-growing-context';
import { MyPlantsApiService } from '../../features/my-plants/data-access/my-plants-api.service';
import { UserPlant } from '../../features/my-plants/models/user-plant.model';

describe('Seasonal care preferences', () => {
  const plant={id:'a',isActive:true,growingEnvironment:'indoor',careSeason:'spring',careSchedules:[{id:'care',nextDueAtUtc:'2027-01-01T08:00:00Z'}]} as unknown as UserPlant;
  async function setup(value=plant) {
    const updateGrowingContext=vi.fn().mockReturnValue(of({...value,growingEnvironment:'balcony',careSeason:'winter'}));
    TestBed.configureTestingModule({providers:[provideRouter([]),{provide:MyPlantsApiService,useValue:{updateGrowingContext}}]});
    const fixture=TestBed.createComponent(PlantGrowingContext);fixture.componentRef.setInput('plant',value);await fixture.whenStable();
    return {fixture,page:fixture.componentInstance,updateGrowingContext};
  }
  afterEach(()=>TestBed.resetTestingModule());
  it('loads saved choices and saves only preferences without editing schedules',async()=>{
    const {page,updateGrowingContext}=await setup();const saved=vi.fn();page.saved.subscribe(saved);
    expect(page.form.getRawValue()).toEqual({environment:'indoor',season:'spring'});
    page.form.setValue({environment:'balcony',season:'winter'});page.save();
    expect(updateGrowingContext).toHaveBeenCalledWith('a',{growingEnvironment:'balcony',careSeason:'winter'});
    expect(saved).toHaveBeenCalledWith(expect.objectContaining({careSchedules:plant.careSchedules}));expect(page.message()).toBe('Seasonal care preferences saved.');
    page.clearPreferences();page.save();expect(updateGrowingContext).toHaveBeenLastCalledWith('a',{growingEnvironment:null,careSeason:null});
  });
  it('retains failed drafts for retry and resets when another plant is loaded',async()=>{
    const {fixture,page,updateGrowingContext}=await setup();updateGrowingContext.mockReturnValueOnce(throwError(()=>({status:503})));
    page.form.setValue({environment:'greenhouse',season:'autumn'});page.save();expect(page.error()).toContain('choices are still here');
    expect(page.form.getRawValue()).toEqual({environment:'greenhouse',season:'autumn'});expect(page.form.enabled).toBe(true);
    page.save();expect(updateGrowingContext).toHaveBeenCalledTimes(2);
    fixture.componentRef.setInput('plant',{...plant,id:'b',careSeason:null});await fixture.whenStable();expect(page.form.controls.season.value).toBe('');expect(page.error()).toBe('');
  });
  it('shows archived context without editable controls or writes',async()=>{
    const {fixture,page,updateGrowingContext}=await setup({...plant,isActive:false});page.save();fixture.detectChanges();
    expect(updateGrowingContext).not.toHaveBeenCalled();expect(fixture.nativeElement.querySelector('form')).toBeNull();expect(fixture.nativeElement.textContent).toContain('Archived preferences: indoor');
  });
  it('provides review prompts for every supported choice and none for unset values',()=>{
    for(const environment of ['indoor','balcony','greenhouse','outdoor']) for(const season of ['spring','summer','autumn','winter'])
      expect(seasonalCareGuidance(environment,season)).toHaveLength(2);
    expect(seasonalCareGuidance('','')).toEqual([]);expect(seasonalCareGuidance('invalid','invalid')).toEqual([]);
  });
});
