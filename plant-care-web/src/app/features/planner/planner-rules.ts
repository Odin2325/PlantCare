import { PlantSpecies, SunlightRequirement } from '../../models/plant-species.model';

export const columns = 24;
export const rows = 16;
export const lightOptions = [
  { value: 'LowLight', label: 'Low light / shade' },
  { value: 'MediumIndirectLight', label: 'Medium indirect light' },
  { value: 'BrightIndirectLight', label: 'Bright indirect light' },
  { value: 'PartialSun', label: 'Partial direct sun' },
  { value: 'FullSun', label: 'Full direct sun' },
] as const;

export type Light = Exclude<SunlightRequirement, 'Unknown'>;
export type Moisture = '' | 'dry' | 'moderate' | 'moist';
export interface Bounds { x: number; y: number; width: number; height: number }
export interface Shape extends Bounds {
  id: string; name: string; kind: 'rectangle' | 'square' | 'circle';
  role: 'outline' | 'obstacle' | 'light'; light: Light | null;
}
export interface Planting { id: string; speciesId: string; moisture: Moisture }
export interface Pot extends Bounds { id: string; name: string; plants: Planting[] }
export interface Plan { id: string; name: string; setting: 'indoor' | 'outdoor'; shapes: Shape[]; pots: Pot[] }

export function lightLabel(value: SunlightRequirement | null): string {
  return lightOptions.find(option => option.value === value)?.label ?? 'Light unknown';
}

function circlesOverlap(a: Bounds, b: Bounds): boolean {
  return Math.hypot(a.x + a.width / 2 - b.x - b.width / 2,
    a.y + a.height / 2 - b.y - b.height / 2) < (a.width + b.width) / 2;
}

function shapeOverlapsPot(shape: Shape, pot: Bounds): boolean {
  if (shape.kind === 'circle') return circlesOverlap(shape, pot);
  const radius = pot.width / 2;
  const centerX = pot.x + radius;
  const centerY = pot.y + radius;
  const nearestX = Math.max(shape.x, Math.min(centerX, shape.x + shape.width));
  const nearestY = Math.max(shape.y, Math.min(centerY, shape.y + shape.height));
  return Math.hypot(centerX - nearestX, centerY - nearestY) < radius;
}

function contains(zone: Shape, pot: Bounds): boolean {
  if (zone.kind === 'circle') {
    const radius = pot.width / 2;
    const zoneRadius = zone.width / 2;
    return Math.hypot(pot.x + radius - zone.x - zoneRadius,
      pot.y + radius - zone.y - zoneRadius) + radius <= zoneRadius;
  }
  return pot.x >= zone.x && pot.x + pot.width <= zone.x + zone.width &&
    pot.y >= zone.y && pot.y + pot.height <= zone.y + zone.height;
}

export function lightAt(plan: Plan, pot: Bounds): Light | null {
  const zones = plan.shapes.filter(shape => shape.role === 'light' && shapeOverlapsPot(shape, pot));
  const zone = zones.find(shape => contains(shape, pot));
  return zone && zones.every(shape => shape.light === zone.light) ? zone.light : null;
}

export function placementProblem(plan: Plan, pot: Pot): string | null {
  const boundaries = plan.shapes.filter(shape => shape.role === 'outline');
  if (boundaries.length && !boundaries.some(shape => contains(shape, pot))) {
    return 'Move the whole pot inside a space boundary.';
  }
  const obstacle = plan.shapes.find(shape => shape.role === 'obstacle' && shapeOverlapsPot(shape, pot));
  if (obstacle) return 'This pot overlaps the obstacle “' + obstacle.name + '”.';
  const overlappingPot = plan.pots.find(other => other.id !== pot.id && circlesOverlap(other, pot));
  return overlappingPot ? 'This pot overlaps “' + overlappingPot.name + '”.' : null;
}

export function placementAdvice(plan: Plan, pot: Pot, catalog: ReadonlyMap<string, PlantSpecies>):
  { spot: Bounds; problem: null } | { spot: null; problem: string } {
  const needs = new Set(pot.plants.map(plant => catalog.get(plant.speciesId)?.sunlightRequirement));
  const light = needs.values().next().value;
  if (!light || needs.has(undefined) || needs.has('Unknown')) {
    return { spot: null, problem: 'Some plants are missing catalog light requirements.' };
  }
  if (needs.size > 1) {
    return { spot: null, problem: 'These plants need different light. Try placing them in separate pots.' };
  }
  if (!plan.shapes.some(shape => shape.role === 'light' && shape.light === light)) {
    return { spot: null, problem: 'Mark a light zone for ' + lightLabel(light).toLowerCase() + '.' };
  }
  let best: Bounds | null = null;
  let distance = Infinity;
  for (let y = 0; y <= rows - pot.height; y++) {
    for (let x = 0; x <= columns - pot.width; x++) {
      const candidate = { ...pot, x, y };
      const offset = (x - pot.x) ** 2 + (y - pot.y) ** 2;
      if (offset < distance && !placementProblem(plan, candidate) && lightAt(plan, candidate) === light) {
        best = candidate;
        distance = offset;
      }
    }
  }
  return best ? { spot: best, problem: null } : {
    spot: null, problem: 'There is no clear space for the whole pot in the matching light zones. Try moving obstacles or making a zone larger.',
  };
}

export function sharing(
  plants: readonly Pick<Planting, 'speciesId' | 'moisture'>[],
  catalog: ReadonlyMap<string, PlantSpecies>,
): { status: 'conflict' | 'review' | 'possible'; label: string; detail: string } | null {
  if (plants.length < 2) return null;
  const species = plants.map(plant => catalog.get(plant.speciesId));
  const light = new Set(species.flatMap(plant => plant && plant.sunlightRequirement !== 'Unknown' ? [plant.sunlightRequirement] : []));
  const moisture = new Set(plants.map(plant => plant.moisture).filter(Boolean));
  const minimum = Math.max(...species.map(plant => plant?.minimumTemperatureCelsius ?? -Infinity));
  const maximum = Math.min(...species.map(plant => plant?.maximumTemperatureCelsius ?? Infinity));
  const differences = [
    ...(light.size > 1 ? ['light'] : []),
    ...(moisture.size > 1 ? ['soil moisture'] : []),
    ...(minimum > maximum ? ['temperature'] : []),
  ];
  if (differences.length) return { status: 'conflict', label: 'Separate pots suggested', detail: 'Different needs for ' + differences.join(', ') + '.' };
  if (species.some(plant => !plant || plant.sunlightRequirement === 'Unknown') ||
      plants.some(plant => !plant.moisture)) {
    return { status: 'review', label: 'More information needed', detail: 'Check catalog light needs and choose a soil moisture preference for each plant.' };
  }
  return { status: 'possible', label: 'Possible match', detail: 'Light and selected soil moisture agree. Check mature size, root space, potting mix and drainage before planting together.' };
}
