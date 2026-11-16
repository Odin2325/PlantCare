import { signal } from '@angular/core';
import { ShortlistService } from '../../core/services/shortlist.service';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../core/auth/services/auth.service';
import { plannerStorageKey } from '../../core/services/planner-storage-key';
import { PlantSpeciesApiService } from '../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../models/plant-species.model';
import { PlannerPage } from './planner-page';
import { serializeLayout } from './planner-layout-file';
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

function setup(owner = 'planner-test-owner', species = [basil, fern, cold, unknown], collection: UserPlant[] = [], shortlistIds: string[] = []) {
  TestBed.configureTestingModule({ providers: [
    provideRouter([]),
    { provide: ShortlistService, useValue: { ids: signal(shortlistIds), ready: signal(true), error: signal(null) } },
    { provide: AuthService, useValue: { currentUser: () => ({ id: owner }) } },
    { provide: PlantSpeciesApiService, useValue: { getAll: () => of(species) } },
    { provide: MyPlantsApiService, useValue: { getAll: () => of(collection) } },
  ] });
  return TestBed.createComponent(PlannerPage);
}

afterEach(() => {
  vi.restoreAllMocks();
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

  it('previews and moves a whole pot within the chosen zone, without falling back when it is blocked', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    for (const [name, x] of [['Near zone', 0], ['Window zone', 10], ['Blocked zone', 18]] as const) {
      page.addShape('square');
      page.renameItem(name);
      page.setRole('light');
      page.setLight('FullSun');
      const width = document.createElement('input');
      width.type = 'number';
      width.value = '5';
      page.changeBounds('width', width);
      page.nudge(x, 0);
    }
    page.addShape('square');
    page.setRole('obstacle');
    page.nudge(18, 0);
    page.addPot();
    page.addPlant('basil');
    const original = page.selectedPot()!;
    fixture.detectChanges();

    const placement = fixture.nativeElement.querySelector('.assessment');
    const select = placement.querySelector('select') as HTMLSelectElement;
    const choose = (name: string) => {
      select.value = Array.from(select.options).find(option => option.text === name)!.value;
      select.dispatchEvent(new Event('change'));
      fixture.detectChanges();
    };
    expect(page.suggestion()?.x).toBe(0);
    choose('Blocked zone');
    expect(placement.textContent).toContain('There is no clear space for the whole pot in this zone.');
    expect(fixture.nativeElement.querySelector('.suggestion-marker')).toBeNull();
    choose('Window zone');
    expect(page.suggestion()?.x).toBe(10);
    expect(page.selectedPot()?.x).toBe(0);
    expect(fixture.nativeElement.querySelector('.suggestion-marker')).not.toBeNull();
    placement.querySelector('button').click();
    expect(page.selectedPot()).toEqual({ ...original, x: 10 });
  });

  it('starts a separate editable balcony layout with unmeasured light and persists both layouts', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.renamePlan('My room');
    page.addShape('circle');
    const original = page.plan();
    fixture.detectChanges();

    const starters = fixture.nativeElement.querySelector('.layout-starters') as HTMLDetailsElement;
    starters.open = true;
    const balcony = Array.from(starters.querySelectorAll('button'))
      .find(button => button.textContent?.trim() === 'Use Balcony')!;
    balcony.click();
    fixture.detectChanges();

    const starter = page.plan();
    expect(starter).toMatchObject({ name: 'Balcony', setting: 'outdoor' });
    expect(starter.pots).toHaveLength(2);
    expect(starter.pots.every(pot => !pot.plants.length)).toBe(true);
    expect(starter.shapes.filter(shape => shape.role === 'light').map(shape => shape.light)).toEqual([null, null]);
    expect(page.selectedShape()?.name).toBe('Rail side');
    page.setLight('FullSun');
    expect(page.plans()[0]).toEqual(original);

    fixture.destroy();
    TestBed.resetTestingModule();
    const saved = setup().componentInstance.plans();
    expect(saved.map(plan => plan.name)).toEqual(['My room', 'Balcony']);
    expect(saved[1].shapes.find(shape => shape.name === 'Rail side')?.light).toBe('FullSun');
  });

  it('imports a portable copy without collection links and rejects invalid bounds without changing layouts', async () => {
    const page = setup().componentInstance;
    page.addPot();
    page.addPlant('basil');
    const original = page.plan();
    const owned = { ...original, pots: original.pots.map(pot => ({
      ...pot, plants: pot.plants.map(plant => ({ ...plant, userPlantId: 'private-plant-id' })),
    })) };
    const json = serializeLayout(owned);
    expect(json).not.toContain('private-plant-id');

    const selectFile = (text: string) => ({
      files: [{ size: text.length, text: async () => text }], value: 'layout.json',
    }) as unknown as HTMLInputElement;
    const invalid = JSON.parse(json);
    invalid.layout.pots[0].x = -1;
    await page.importLayout(selectFile(JSON.stringify(invalid)));
    expect(page.layoutFileMessage()).toContain('not a supported');
    expect(page.plans()).toEqual([original]);

    // Even a file crafted with a private identifier cannot import that link.
    const portable = JSON.parse(json);
    portable.layout.pots[0].plants[0].userPlantId = 'forged-id';
    await page.importLayout(selectFile(JSON.stringify(portable)));
    expect(page.plans()).toHaveLength(2);
    expect(page.plans()[0]).toEqual(original);
    expect(page.plan().id).not.toBe(original.id);
    expect(page.plan().pots[0].id).not.toBe(original.pots[0].id);
    expect(page.plan().pots[0].plants[0]).toMatchObject({ speciesId: 'basil', moisture: '' });
    expect(page.plan().pots[0].plants[0].userPlantId).toBeUndefined();
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

  it('previews map taps and only saves the position after applying', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.addPot();
    fixture.detectChanges();
    const map = fixture.nativeElement.querySelector('.map-panel') as HTMLElement;
    const action = (label: string) => Array.from(map.querySelectorAll('button'))
      .find(button => button.textContent?.trim() === label)!;
    const preview = (x: number, y: number) => {
      const surface = map.querySelector<HTMLButtonElement>('.place-surface')!;
      vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(10, 20, 240, 160));
      surface.dispatchEvent(new MouseEvent('click', { detail: 1, clientX: x, clientY: y }));
      fixture.detectChanges();
    };

    action('Place on map').click();
    fixture.detectChanges();
    expect(action('Apply position').disabled).toBe(true);
    preview(62, 51);
    expect(map.querySelector('.placement-preview')).not.toBeNull();
    expect(map.querySelector('[role="status"]')?.textContent).toContain('column 5, row 3');
    expect(page.selectedPot()).toMatchObject({ x: 0, y: 0 });

    action('Cancel placement').click();
    fixture.detectChanges();
    expect(map.querySelector('.placement-preview')).toBeNull();
    expect(page.selectedPot()).toMatchObject({ x: 0, y: 0 });

    action('Place on map').click();
    fixture.detectChanges();
    preview(249, 179);
    expect(page.selectedPot()).toMatchObject({ x: 0, y: 0 });
    action('Apply position').click();
    fixture.detectChanges();

    expect(map.querySelector('.placement-preview')).toBeNull();
    expect(page.selectedPot()).toMatchObject({ x: 22, y: 14 });
    const saved = JSON.parse(localStorage.getItem(plannerStorageKey('planner-test-owner'))!);
    expect(saved[0].pots[0]).toMatchObject({ x: 22, y: 14 });
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
    expect(fixture.nativeElement.querySelector('.inspector .assessment [role="status"]').textContent).toContain('Removed Pot 1');
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
    const careNotes = finder().querySelector('summary[aria-label="Care notes for Kitchen basil"]') as HTMLElement;
    const preview = careNotes.closest('details')!;
    expect(preview.open).toBe(false);
    careNotes.click();
    expect(preview.open).toBe(true);
    expect(preview.querySelector('dl')?.textContent).toContain('Keep the mix evenly moist.');
    expect(preview.querySelector('dl')?.textContent).toContain('Well-drained mix.');
    expect(page.plan().pots).toEqual([]);
    buttons()[1].click();
    expect(page.selectedPot()).toMatchObject({ name: 'Kitchen basil', x: 4, y: 0,
      plants: [{ speciesId: 'basil', userPlantId: 'mine-1' }] });
    expect(page.potLight()).toBe('FullSun');

    fixture.detectChanges();
    const backToZone = () => fixture.nativeElement.querySelector('button[aria-label^="Back to zone suggestions for "]') as HTMLButtonElement;
    expect(backToZone()).not.toBeNull();
    backToZone().click();
    fixture.detectChanges();
    expect(page.selectedZone()?.id).toBe(zoneId);
    expect(buttons()[1].disabled).toBe(true);
    const hidePlanned = finder().querySelector('input[type="checkbox"]') as HTMLInputElement;
    hidePlanned.click();
    fixture.detectChanges();
    expect(hidePlanned.checked).toBe(true);
    expect(buttons().map(button => button.textContent?.trim())).toEqual(['Plan Balcony basil']);
    expect(buttons()[0].disabled).toBe(false);

    const mode = finder().querySelector('select') as HTMLSelectElement;
    mode.value = 'catalog';
    mode.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    page.zoneQuery.set('Cold');
    fixture.detectChanges();
    expect(buttons().map(button => button.textContent?.trim())).toEqual(['Plan Cold fixture']);
    buttons()[0].click();
    expect(page.selectedPot()?.plants[0].userPlantId).toBeUndefined();
    expect(page.selectedPot()?.x).toBe(6);
    expect(page.potPlacementProblem()).toBeNull();
    expect(page.collection()).toEqual(collection.filter(plant => plant.isActive));

    fixture.detectChanges();
    backToZone().click();
    fixture.detectChanges();
    expect(page.zoneSource()).toBe('catalog');
    expect(page.zoneQuery()).toBe('Cold');
    expect(page.hidePlannedZonePlants()).toBe(true);
    expect(buttons().map(button => button.textContent?.trim())).toEqual(['Plan Cold fixture']);
    page.select('pot', page.plan().pots[0].id);
    fixture.detectChanges();
    expect(backToZone()).toBeNull();

    fixture.destroy();
    TestBed.resetTestingModule();
    const reopened = setup(undefined, [basil, fern, cold, unknown], collection).componentInstance;
    expect(reopened.plan().pots.map(pot => pot.plants[0].userPlantId)).toEqual(['mine-1', undefined]);
    expect(reopened.plan().pots.map(pot => pot.x)).toEqual([4, 6]);
  });

  it('shows only matching unowned shortlist plants and plans them locally', () => {
    const third = { ...basil, id: 'mint', commonName: 'Mint' };
    const collection = [{ id: 'my-basil', plantSpeciesId: 'basil', nickname: 'Kitchen basil', isActive: true }] as UserPlant[];
    const fixture = setup(undefined, [basil, fern, cold, third], collection, ['basil', 'fern', 'mint']);
    const page = fixture.componentInstance;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    fixture.detectChanges();

    const finder = fixture.nativeElement.querySelector('.zone-plant-finder') as HTMLElement;
    const source = finder.querySelector('select')!;
    source.value = 'shortlist';
    source.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const results = finder.querySelectorAll<HTMLButtonElement>('.plant-list button');
    expect(Array.from(results).map(button => button.textContent?.trim())).toEqual(['Plan Mint']);
    results[0].click();
    expect(page.plan().pots[0].plants[0].speciesId).toBe('mint');
    expect(page.plan().pots[0].plants[0].userPlantId).toBeUndefined();
  });

  it('filters zone matches by category and light across collection, shortlist and catalog', () => {
    const herb: PlantSpecies = { ...basil, catalogProfile: {
      categories: ['herb'], careSourceUrl: 'https://example.org/basil', reviewedOn: '2026-11-16', photo: null,
    } };
    const crop: PlantSpecies = { ...cold, commonName: 'Tomato', scientificName: 'Solanum lycopersicum', catalogProfile: {
      categories: ['vegetable'], careSourceUrl: 'https://example.org/tomato', reviewedOn: '2026-11-16', photo: null,
    } };
    const shadeCrop: PlantSpecies = { ...crop, id: 'shade-crop', sunlightRequirement: 'LowLight' };
    const collection = [{ id: 'mine', plantSpeciesId: 'basil', nickname: 'Kitchen basil', isActive: true }] as UserPlant[];
    const fixture = setup(undefined, [herb, crop, shadeCrop, { ...basil, id: 'untagged' }], collection,
      ['cold', 'shade-crop']);
    const page = fixture.componentInstance;
    page.addShape('square');
    page.setRole('light');
    page.setLight('FullSun');
    fixture.detectChanges();
    const finder = fixture.nativeElement.querySelector('.zone-plant-finder') as HTMLElement;
    const source = finder.querySelector<HTMLSelectElement>('#zone-plant-source')!;
    const category = finder.querySelector<HTMLSelectElement>('#zone-plant-category')!;
    const choose = (select: HTMLSelectElement, value: string) => {
      select.value = value;
      select.dispatchEvent(new Event('change'));
      fixture.detectChanges();
    };
    const names = () => page.zoneChoices().map(choice => choice.name);

    choose(category, 'herb');
    expect(names()).toEqual(['Kitchen basil']);
    choose(source, 'catalog');
    expect(names()).toEqual([]);
    choose(category, 'vegetable');
    expect(names()).toEqual(['Tomato']);
    choose(source, 'shortlist');
    expect(names()).toEqual(['Tomato']);
    page.zoneQuery.set('basil');
    expect(names()).toEqual([]);
    page.zoneQuery.set('');

    const zoneId = page.selectedZone()!.id;
    page.select('shape', zoneId);
    fixture.detectChanges();
    expect(category.value).toBe('');
    expect(names()).toEqual(['Kitchen basil']);
    expect(page.plan().pots).toEqual([]);
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
    page.zoneSource.set('catalog');
    fixture.detectChanges();
    const finder = fixture.nativeElement.querySelector('.zone-plant-finder');
    const select = finder.querySelector('#zone-plant-destination') as HTMLSelectElement;
    expect(Array.from(select.options).map(option => option.value)).toEqual(['', destination.id]);
    expect(page.zoneSpot()).toBeNull();
    expect(page.canPlanInZone()).toBe(false);

    select.value = destination.id;
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(page.canPlanInZone()).toBe(true);
    const candidate = () => Array.from(finder.querySelectorAll('.plant-list button') as NodeListOf<HTMLButtonElement>)
      .find(button => button.textContent?.trim() === 'Plan Cold fixture');
    expect(candidate()?.parentElement?.textContent).toContain('Different needs for temperature.');
    const toggle = finder.querySelector('input[type="checkbox"]') as HTMLInputElement;
    toggle.click();
    fixture.detectChanges();
    expect(candidate()).toBeUndefined();
    toggle.click();
    fixture.detectChanges();
    candidate()!.click();
    expect(page.plan().pots).toHaveLength(2);
    expect(page.selectedPot()?.id).toBe(destination.id);
    expect(page.selectedPot()?.plants.map(plant => plant.speciesId)).toEqual(['basil', 'cold']);
    page.returnToZoneSuggestions();
    expect(page.selectedZone()?.id).toBe(zoneId);
    expect(page.zoneDestination()?.id).toBe(destination.id);
    expect(page.zoneSource()).toBe('catalog');
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
    page.zoneSource.set('catalog');
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

  it('undoes deletion of a layout with its plantings and persists the restored layout', () => {
    const fixture = setup();
    const page = fixture.componentInstance;
    page.renamePlan('Balcony');
    page.addPot();
    page.addPlant('basil');
    const balcony = page.plan();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    page.deletePlan();

    fixture.detectChanges();
    expect(page.plan().pots).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('.layout-undo [role="status"]').textContent).toContain('Deleted Balcony.');
    const button = fixture.nativeElement.querySelector('.layout-undo button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(page.plans()).toEqual([balcony]);
    expect(page.plan()).toEqual(balcony);
    expect(fixture.nativeElement.querySelector('.layout-undo button')).toBeNull();
    expect(JSON.parse(localStorage.getItem(plannerStorageKey('planner-test-owner'))!)).toEqual([balcony]);
  });

  it('preserves edits made to the replacement layout before undo', () => {
    const page = setup().componentInstance;
    page.renamePlan('Balcony');
    const balcony = page.plan();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    page.deletePlan();
    page.renamePlan('Kitchen');
    const kitchen = page.plan();

    page.undoLayoutDeletion();

    expect(page.plans()).toEqual([balcony, kitchen]);
    expect(page.plan()).toEqual(balcony);
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
