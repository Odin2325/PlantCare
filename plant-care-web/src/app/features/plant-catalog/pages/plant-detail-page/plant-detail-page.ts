import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  takeUntilDestroyed,
} from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  ActivatedRoute,
  Router,
  RouterLink,
} from '@angular/router';
import {
  finalize,
  map,
  switchMap,
} from 'rxjs';

import { AuthService } from '../../../../core/auth/services/auth.service';
import { PlantSpecies } from '../../../../models/plant-species.model';
import { PlantSpeciesApiService } from '../../../../core/services/plant-species-api.service';
import { MyPlantsApiService } from '../../../my-plants/data-access/my-plants-api.service';

@Component({
  selector: 'app-plant-detail-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './plant-detail-page.html',
  styleUrl: './plant-detail-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly plantSpeciesApi =
    inject(PlantSpeciesApiService);
  private readonly myPlantsApi =
    inject(MyPlantsApiService);

  readonly authService = inject(AuthService);

  readonly plantSpecies =
    signal<PlantSpecies | null>(null);

  readonly isLoading = signal(true);

  readonly loadError =
    signal<string | null>(null);

  readonly isSubmitting = signal(false);

  readonly submitError =
    signal<string | null>(null);

  readonly addPlantForm = new FormGroup({
    nickname: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(100),
      ],
    }),
    location: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.maxLength(150),
      ],
    }),
    acquiredOn: new FormControl('', {
      nonNullable: true,
    }),
    notes: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.maxLength(2000),
      ],
    }),
  });

  constructor() {
    this.loadPlantSpecies();
  }

  addToMyPlants(): void {
    const plantSpecies = this.plantSpecies();

    if (!plantSpecies) {
      return;
    }

    if (!this.authService.isAuthenticated()) {
      void this.router.navigate(
        ['/login'],
        {
          queryParams: {
            returnUrl: this.router.url,
          },
        },
      );

      return;
    }

    if (
      this.addPlantForm.invalid ||
      this.isSubmitting()
    ) {
      this.addPlantForm.markAllAsTouched();
      return;
    }

    const formValue =
      this.addPlantForm.getRawValue();

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.myPlantsApi
      .add({
        plantSpeciesId: plantSpecies.id,
        nickname: formValue.nickname,
        location:
          formValue.location.trim() || null,
        acquiredOn:
          formValue.acquiredOn || null,
        notes:
          formValue.notes.trim() || null,
      })
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigate([
            '/my-plants',
          ]);
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Unable to add plant.',
            error,
          );

          if (error.status === 404) {
            this.submitError.set(
              'This plant species no longer exists.',
            );

            return;
          }

          if (error.status === 401) {
            this.submitError.set(
              'Your login session has expired. Please log in again.',
            );

            return;
          }

          if (error.status === 400) {
            this.submitError.set(
              'The plant information is invalid. Please check the form.',
            );

            return;
          }

          this.submitError.set(
            'The plant could not be added.',
          );
        },
      });
  }

  formatEnumName(value: string): string {
    return value.replace(
      /([a-z])([A-Z])/g,
      '$1 $2',
    );
  }

  private loadPlantSpecies(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.route.paramMap
      .pipe(
        map((parameters) =>
          parameters.get('id'),
        ),
        switchMap((id) => {
          if (!id) {
            throw new Error(
              'Plant species ID is missing.',
            );
          }

          return this.plantSpeciesApi
            .getById(id);
        }),
        takeUntilDestroyed(),
      )
      .subscribe({
        next: (plantSpecies) => {
          this.plantSpecies.set(
            plantSpecies,
          );

          this.addPlantForm.controls.nickname
            .setValue(
              plantSpecies.commonName,
            );

          this.isLoading.set(false);
        },
        error: (error: unknown) => {
          console.error(
            'Unable to load plant species.',
            error,
          );

          this.loadError.set(
            'The requested plant could not be found.',
          );

          this.isLoading.set(false);
        },
      });
  }
}
