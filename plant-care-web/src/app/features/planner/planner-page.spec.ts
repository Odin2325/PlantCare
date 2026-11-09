import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, describe, expect, it } from 'vitest';

import { AuthService } from '../../core/auth/services/auth.service';
import { plannerStorageKey } from '../../core/services/planner-storage-key';
import { PlantSpeciesApiService } from '../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../models/plant-species.model';
import { PlannerPage } from './planner-page';
import { MyPlantsApiService } from '../my-plants/data-access/my-plants-api.service';
import { UserPlant } from '../my-plants/models/user-plant.model';

const basil: PlantSpecies = {
  id: 'basil', commonName: 'Basil', scientificName: 'Ocimum basilicum', description: '',
  sunlightRequirement: 'FullSun', sunlightInstructions: 'Direct sun.',
  defaultWateringIntervalDays: 3, wateringInstructions: 'Keep the mix evenly moist.',
  defaultFertilizingIntervalDays: null, fertilizingInstructions: null,
  soilInstructions: 'Well-drained mix.', humidityInstructions: null,
  minimumTemperatureCelsius: 12, maximumTemperatureCelsius: null, isToxicToPets: false,
};
const fern: PlantSpecies = { ...basil, id: 'fern', commonName: 'Fern', sunlightRequirement: 'LowLight' };
const cold: PlantSpecies = { ...basil, id: 'cold', commonName: 'Cold fixture',
  minimumTemperatureCelsius: null, maximumTemperatureCelsius: 8 };
const unknown: PlantSpecies = { ...basil, id: 'unknown', commonName: 'Unknown fixture', sunlightRequirement: 'Unknown' };

