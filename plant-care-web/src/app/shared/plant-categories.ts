import { PlantCategory } from '../models/plant-species.model';

export const plantCategoryOptions: ReadonlyArray<{ value: PlantCategory; label: string }> = [
  { value: 'houseplant', label: 'Houseplants' },
  { value: 'fruit', label: 'Fruit' },
  { value: 'vegetable', label: 'Vegetables' },
  { value: 'root-tuber', label: 'Roots & tubers' },
  { value: 'herb', label: 'Herbs' },
  { value: 'tree', label: 'Trees' },
];

export function parsePlantCategory(value: string | null): PlantCategory | '' {
  return plantCategoryOptions.find(option => option.value === value)?.value ?? '';
}

export function plantCategoryLabel(value: PlantCategory): string {
  return plantCategoryOptions.find(option => option.value === value)?.label ?? value;
}
