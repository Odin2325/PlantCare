import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { MyPlantsApiService } from '../../features/my-plants/data-access/my-plants-api.service';
import { UserPlant } from '../../features/my-plants/models/user-plant.model';

@Component({
  selector: 'app-plant-growing-plan', standalone: true, imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './plant-growing-plan.html', styleUrl: './plant-growing-plan.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantGrowingPlan {
  private readonly api = inject(MyPlantsApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly plant = input.required<UserPlant>();
  readonly saved = output<UserPlant>();
  readonly saving = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly form = new FormGroup({
    sowing: new FormControl('', { nonNullable: true }),
    plantingOut: new FormControl('', { nonNullable: true }),
    harvest: new FormControl('', { nonNullable: true }),
  }, { validators: group => {
    const { sowing, plantingOut, harvest } = group.getRawValue();
    return (sowing && plantingOut && plantingOut < sowing) ||
      (sowing && harvest && harvest < sowing) ||
      (plantingOut && harvest && harvest < plantingOut) ? { dateOrder: true } : null;
  } });

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => { this.message.set(''); this.error.set(''); });
    effect(() => {
      const plant = this.plant();
      this.form.reset({ sowing: plant.plannedSowingOn ?? '', plantingOut: plant.plannedPlantingOutOn ?? '', harvest: plant.expectedHarvestOn ?? '' }, { emitEvent: false });
    });
  }
  clearDates(): void { this.form.reset(); this.message.set(''); this.error.set(''); }
  save(): void {
    if (this.form.invalid || this.saving() || !this.plant().isActive) return;
    const { sowing, plantingOut, harvest } = this.form.getRawValue();
    this.saving.set(true); this.message.set(''); this.error.set(''); this.form.disable({ emitEvent: false });
    this.api.updateGrowingPlan(this.plant().id, {
      plannedSowingOn: sowing || null, plannedPlantingOutOn: plantingOut || null, expectedHarvestOn: harvest || null,
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => { this.saving.set(false); this.form.enable({ emitEvent: false }); })).subscribe({
      next: plant => { this.saved.emit(plant); this.message.set('Planting plan saved.'); },
      error: response => this.error.set(response.status === 404
        ? 'This plant is unavailable or archived. Reload its details before trying again.'
        : response.status === 400 ? 'Check that your dates follow sowing, planting out and harvest order.'
        : 'Your planting plan could not be saved. Your entered dates are still here; please try again.'),
    });
  }
}
