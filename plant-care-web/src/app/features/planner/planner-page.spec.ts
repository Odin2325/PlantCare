import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, describe, expect, it } from 'vitest';

import { AuthService } from '../../core/auth/services/auth.service';
import { plannerStorageKey } from '../../core/services/planner-storage-key';
import { PlantSpeciesApiService } from '../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../models/plant-species.model';
import { PlannerPage } from './planner-page';

const basil: PlantSpecies = {
  id: 'basil', commonName: 'Basil', scientificName: 'Ocimum basilicum', description: '',
  sunlightRequirement: 'FullSun', sunlightInstructions: 'Direct sun.',
  defaultWateringIntervalDays: 3, wateringInstructions: 'Keep the mix evenly moist.',
  defaultFertilizingIntervalDays: null, fertilizingInstructions: null,
  soilInstructions: 'Well-drained mix.', humidityInstructions: null,
  minimumTemperatureCelsius: 12, maximumTemperatureCelsius: null, isToxicToPets: false,
};
const fern: PlantSpecies = { ...basil, id: 'fern', commonName: 'Fern', sunlightRequirement: 'LowLight' };

function setup(owner = 'planner-test-owner') {
  TestBed.configureTestingModule({ providers: [
    provideRouter([]),
    { provide: AuthService, useValue: { currentUser: () => ({ id: owner }) } },
    { provide: PlantSpeciesApiService, useValue: { getAll: () => of([basil, fern]) } },
  ] });
  return TestBed.createComponent(PlannerPage);
}

afterEach(() => {
  TestBed.resetTestingModule();
  localStorage.removeItem(plannerStorageKey('planner-test-owner'));
  localStorage.removeItem(plannerStorageKey('planner-test-other'));
});

describe('Space planner', () => {
  it('suggests a whole-pot position inside a light zone and clear of an obstacle', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addShape('rectangle');
    page.setRole('light');
    page.setLight('FullSun');
    page.addShape('square');
    page.setRole('obstacle');
    const width = document.createElement('input');
    width.type = 'number';
    width.value = '2';
    page.changeBounds('width', width);
    page.addPot();
    page.addPlant('basil');

    fixture.detectChanges();
    const move = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>)
      .find(button => button.textContent?.trim() === 'Move to suggested spot');
    expect(move).toBeDefined();
    move!.click();
    expect(page.potBlocked()).toBe(false);
    expect(page.potLight()).toBe('FullSun');
    expect(page.selectedPot()?.x).toBe(2);
  });

  it('asks for missing moisture data and flags a mismatch in a shared pot', () => {
    const page = setup().componentInstance;
    page.addPot();
    page.addPlant('basil');
    page.addPlant('basil');
    expect(page.shared()?.label).toBe('More information needed');
    const [first, second] = page.selectedPot()!.plants;
    page.setMoisture(first.id, 'moist');
    page.setMoisture(second.id, 'moist');
    expect(page.shared()?.label).toBe('Possible match');
    page.setMoisture(second.id, 'dry');
    expect(page.shared()?.detail).toContain('soil moisture');
    page.addPlant('fern');
    expect(page.shared()?.detail).toContain('light');
  });

  it('restores layouts for the same account and keeps another account separate', () => {
    const first = setup();
    first.componentInstance.renamePlan('Balcony');
    first.componentInstance.setSetting('outdoor');
    first.componentInstance.addPot();
    first.destroy();
    TestBed.resetTestingModule();
    const reopened = setup();
    expect(reopened.componentInstance.plan().name).toBe('Balcony');
    expect(reopened.componentInstance.plan().setting).toBe('outdoor');
    expect(reopened.componentInstance.plan().pots).toHaveLength(1);
    TestBed.resetTestingModule();
    expect(setup('planner-test-other').componentInstance.plan().pots).toHaveLength(0);
  });
});
