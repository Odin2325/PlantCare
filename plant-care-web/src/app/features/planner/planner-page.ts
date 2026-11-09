import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/services/auth.service';
import { plannerStorageKey } from '../../core/services/planner-storage-key';
import { PlantSpeciesApiService } from '../../core/services/plant-species-api.service';
import { PlantSpecies } from '../../models/plant-species.model';
import { Bounds, Light, Moisture, Plan, Pot, Shape, blocked, columns, lightAt, lightLabel,
  lightOptions, placement, rows, sharing } from './planner-rules';

const maxPlans = 12;
const maxShapes = 40;
const maxPots = 40;
const maxPlants = 8;
const newPlan = (): Plan => ({ id: crypto.randomUUID(), name: 'My space', setting: 'indoor', shapes: [], pots: [] });

function validBounds(value: Bounds): boolean {
  return [value.x, value.y, value.width, value.height].every(Number.isInteger) &&
    value.x >= 0 && value.y >= 0 && value.width >= 1 && value.height >= 1 &&
    value.x + value.width <= columns && value.y + value.height <= rows;
}

function named(value: { id: string; name: string }): boolean {
  return !!value && typeof value.id === 'string' && !!value.id && typeof value.name === 'string' && value.name.length <= 60;
}

function validPlans(plans: Plan[]): boolean {
  return Array.isArray(plans) && plans.length > 0 && plans.length <= maxPlans && plans.every(plan =>
    named(plan) && ['indoor', 'outdoor'].includes(plan.setting) &&
    Array.isArray(plan.shapes) && plan.shapes.length <= maxShapes && plan.shapes.every(shape =>
      named(shape) && validBounds(shape) && ['rectangle', 'square', 'circle'].includes(shape.kind) &&
      (shape.kind === 'rectangle' || shape.width === shape.height) &&
      ['outline', 'obstacle', 'light'].includes(shape.role) && (shape.light === null || lightOptions.some(option => option.value === shape.light))) &&
    Array.isArray(plan.pots) && plan.pots.length <= maxPots && plan.pots.every(pot =>
      named(pot) && validBounds(pot) && pot.width === pot.height &&
      Array.isArray(pot.plants) && pot.plants.length <= maxPlants && pot.plants.every(plant =>
        !!plant && typeof plant.id === 'string' && typeof plant.speciesId === 'string' &&
        ['', 'dry', 'moderate', 'moist'].includes(plant.moisture))));
}