function setup(owner = 'planner-test-owner', species = [basil, fern, cold, unknown], collection: UserPlant[] = []) {
  TestBed.configureTestingModule({ providers: [
    provideRouter([]),
    { provide: AuthService, useValue: { currentUser: () => ({ id: owner }) } },
    { provide: PlantSpeciesApiService, useValue: { getAll: () => of(species) } },
    { provide: MyPlantsApiService, useValue: { getAll: () => of(collection) } },
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
    page.renameItem('Shelf');
    const width = document.createElement('input');
    width.type = 'number';
    width.value = '2';
    page.changeBounds('width', width);
    page.addPot();
    page.addPlant('basil');

    fixture.detectChanges();
    const move = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>)
      .find(button => button.textContent?.trim() === 'Move to suggested spot');
    expect(fixture.nativeElement.querySelector('.map-legend').textContent).toContain('Suggested pot position');
    expect(fixture.nativeElement.querySelector('.map-legend [data-light="FullSun"]').parentElement.textContent).toContain('Full direct sun');
    expect(fixture.nativeElement.querySelector('.map [data-light="FullSun"]').getAttribute('aria-label')).toContain('Full direct sun');
    expect(fixture.nativeElement.querySelector('.assessment').textContent).toContain('This pot overlaps the obstacle “Shelf”.');
    expect(fixture.nativeElement.querySelector('.suggestion-marker')).not.toBeNull();
    expect(move).toBeDefined();
    move!.click();
    expect(page.potPlacementProblem()).toBeNull();
    expect(page.potLight()).toBe('FullSun');
    expect(page.selectedPot()?.x).toBe(2);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.suggestion-marker')).toBeNull();
    expect(fixture.nativeElement.querySelector('.assessment').textContent).not.toContain('Shelf');
  });

  it('moves the selected item one square with the direction buttons', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addPot();
    fixture.detectChanges();
    const button = (direction: string): HTMLButtonElement =>
      fixture.nativeElement.querySelector('button[aria-label="Move ' + direction + '"]');

    expect(button('left').disabled).toBe(true);
    expect(button('up').disabled).toBe(true);
    button('right').click();
    button('down').click();
    fixture.detectChanges();
    expect(page.selectedPot()).toMatchObject({ x: 1, y: 1, width: 2, height: 2 });
    button('left').click();
    fixture.detectChanges();
    expect(page.selectedPot()?.x).toBe(0);
    expect(button('left').disabled).toBe(true);
  });

  it('restores the last removed pot with its plantings and clears undo when changing layouts', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addPot();
    page.addPlant('basil');
    const original = page.selectedPot()!;
    fixture.detectChanges();
    const action = (label: string) => Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>)
      .find(button => button.textContent?.trim() === label);
    action('Remove item')!.click();
    fixture.detectChanges();
    expect(page.plan().pots).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain('Removed Pot 1');
    action('Undo removal')!.click();
    fixture.detectChanges();
    expect(page.selectedPot()).toEqual(original);
    expect(action('Undo removal')).toBeUndefined();

    action('Remove item')!.click();
    page.addPlan();
    fixture.detectChanges();
    expect(action('Undo removal')).toBeUndefined();
    expect(page.plan().pots).toHaveLength(0);
  });

  it('shows layout checks across pots and lets the user select one to review', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    page.addPot();
    page.addPlant('basil');
    page.addPot();
    page.nudge(2, 0);
    page.addPlant('fern');
    fixture.detectChanges();

    const checklist = fixture.nativeElement.querySelector('.layout-check');
    expect(checklist.querySelector('summary').textContent).toContain('1 to review');
    expect(checklist.textContent).toContain('Matches the catalog light needs.');
    expect(checklist.textContent).toContain('Needs low light / shade.');
    checklist.querySelectorAll('button')[0].click();
    expect(page.selectedPot()?.name).toBe('Pot 1');
  });

  it('counts shared-pot conflicts even when the current light matches', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    page.addPot();
    page.addPlant('basil');
    page.addPlant('basil');
    const [first, second] = page.selectedPot()!.plants;
    page.setMoisture(first.id, 'moist');
    page.setMoisture(second.id, 'dry');
    fixture.detectChanges();

    const checklist = fixture.nativeElement.querySelector('.layout-check');
    expect(checklist.querySelector('summary').textContent).toContain('1 to review');
    expect(checklist.textContent).toContain('Matches the catalog light needs.');
    expect(checklist.textContent).toContain('Different needs for soil moisture.');
    page.setMoisture(second.id, 'moist');
    fixture.detectChanges();
    expect(checklist.querySelector('summary').textContent).toContain('0 to review');
    expect(checklist.querySelector('.layout-companion-issue')).toBeNull();
  });

  it('lists owned and unowned matches separately and plans each inside the selected zone', () => {
    const collection = [
      { id: 'mine-1', plantSpeciesId: 'basil', nickname: 'Kitchen basil', speciesCommonName: 'Basil', isActive: true },
      { id: 'mine-2', plantSpeciesId: 'basil', nickname: 'Balcony basil', speciesCommonName: 'Basil', isActive: true },
      { id: 'mine-3', plantSpeciesId: 'fern', nickname: 'Hall fern', speciesCommonName: 'Fern', isActive: true },
      { id: 'archived', plantSpeciesId: 'cold', nickname: 'Old plant', speciesCommonName: 'Cold fixture', isActive: false },
    ] as UserPlant[];
    const fixture = setup(undefined, [basil, fern, cold, unknown], collection);
    const page = fixture.componentInstance;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    page.nudge(4, 0);
    const zoneId = page.selectedZone()!.id;
    fixture.detectChanges();

    const finder = () => fixture.nativeElement.querySelector('.zone-plant-finder');
    const buttons = () => Array.from(finder().querySelectorAll('.plant-list button') as NodeListOf<HTMLButtonElement>);
    expect(buttons().map(button => button.textContent?.trim())).toEqual(['Plan Balcony basil', 'Plan Kitchen basil']);
    buttons()[1].click();
    expect(page.selectedPot()).toMatchObject({ name: 'Kitchen basil', x: 4, y: 0,
      plants: [{ speciesId: 'basil', userPlantId: 'mine-1' }] });
    expect(page.potLight()).toBe('FullSun');

    page.select('shape', zoneId);
    fixture.detectChanges();
    expect(buttons()[1].disabled).toBe(true);
    const mode = finder().querySelector('select') as HTMLSelectElement;
    mode.value = 'catalog';
    mode.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(buttons().map(button => button.textContent?.trim())).toEqual(['Plan Cold fixture']);
    buttons()[0].click();
    expect(page.selectedPot()?.plants[0].userPlantId).toBeUndefined();
    expect(page.selectedPot()?.x).toBe(6);
    expect(page.potPlacementProblem()).toBeNull();
    expect(page.collection()).toEqual(collection.filter(plant => plant.isActive));

    fixture.destroy();
    TestBed.resetTestingModule();
    const reopened = setup(undefined, [basil, fern, cold, unknown], collection).componentInstance;
    expect(reopened.plan().pots.map(pot => pot.plants[0].userPlantId)).toEqual(['mine-1', undefined]);
    expect(reopened.plan().pots.map(pot => pot.x)).toEqual([4, 6]);
  });

  it('lets a plant join an existing pot in the selected zone and warns about companion conflicts', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    const width = document.createElement('input');
    width.type = 'number';
    width.value = '2';
    page.changeBounds('width', width);
    const zoneId = page.selectedZone()!.id;
    page.addPot();
    page.addPlant('basil');
    const destination = page.selectedPot()!;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    page.nudge(4, 0);
    page.addPot();
    page.nudge(4, 0);

    page.select('shape', zoneId);
    page.browseCatalog.set(true);
    fixture.detectChanges();
    const finder = fixture.nativeElement.querySelector('.zone-plant-finder');
    const select = finder.querySelectorAll('select')[1] as HTMLSelectElement;
    expect(Array.from(select.options).map(option => option.value)).toEqual(['', destination.id]);
    expect(page.zoneSpot()).toBeNull();
    expect(page.canPlanInZone()).toBe(false);

    select.value = destination.id;
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(page.canPlanInZone()).toBe(true);
    const candidate = Array.from(finder.querySelectorAll('.plant-list button') as NodeListOf<HTMLButtonElement>)
      .find(button => button.textContent?.trim() === 'Plan Cold fixture');
    expect(candidate?.parentElement?.textContent).toContain('Different needs for temperature.');
    candidate!.click();
    expect(page.plan().pots).toHaveLength(2);
    expect(page.selectedPot()?.id).toBe(destination.id);
    expect(page.selectedPot()?.plants.map(plant => plant.speciesId)).toEqual(['basil', 'cold']);
  });

  it('keeps the plant ideas visible when the selected zone cannot fit a pot', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    const width = document.createElement('input');
    width.type = 'number';
    width.value = '1';
    page.changeBounds('width', width);
    page.browseCatalog.set(true);
    fixture.detectChanges();

    const finder = fixture.nativeElement.querySelector('.zone-plant-finder');
    expect(finder.querySelector('.plant-list button').disabled).toBe(true);
    expect(finder.textContent).toContain('no clear space');
    page.planZonePlant('basil');
    expect(page.plan().pots).toHaveLength(0);
  });

  it('explains why it cannot suggest a light zone', () => {
    const page = setup().componentInstance;
    page.addPot();
    page.addPlant('basil');
    expect(page.placementAdvice()?.problem).toBe('Mark a light zone for full direct sun.');

    page.addPlant('fern');
    expect(page.placementAdvice()?.problem).toContain('different light');
    page.addPlant('unknown');
    expect(page.placementAdvice()?.problem).toContain('missing catalog light requirements');
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

  it('previews conflicts before adding and filters unknown and conflicting companions', () => {
    const page = setup().componentInstance;
    page.addPot();
    page.addPlant('basil');
    page.query.set('fern');
    expect(page.choices()[0].preview).toContain('Different needs for light');
    page.query.set('cold');
    expect(page.choices()[0].preview).toContain('temperature');
    page.query.set('');
    page.companionsOnly.set(true);
    expect(page.choices().map(choice => choice.plant.id)).toEqual(['basil']);
    expect(page.choices()[0].preview).toContain('Check soil moisture');
    expect(page.selectedPot()?.plants).toHaveLength(1);
  });

  it('shows later plant matches and resets paging when the search changes', () => {
    const species = Array.from({ length: 13 }, (_, index) => ({
      ...basil, id: String(index), commonName: 'Basil ' + (index + 1),
    }));
    const fixture = setup(undefined, species);
    const page = fixture.componentInstance;
    page.addPot();
    fixture.detectChanges();
    const search = fixture.nativeElement.querySelector('input[type="search"]') as HTMLInputElement;
    const buttons = () => Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>);
    const searchFor = (value: string) => {
      search.value = value;
      search.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };
    const resultLabels = () => buttons().map(button => button.textContent?.trim())
      .filter(label => label?.startsWith('Add Basil'));

    searchFor('Basil');
    expect(resultLabels()).toHaveLength(12);
    buttons().find(button => button.textContent?.trim() === 'Next plants')!.click();
    fixture.detectChanges();
    expect(resultLabels()).toEqual(['Add Basil 13']);

    searchFor('Basil 3');
    expect(page.choicePage()).toBe(0);
    expect(resultLabels()).toEqual(['Add Basil 3']);
  });

  it('moves a planting once, keeping its identity and moisture preference', () => {
    const page = setup().componentInstance;
    page.addPot();
    const source = page.selectedPot()!.id;
    page.addPlant('basil');
    const original = page.selectedPot()!.plants[0];
    page.setMoisture(original.id, 'moist');
    page.addPot();
    const destination = page.selectedPot()!.id;
    page.select('pot', source);
    page.movePlant(original.id, destination);
    expect(page.selectedPot()?.id).toBe(destination);
    expect(page.selectedPot()?.plants).toEqual([{ ...original, moisture: 'moist' }]);
    expect(page.plan().pots.find(pot => pot.id === source)?.plants).toEqual([]);
    page.movePlant(original.id, destination);
    expect(page.selectedPot()?.plants).toHaveLength(1);
  });

  it('duplicates a layout and saves its independent changes alongside the original', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.renamePlan('Balcony');
    page.setSetting('outdoor');
    page.addShape('rectangle');
    page.setRole('light');
    page.setLight('FullSun');
    page.addPot();
    page.addPlant('basil');
    const original = page.plan();
    const planting = original.pots[0].plants[0];
    page.setMoisture(planting.id, 'moist');

    fixture.detectChanges();
    const duplicate = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>)
      .find(button => button.textContent?.trim() === 'Duplicate layout');
    duplicate!.click();

    const copy = page.plan();
    expect(copy.name).toBe('Balcony (copy)');
    expect(copy.id).not.toBe(original.id);
    expect(copy.setting).toBe('outdoor');
    expect(copy.shapes[0]).toEqual({ ...original.shapes[0], id: copy.shapes[0].id });
    expect(copy.shapes[0].id).not.toBe(original.shapes[0].id);
    expect(copy.pots[0].id).not.toBe(original.pots[0].id);
    expect(copy.pots[0].plants[0].id).not.toBe(planting.id);
    expect(copy.pots[0].plants[0]).toMatchObject({ speciesId: 'basil', moisture: 'moist' });
    page.select('pot', copy.pots[0].id);
    page.setMoisture(copy.pots[0].plants[0].id, 'dry');

    fixture.destroy();
    TestBed.resetTestingModule();
    const saved = setup().componentInstance.plans();
    expect(saved.map(plan => plan.pots[0].plants[0].moisture)).toEqual(['moist', 'dry']);
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
