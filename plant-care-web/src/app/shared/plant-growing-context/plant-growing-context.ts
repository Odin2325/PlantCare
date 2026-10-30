import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { MyPlantsApiService } from '../../features/my-plants/data-access/my-plants-api.service';
import { UserPlant } from '../../features/my-plants/models/user-plant.model';

@Component({
  selector: 'app-plant-growing-context', standalone: true, imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './plant-growing-context.html', styleUrl: './plant-growing-context.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantGrowingContext {
  private readonly api = inject(MyPlantsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private plantId = '';
  readonly plant = input.required<UserPlant>();
  readonly saved = output<UserPlant>();
  readonly saving = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly environments = ['indoor', 'balcony', 'greenhouse', 'outdoor'] as const;
  readonly seasons = ['spring', 'summer', 'autumn', 'winter'] as const;
  readonly form = new FormGroup({
    environment: new FormControl('', { nonNullable: true }),
    season: new FormControl('', { nonNullable: true }),
  });
  guidance(): string[] { const { environment, season } = this.form.getRawValue(); return seasonalCareGuidance(environment, season); }

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => { this.message.set(''); this.error.set(''); });
    effect(() => {
      const plant = this.plant();
      this.form.reset({ environment: plant.growingEnvironment ?? '', season: plant.careSeason ?? '' }, { emitEvent: false });
      if (this.plantId !== plant.id) { this.message.set(''); this.error.set(''); this.plantId = plant.id; }
    });
  }
  clearPreferences(): void { this.form.reset(); this.message.set(''); this.error.set(''); }
  save(): void {
    if (this.form.invalid || this.saving() || !this.plant().isActive) return;
    const { environment, season } = this.form.getRawValue();
    this.saving.set(true); this.message.set(''); this.error.set(''); this.form.disable({ emitEvent: false });
    this.api.updateGrowingContext(this.plant().id, {
      growingEnvironment: (environment || null) as UserPlant['growingEnvironment'], careSeason: (season || null) as UserPlant['careSeason'],
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => { this.saving.set(false); this.form.enable({ emitEvent: false }); })).subscribe({
      next: plant => { this.saved.emit(plant); this.message.set('Seasonal care preferences saved.'); },
      error: response => this.error.set(response.status === 404
        ? 'This plant is unavailable or archived. Reload its details before trying again.'
        : response.status === 400 ? 'Choose one of the listed environments and seasons.'
        : 'Your preferences could not be saved. Your choices are still here; please try again.'),
    });
  }
}

export function seasonalCareGuidance(environment: string, season: string): string[] {
  const environments: Record<string, string> = {
    indoor: 'Check indoor light, heating and soil moisture; conditions can differ between rooms.',
    balcony: 'Check container moisture, wind exposure and shelter. Inspect forecast frost risk before moving tender plants.',
    greenhouse: 'Check ventilation, temperature and container moisture; protected growing spaces can warm up quickly.',
    outdoor: 'Check rainfall, drainage and soil moisture before watering. Match care to the plant and local conditions.',
  };
  const seasons: Record<string, string> = {
    spring: 'Watch for renewed growth and review care gradually as light and temperatures change.',
    summer: 'Warm conditions and active growth can increase water needs, especially in containers. Check before watering.',
    autumn: 'Shorter days and cooling conditions can slow growth. Review moisture and feeding rather than relying on summer habits.',
    winter: 'Many plants grow more slowly in low light. Check moisture before watering and consider heating, frost exposure and species-specific needs.',
  };
  return [environments[environment], seasons[season]].filter((text): text is string => Boolean(text));
}
