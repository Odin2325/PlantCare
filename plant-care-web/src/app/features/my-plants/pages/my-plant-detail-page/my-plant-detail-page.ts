import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { MyPlantsApiService } from '../../data-access/my-plants-api.service';
import { UserPlant } from '../../models/user-plant.model';
import { PlantGrowthReferences } from '../../../../shared/plant-growth-references/plant-growth-references';
import { PlantPhoto } from '../../../../shared/plant-photo/plant-photo';
import { HarvestLog } from '../../../../shared/harvest-log/harvest-log';
import { PlantGrowingContext } from '../../../../shared/plant-growing-context/plant-growing-context';
import { PlantGrowingPlan } from '../../../../shared/plant-growing-plan/plant-growing-plan';
import { PlantGrowingGuide } from '../../../../shared/plant-growing-guide/plant-growing-guide';

@Component({
  selector: 'app-my-plant-detail-page', standalone: true,
  imports: [DatePipe, RouterLink, PlantPhoto, PlantGrowthReferences, PlantGrowingGuide, PlantGrowingPlan, HarvestLog, PlantGrowingContext],
  templateUrl: './my-plant-detail-page.html', styleUrl: './my-plant-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPlantDetailPage {
  readonly plant = signal<UserPlant | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  constructor() {
    const id = inject(ActivatedRoute).snapshot.paramMap.get('id');
    const destroyRef = inject(DestroyRef);
    if (!id || !/^[a-f0-9-]{36}$/i.test(id)) {
      this.loading.set(false); this.error.set('This plant could not be found.'); return;
    }
    inject(MyPlantsApiService).getById(id).pipe(
      takeUntilDestroyed(destroyRef), finalize(() => this.loading.set(false)),
    ).subscribe({
      next: plant => this.plant.set(plant),
      error: error => this.error.set(error.status === 404
        ? 'This plant could not be found.' : 'Plant details could not be loaded. Please try again.'),
    });
  }
}