@Component({
  standalone: true,
  imports: [RouterLink],
  templateUrl: './planner-page.html',
  styleUrl: './planner-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlannerPage {
  private readonly auth = inject(AuthService);
  private readonly owner = this.auth.currentUser()?.id;
  private readonly api = inject(PlantSpeciesApiService);
  private readonly destroyRef = inject(DestroyRef);
  private storedValue: string | null = null;
  readonly saveMessage = signal('');
  readonly plans = signal(this.loadPlans());
  readonly planId = signal(this.plans()[0].id);
  readonly plan = computed(() => this.plans().find(plan => plan.id === this.planId()) ?? this.plans()[0]);
  readonly selection = signal<{ kind: 'shape' | 'pot'; id: string } | null>(null);
  readonly selectedShape = computed(() => this.selection()?.kind === 'shape'
    ? this.plan().shapes.find(shape => shape.id === this.selection()?.id) : undefined);
  readonly selectedPot = computed(() => this.selection()?.kind === 'pot'
    ? this.plan().pots.find(pot => pot.id === this.selection()?.id) : undefined);
  readonly item = computed(() => this.selectedShape() ?? this.selectedPot());
  readonly placing = signal(false);
  readonly species = signal<PlantSpecies[]>([]);
  readonly loading = signal(false);
  readonly catalogError = signal(false);
  readonly query = signal('');
  readonly matchLight = signal(false);
  readonly catalog = computed(() => new Map(this.species().map(plant => [plant.id, plant])));
  readonly choices = computed(() => {
    const query = this.query().trim().toLowerCase();
    const light = this.matchLight() ? this.potLight() : null;
    if (!query && !light) return [];
    return this.species().filter(plant => (!light || plant.sunlightRequirement === light) &&
      (!query || [plant.commonName, plant.scientificName, plant.catalogProfile?.cultivarName,
        ...(plant.catalogProfile?.commonNameAliases ?? [])].some(name => name?.toLowerCase().includes(query)))).slice(0, 12);
  });
  readonly potLight = computed(() => {
    const pot = this.selectedPot();
    return pot ? lightAt(this.plan(), pot) : null;
  });
  readonly suggestion = computed(() => {
    const pot = this.selectedPot();
    return pot ? placement(this.plan(), pot, this.catalog()) : null;
  });
  readonly shared = computed(() => {
    const pot = this.selectedPot();
    return pot ? sharing(pot, this.catalog()) : null;
  });
  readonly potBlocked = computed(() => {
    const pot = this.selectedPot();
    return !!pot && blocked(this.plan(), pot);
  });
  readonly columns = columns;
  readonly rows = rows;
  readonly lights = lightOptions;
  readonly lightLabel = lightLabel;
  readonly maxPlans = maxPlans;
  readonly maxShapes = maxShapes;
  readonly maxPots = maxPots;
  readonly maxPlants = maxPlants;

  constructor() { this.loadCatalog(); }

  loadCatalog(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.catalogError.set(false);
    this.api.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: species => { this.species.set(species); this.loading.set(false); },
      error: () => { this.catalogError.set(true); this.loading.set(false); },
    });
  }

  private loadPlans(): Plan[] {
    try {
      const raw = this.owner ? localStorage.getItem(plannerStorageKey(this.owner)) : null;
      this.storedValue = raw;
      if (raw) {
        const plans: Plan[] = JSON.parse(raw);
        if (validPlans(plans)) return plans;
        this.saveMessage.set('The saved planner layout could not be read.');
      }
    } catch {
      this.saveMessage.set('Browser storage is unavailable or the saved layout could not be read.');
    }
    return [newPlan()];
  }

  private save(plans: Plan[]): void {
    this.plans.set(plans);
    try {
      if (!this.owner || this.auth.currentUser()?.id !== this.owner) throw new Error();
      const key = plannerStorageKey(this.owner);
      if (localStorage.getItem(key) !== this.storedValue) {
        this.saveMessage.set('Another tab saved newer layouts. Reload to see them. Your latest edits are in this tab only.');
        return;
      }
      const value = JSON.stringify(plans);
      localStorage.setItem(key, value);
      this.storedValue = value;
      this.saveMessage.set('Saved in this browser.');
    } catch {
      this.saveMessage.set('Changes could not be saved in this browser.');
    }
  }

  private update(patch: Partial<Plan>): void {
    const id = this.plan().id;
    this.save(this.plans().map(plan => plan.id === id ? { ...plan, ...patch } : plan));
  }

  switchPlan(id: string): void { this.planId.set(id); this.clearSelection(); }
  addPlan(): void {
    if (this.plans().length >= maxPlans) return;
    const plan = newPlan();
    this.save([...this.plans(), plan]);
    this.switchPlan(plan.id);
  }
  deletePlan(): void {
    if (!window.confirm('Delete this layout from this browser?')) return;
    const remaining = this.plans().filter(plan => plan.id !== this.plan().id);
    if (!remaining.length) remaining.push(newPlan());
    this.save(remaining);
    this.switchPlan(remaining[0].id);
  }
  renamePlan(name: string): void { this.update({ name: name.trim().slice(0, 60) || 'My space' }); }
  setSetting(setting: string): void { this.update({ setting: setting === 'outdoor' ? 'outdoor' : 'indoor' }); }

  select(kind: 'shape' | 'pot', id: string): void {
    this.selection.set({ kind, id });
    this.matchLight.set(false);
    this.placing.set(false);
    this.query.set('');
  }
  selectFromList(id: string): void {
    if (this.plan().shapes.some(shape => shape.id === id)) this.select('shape', id);
    else if (this.plan().pots.some(pot => pot.id === id)) this.select('pot', id);
    else this.clearSelection();
  }
  clearSelection(): void { this.selection.set(null); this.placing.set(false); this.query.set(''); this.matchLight.set(false); }
  addShape(kind: Shape['kind']): void {
    if (this.plan().shapes.length >= maxShapes) return;
    const shape: Shape = { id: crypto.randomUUID(), name: 'New ' + kind, kind,
      role: 'outline', light: null, x: 0, y: 0, width: 8, height: kind === 'rectangle' ? 5 : 8 };
    this.update({ shapes: [...this.plan().shapes, shape] });
    this.select('shape', shape.id);
  }
  addPot(): void {
    if (this.plan().pots.length >= maxPots) return;
    const pot: Pot = { id: crypto.randomUUID(), name: 'Pot ' + (this.plan().pots.length + 1),
      x: 0, y: 0, width: 2, height: 2, plants: [] };
    this.update({ pots: [...this.plan().pots, pot] });
    this.select('pot', pot.id);
  }
  removeItem(): void {
    const item = this.selection();
    if (!item) return;
    if (item.kind === 'shape') this.update({ shapes: this.plan().shapes.filter(shape => shape.id !== item.id) });
    else this.update({ pots: this.plan().pots.filter(pot => pot.id !== item.id) });
    this.clearSelection();
  }
  renameItem(name: string): void { this.updateItem({ name: name.trim().slice(0, 60) || 'Unnamed item' }); }
  setRole(role: Shape['role']): void { this.updateShape({ role }); }
  setLight(light: Light | ''): void { this.updateShape({ light: light || null }); }

  private updateShape(patch: Partial<Shape>): void {
    const selected = this.selectedShape();
    if (selected) this.update({ shapes: this.plan().shapes.map(shape => shape.id === selected.id ? { ...shape, ...patch } : shape) });
  }
  private updatePot(patch: Partial<Pot>): void {
    const selected = this.selectedPot();
    if (selected) this.update({ pots: this.plan().pots.map(pot => pot.id === selected.id ? { ...pot, ...patch } : pot) });
  }
  private updateItem(patch: Partial<Bounds> & { name?: string }): void {
    if (this.selectedShape()) this.updateShape(patch);
    else this.updatePot(patch);
  }

  changeBounds(field: keyof Bounds, input: HTMLInputElement): void {
    const item = this.item();
    if (!item) return;
    const value = Number.isFinite(input.valueAsNumber) ? Math.round(input.valueAsNumber) : item[field];
    const next = { x: item.x, y: item.y, width: item.width, height: item.height, [field]: value };
    const equalSides = this.selectedShape()?.kind !== 'rectangle';
    next.width = Math.min(equalSides ? rows : columns, Math.max(1, next.width));
    next.height = equalSides ? next.width : Math.min(rows, Math.max(1, next.height));
    next.x = Math.min(columns - next.width, Math.max(0, next.x));
    next.y = Math.min(rows - next.height, Math.max(0, next.y));
    input.value = String(next[field]);
    this.updateItem(next);
  }

  place(event: MouseEvent, surface: HTMLButtonElement): void {
    const item = this.item();
    if (!item || event.detail === 0) return;
    const rect = surface.getBoundingClientRect();
    this.updateItem({
      x: Math.max(0, Math.min(columns - item.width, Math.floor((event.clientX - rect.left) / rect.width * columns))),
      y: Math.max(0, Math.min(rows - item.height, Math.floor((event.clientY - rect.top) / rect.height * rows))),
    });
    this.placing.set(false);
  }
  applySuggestion(): void {
    const candidate = this.suggestion();
    if (candidate) this.updatePot({ x: candidate.x, y: candidate.y });
  }
  box(item: Bounds): Record<string, string> {
    return { left: item.x / columns * 100 + '%', top: item.y / rows * 100 + '%',
      width: item.width / columns * 100 + '%', height: item.height / rows * 100 + '%' };
  }

  addPlant(speciesId: string): void {
    const pot = this.selectedPot();
    if (!pot || !this.catalog().has(speciesId) || pot.plants.length >= maxPlants) return;
    this.updatePot({ plants: [...pot.plants, { id: crypto.randomUUID(), speciesId, moisture: '' }] });
    this.query.set('');
  }
  removePlant(id: string): void {
    const pot = this.selectedPot();
    if (pot) this.updatePot({ plants: pot.plants.filter(plant => plant.id !== id) });
  }
  setMoisture(id: string, moisture: Moisture): void {
    const pot = this.selectedPot();
    if (pot) this.updatePot({ plants: pot.plants.map(plant => plant.id === id ? { ...plant, moisture } : plant) });
  }
}
