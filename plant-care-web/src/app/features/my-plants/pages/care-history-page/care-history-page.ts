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

  readonly editingEventId =
    signal<string | null>(null);

  readonly mutatingEventId =
    signal<string | null>(null);

  readonly editForm = new FormGroup({
    completedAtLocal: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    notes: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)],
    }),
  });

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

  beginEdit(event: CareEventHistory): void {
    this.editingEventId.set(event.id);
    this.editForm.setValue({
      completedAtLocal:
        this.toLocalDateTimeValue(event.completedAtUtc),
      notes: event.notes ?? '',
    });
    this.submitError.set(null);
  }

  cancelEdit(): void {
    this.editingEventId.set(null);
    this.editForm.reset({
      completedAtLocal: '',
      notes: '',
    });
  }

  saveEdit(event: CareEventHistory): void {
    const plant = this.userPlant();

    if (!plant || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const value = this.editForm.getRawValue();
    const completedAt = new Date(value.completedAtLocal);

    if (Number.isNaN(completedAt.getTime())) {
      this.submitError.set('The completion date is invalid.');
      return;
    }

    this.mutatingEventId.set(event.id);
    this.submitError.set(null);

    this.myPlantsApi
      .updateCareEvent(
        plant.id,
        event.id,
        {
          completedAtUtc: completedAt.toISOString(),
          notes: value.notes.trim() || null,
        },
      )
      .pipe(
        finalize(() => this.mutatingEventId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updatedEvent) => {
          this.history.update((events) =>
            events
              .map((currentEvent) =>
                currentEvent.id === updatedEvent.id
                  ? updatedEvent
                  : currentEvent,
              )
              .sort((left, right) =>
                right.completedAtUtc.localeCompare(
                  left.completedAtUtc,
                ),
              ),
          );
          this.cancelEdit();
        },
        error: (error: HttpErrorResponse) => {
          this.submitError.set(
            error.error?.detail ??
            'The care entry could not be updated.',
          );
        },
      });
  }

  deleteEvent(event: CareEventHistory): void {
    const plant = this.userPlant();

    if (
      !plant ||
      !window.confirm('Delete this care entry?')
    ) {
      return;
    }

    this.mutatingEventId.set(event.id);
    this.submitError.set(null);

    this.myPlantsApi
      .deleteCareEvent(plant.id, event.id)
      .pipe(
        finalize(() => this.mutatingEventId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.history.update((events) =>
            events.filter(
              (currentEvent) => currentEvent.id !== event.id,
            ),
          );
          if (this.editingEventId() === event.id) {
            this.cancelEdit();
          }
        },
        error: () => {
          this.submitError.set(
            'The care entry could not be deleted.',
          );
        },
      });
  }

  private toLocalDateTimeValue(value: string): string {
    const date = new Date(value);
    const pad = (part: number) =>
      part.toString().padStart(2, '0');

    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
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
