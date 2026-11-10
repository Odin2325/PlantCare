import { Bounds, Plan } from './planner-rules';

function bounds({ x, y, width, height }: Bounds): Bounds {
  return { x, y, width, height };
}

export function portableLayout(plan: Plan, getId = (id: string) => id): Plan {
  return {
    id: getId(plan.id),
    name: plan.name,
    setting: plan.setting,
    shapes: plan.shapes.map(shape => ({
      ...bounds(shape), id: getId(shape.id), name: shape.name,
      kind: shape.kind, role: shape.role, light: shape.light,
    })),
    pots: plan.pots.map(pot => ({
      ...bounds(pot), id: getId(pot.id), name: pot.name,
      plants: pot.plants.map(plant => ({
        id: getId(plant.id), speciesId: plant.speciesId, moisture: plant.moisture,
      })),
    })),
  };
}

export function serializeLayout(plan: Plan): string {
  return JSON.stringify({ format: 'plantcare-layout', version: 1, layout: portableLayout(plan) }, null, 2);
}
