import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
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
  RouterLink,
} from '@angular/router';
import {
  finalize,
  forkJoin,
} from 'rxjs';

import { MyPlantsApiService } from '../../data-access/my-plants-api.service';
import {
  CareActionType,
  CareEventHistory,
  UserPlant,
} from '../../models/user-plant.model';

@Component({
  selector: 'app-care-history-page',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './care-history-page.html',
  styleUrl: './care-history-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CareHistoryPage {
  private readonly route =
    inject(ActivatedRoute);

  private readonly myPlantsApi =
    inject(MyPlantsApiService);

  private readonly destroyRef =
    inject(DestroyRef);

  readonly userPlant =
    signal<UserPlant | null>(null);

  readonly history =
    signal<CareEventHistory[]>([]);

  readonly isLoading =
    signal(true);

  readonly isSubmitting =
    signal(false);

  readonly errorMessage =
    signal<string | null>(null);

  readonly submitError =
    signal<string | null>(null);

  readonly form = new FormGroup({
    actionType: new FormControl<CareActionType>(
      'Watering',
      {
        nonNullable: true,
        validators: [
          Validators.required,
        ],
      },
    ),

    completedAtLocal: new FormControl(
      '',
      {
        nonNullable: true,
      },
    ),

    notes: new FormControl(
      '',
      {
        nonNullable: true,
        validators: [
          Validators.maxLength(1000),
        ],
      },
    ),
  });

  constructor() {
    this.loadData();
  }

  submit(): void {
    const plant = this.userPlant();

    if (
      !plant ||
      this.form.invalid ||
      this.isSubmitting()
    ) {
      this.form.markAllAsTouched();
      return;
    }

    const value =
      this.form.getRawValue();

    let completedAtUtc: string | null = null;

    if (value.completedAtLocal) {
      const completedDate =
        new Date(value.completedAtLocal);

      if (
        Number.isNaN(
          completedDate.getTime(),
        )
      ) {
        this.submitError.set(
          'The completion date is invalid.',
        );

        return;
      }

      completedAtUtc =
        completedDate.toISOString();
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.myPlantsApi
      .completeCareAction(
        plant.id,
        value.actionType,
        {
          completedAtUtc,
          notes:
            value.notes.trim() || null,
        },
      )
      .pipe(
        finalize(() => {
          this.isSubmitting.set(false);
        }),
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: () => {
          this.form.controls.completedAtLocal
            .setValue('');

          this.form.controls.notes
            .setValue('');

          this.loadData();
        },

        error: (
          error: HttpErrorResponse,
        ) => {
          console.error(
            'Unable to record care.',
            error,
          );

          if (error.status === 400) {
            const detail =
              error.error?.detail;

            this.submitError.set(
              typeof detail === 'string'
                ? detail
                : 'The care entry is invalid.',
            );

            return;
          }

          this.submitError.set(
            'The care entry could not be saved.',
          );
        },
      });
  }

  getCareActionName(
    actionType: CareActionType,
  ): string {
    return actionType.replace(
      /([a-z])([A-Z])/g,
      '$1 $2',
    );
  }

  private loadData(): void {
    const plantId =
      this.route.snapshot.paramMap.get('id');

    if (!plantId) {
      this.errorMessage.set(
        'The plant ID is missing.',
      );

      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      plant:
        this.myPlantsApi.getById(
          plantId,
        ),

      history:
        this.myPlantsApi.getCareHistory(
          plantId,
        ),
    })
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: ({
          plant,
          history,
        }) => {
          this.userPlant.set(plant);
          this.history.set(history);

          const firstSchedule =
            plant.careSchedules
              .find(
                schedule =>
                  schedule.isEnabled,
              );

          if (firstSchedule) {
            this.form.controls.actionType
              .setValue(
                firstSchedule.actionType,
              );
          }

          this.isLoading.set(false);
        },

        error: (
          error: HttpErrorResponse,
        ) => {
          console.error(
            'Unable to load care history.',
            error,
          );

          this.errorMessage.set(
            'Care history could not be loaded.',
          );

          this.isLoading.set(false);
        },
      });
  }
}
