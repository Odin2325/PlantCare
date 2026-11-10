import { Plan, Pot, Shape } from './planner-rules';

export interface StarterLayout {
  name: string;
  setting: Plan['setting'];
  description: string;
  shapes: readonly Omit<Shape, 'id'>[];
  pots: readonly Omit<Pot, 'id' | 'plants'>[];
}

export const starterLayouts: readonly StarterLayout[] = [
  {
    name: 'Windowsill',
    setting: 'indoor',
    description: 'A shallow sill with two areas to observe and two small pots.',
    shapes: [
      { name: 'Sill', kind: 'rectangle', role: 'outline', light: null, x: 3, y: 5, width: 18, height: 5 },
      { name: 'Left side', kind: 'rectangle', role: 'light', light: null, x: 3, y: 5, width: 9, height: 5 },
      { name: 'Right side', kind: 'rectangle', role: 'light', light: null, x: 12, y: 5, width: 9, height: 5 },
    ],
    pots: [
      { name: 'Left pot', x: 6, y: 6, width: 2, height: 2 },
      { name: 'Right pot', x: 15, y: 6, width: 2, height: 2 },
    ],
  },
  {
    name: 'Balcony',
    setting: 'outdoor',
    description: 'A rail side, a wall side, and two pots to try in either area.',
    shapes: [
      { name: 'Balcony boundary', kind: 'rectangle', role: 'outline', light: null, x: 2, y: 3, width: 20, height: 10 },
      { name: 'Rail side', kind: 'rectangle', role: 'light', light: null, x: 2, y: 3, width: 20, height: 4 },
      { name: 'Wall side', kind: 'rectangle', role: 'light', light: null, x: 2, y: 9, width: 20, height: 4 },
    ],
    pots: [
      { name: 'Rail pot', x: 5, y: 4, width: 2, height: 2 },
      { name: 'Wall pot', x: 16, y: 10, width: 2, height: 2 },
    ],
  },
  {
    name: 'Small garden',
    setting: 'outdoor',
    description: 'Two planting areas separated by a path to keep clear.',
    shapes: [
      { name: 'Garden boundary', kind: 'rectangle', role: 'outline', light: null, x: 2, y: 2, width: 20, height: 12 },
      { name: 'Left planting area', kind: 'rectangle', role: 'light', light: null, x: 2, y: 2, width: 9, height: 12 },
      { name: 'Right planting area', kind: 'rectangle', role: 'light', light: null, x: 13, y: 2, width: 9, height: 12 },
      { name: 'Path', kind: 'rectangle', role: 'obstacle', light: null, x: 11, y: 2, width: 2, height: 12 },
    ],
    pots: [
      { name: 'Left container', x: 5, y: 5, width: 3, height: 3 },
      { name: 'Right container', x: 16, y: 8, width: 3, height: 3 },
    ],
  },
];
