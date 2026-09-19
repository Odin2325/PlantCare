import {
  DatePipe,
} from '@angular/common';
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
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../../core/auth/services/auth.service';
import { MyPlantsApiService } from '../../data-access/my-plants-api.service';
import {
  CareActionType,
  CareSchedule,
  UserPlant,
} from '../../models/user-plant.model';

type CareScheduleStatus =
  | 'not-started'
  | 'upcoming'
  | 'due-today'
  | 'overdue';

@Component({
  selector: 'app-my-plants-page',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
  ],
  templateUrl: './my-plants-page.html',
  styleUrl: './my-plants-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPlantsPage {
  readonly optionalCareActionTypes: CareActionType[] = [
    'Fertilizing',
    'Misting',
    'Pruning',
    'Repotting',
  ];

  private readonly myPlantsApi =
    inject(MyPlantsApiService);

  private readonly destroyRef =
    inject(DestroyRef);

  readonly authService =
    inject(AuthService);

  readonly userPlants =
    signal<UserPlant[]>([]);

  readonly archivedPlants =
    signal<UserPlant[]>([]);

  readonly showArchived = signal(false);

  readonly isLoadingArchived = signal(false);

  readonly restoringPlantId = signal<string | null>(null);

  readonly isLoading =
    signal(true);

  readonly errorMessage =
    signal<string | null>(null);

  readonly careActionError =
    signal<string | null>(null);

  readonly completingCareActionKey =
    signal<string | null>(null);

  readonly updatingScheduleKey =
    signal<string | null>(null);

  readonly intervalDrafts =
    signal<Record<string, number>>({});

  readonly changingOptionalScheduleKey =
    signal<string | null>(null);

  constructor() {
    this.loadPlants();
  }

  reload(): void {
    this.loadPlants();
  }

  toggleArchivedPlants(): void {
    if (this.showArchived()) {
      this.showArchived.set(false);
      return;
    }

    this.showArchived.set(true);
    this.loadArchivedPlants();
  }

  restorePlant(plant: UserPlant): void {
    if (this.restoringPlantId()) {
      return;
    }

    this.restoringPlantId.set(plant.id);
    this.errorMessage.set(null);

    this.myPlantsApi
      .restore(plant.id)
      .pipe(
        finalize(() => this.restoringPlantId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.archivedPlants.update((plants) =>
            plants.filter((item) => item.id !== plant.id),
          );
          this.userPlants.update((plants) =>
            [...plants, { ...plant, isActive: true }]
              .sort((left, right) =>
                left.nickname.localeCompare(right.nickname),
              ),
          );
        },
        error: () => {
          this.errorMessage.set(
            'The plant could not be restored.',
          );
        },
      });
  }

  completeCareAction(
    plant: UserPlant,
    schedule: CareSchedule,
  ): void {
    if (
      !schedule.isEnabled ||
      this.isCompletingCareAction(
        plant.id,
        schedule.actionType,
      )
    ) {
      return;
    }

    const key = this.createCareActionKey(
      plant.id,
      schedule.actionType,
    );

    this.completingCareActionKey.set(key);
    this.careActionError.set(null);

    this.myPlantsApi
      .completeCareAction(
        plant.id,
        schedule.actionType,
      )
      .pipe(
        finalize(() => {
          this.completingCareActionKey.set(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.userPlants.update(
            (plants) =>
              plants.map((currentPlant) => {
                if (currentPlant.id !== plant.id) {
                  return currentPlant;
                }

                return {
                  ...currentPlant,

                  careSchedules:
                    currentPlant.careSchedules.map(
                      (currentSchedule) =>
                        currentSchedule.id ===
                          result.schedule.id
                          ? result.schedule
                          : currentSchedule,
                    ),
                };
              }),
          );
        },

        error: (error: HttpErrorResponse) => {
          console.error(
            'Unable to complete care action.',
            error,
          );

          if (error.status === 401) {
            this.careActionError.set(
              'Your login session has expired. Please log in again.',
            );

            return;
          }

          if (error.status === 404) {
            this.careActionError.set(
              'The care schedule could not be found.',
            );

            return;
          }

          this.careActionError.set(
            'The care action could not be saved.',
          );
        },
      });
  }

  isCompletingCareAction(
    plantId: string,
    actionType: CareActionType,
  ): boolean {
    return (
      this.completingCareActionKey() ===
      this.createCareActionKey(
        plantId,
        actionType,
      )
    );
  }

  getIntervalDraft(
    plant: UserPlant,
    schedule: CareSchedule,
  ): number {
    return this.intervalDrafts()[
      this.createCareActionKey(
        plant.id,
        schedule.actionType,
      )
    ] ?? schedule.intervalDays;
  }

  setIntervalDraft(
    plant: UserPlant,
    schedule: CareSchedule,
    event: Event,
  ): void {
    const intervalDays = Number(
      (event.target as HTMLInputElement).value,
    );
    const key = this.createCareActionKey(
      plant.id,
      schedule.actionType,
    );

    this.intervalDrafts.update((drafts) => ({
      ...drafts,
      [key]: intervalDays,
    }));
  }

  saveSchedule(
    plant: UserPlant,
    schedule: CareSchedule,
  ): void {
    this.updateSchedule(
      plant,
      schedule,
      this.getIntervalDraft(plant, schedule),
      schedule.isEnabled,
    );
  }

  toggleSchedule(
    plant: UserPlant,
    schedule: CareSchedule,
  ): void {
    this.updateSchedule(
      plant,
      schedule,
      this.getIntervalDraft(plant, schedule),
      !schedule.isEnabled,
    );
  }

  isUpdatingSchedule(
    plantId: string,
    actionType: CareActionType,
  ): boolean {
    return this.updatingScheduleKey() ===
      this.createCareActionKey(plantId, actionType);
  }

  getAvailableCareActionTypes(
    plant: UserPlant,
  ): CareActionType[] {
    const existingTypes = new Set(
      plant.careSchedules.map(
        (schedule) => schedule.actionType,
      ),
    );

    return this.optionalCareActionTypes.filter(
      (actionType) => !existingTypes.has(actionType),
    );
  }

  addOptionalSchedule(
    plant: UserPlant,
    actionTypeValue: string,
    intervalValue: string,
  ): void {
    const actionType = actionTypeValue as CareActionType;
    const intervalDays = Number(intervalValue);

    if (
      !this.optionalCareActionTypes.includes(actionType) ||
      !Number.isInteger(intervalDays) ||
      intervalDays < 1 ||
      intervalDays > 3650
    ) {
      this.careActionError.set(
        'Choose a care type and an interval between 1 and 3650 days.',
      );
      return;
    }

    const key = `${plant.id}:add`;
    this.changingOptionalScheduleKey.set(key);
    this.careActionError.set(null);

    this.myPlantsApi
      .addCareSchedule(
        plant.id,
        actionType,
        intervalDays,
      )
      .pipe(
        finalize(() => {
          this.changingOptionalScheduleKey.set(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (schedule) => {
          this.userPlants.update((plants) =>
            plants.map((currentPlant) =>
              currentPlant.id === plant.id
                ? {
                    ...currentPlant,
                    careSchedules: [
                      ...currentPlant.careSchedules,
                      schedule,
                    ].sort(
                      (left, right) =>
                        left.actionType.localeCompare(
                          right.actionType,
                        ),
                    ),
                  }
                : currentPlant,
            ),
          );
        },
        error: () => {
          this.careActionError.set(
            'The care schedule could not be added.',
          );
        },
      });
  }

  removeOptionalSchedule(
    plant: UserPlant,
    schedule: CareSchedule,
  ): void {
    const key = this.createCareActionKey(
      plant.id,
      schedule.actionType,
    );
    this.changingOptionalScheduleKey.set(key);
    this.careActionError.set(null);

    this.myPlantsApi
      .archiveCareSchedule(
        plant.id,
        schedule.actionType,
      )
      .pipe(
        finalize(() => {
          this.changingOptionalScheduleKey.set(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.userPlants.update((plants) =>
            plants.map((currentPlant) =>
              currentPlant.id === plant.id
                ? {
                    ...currentPlant,
                    careSchedules:
                      currentPlant.careSchedules.filter(
                        (currentSchedule) =>
                          currentSchedule.id !== schedule.id,
                      ),
                  }
                : currentPlant,
            ),
          );
        },
        error: () => {
          this.careActionError.set(
            'The care schedule could not be removed.',
          );
        },
      });
  }

  isChangingOptionalSchedule(key: string): boolean {
    return this.changingOptionalScheduleKey() === key;
  }

  getCareStatus(
    schedule: CareSchedule,
  ): CareScheduleStatus {
    if (!schedule.isEnabled || !schedule.nextDueAtUtc) {
      return 'not-started';
    }

    const dueDate =
      new Date(schedule.nextDueAtUtc);

    if (Number.isNaN(dueDate.getTime())) {
      return 'not-started';
    }

    const today =
      this.getLocalCalendarDayNumber(
        new Date(),
      );

    const dueDay =
      this.getLocalCalendarDayNumber(
        dueDate,
      );

    if (dueDay < today) {
      return 'overdue';
    }

    if (dueDay === today) {
      return 'due-today';
    }

    return 'upcoming';
  }

  getCareStatusText(
    schedule: CareSchedule,
  ): string {
    if (!schedule.isEnabled) {
      return 'Paused';
    }

    if (!schedule.nextDueAtUtc) {
      return 'Not started';
    }

    const dueDate =
      new Date(schedule.nextDueAtUtc);

    if (Number.isNaN(dueDate.getTime())) {
      return 'Unknown';
    }

    const today =
      this.getLocalCalendarDayNumber(
        new Date(),
      );

    const dueDay =
      this.getLocalCalendarDayNumber(
        dueDate,
      );

    const difference =
      dueDay - today;

    if (difference < 0) {
      const overdueDays =
        Math.abs(difference);

      return overdueDays === 1
        ? '1 day overdue'
        : `${overdueDays} days overdue`;
    }

    if (difference === 0) {
      return 'Due today';
    }

    if (difference === 1) {
      return 'Due tomorrow';
    }

    return `Due in ${difference} days`;
  }

  getCareActionName(
    actionType: CareActionType,
  ): string {
    return actionType.replace(
      /([a-z])([A-Z])/g,
      '$1 $2',
    );
  }

  getCompletionButtonText(
    actionType: CareActionType,
  ): string {
    switch (actionType) {
      case 'Watering':
        return 'Watered today';

      case 'Fertilizing':
        return 'Fertilized today';

      case 'Misting':
        return 'Misted today';

      case 'Pruning':
        return 'Pruned today';

      case 'Repotting':
        return 'Repotted today';

      default:
        return 'Completed today';
    }
  }

  private loadPlants(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.myPlantsApi
      .getAll()
      .pipe(
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: (plants) => {
          this.userPlants.set(plants);
          this.intervalDrafts.set(
            Object.fromEntries(
              plants.flatMap((plant) =>
                plant.careSchedules.map((schedule) => [
                  this.createCareActionKey(
                    plant.id,
                    schedule.actionType,
                  ),
                  schedule.intervalDays,
                ]),
              ),
            ),
          );
          this.isLoading.set(false);
        },

        error: (error: HttpErrorResponse) => {
          console.error(
            'Unable to load My Plants.',
            error,
          );

          this.errorMessage.set(
            'Your plants could not be loaded.',
          );

          this.isLoading.set(false);
        },
      });
  }

  private loadArchivedPlants(): void {
    this.isLoadingArchived.set(true);

    this.myPlantsApi
      .getArchived()
      .pipe(
        finalize(() => this.isLoadingArchived.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (plants) => this.archivedPlants.set(plants),
        error: () => {
          this.errorMessage.set(
            'Archived plants could not be loaded.',
          );
        },
      });
  }

  private createCareActionKey(
    plantId: string,
    actionType: CareActionType,
  ): string {
    return `${plantId}:${actionType}`;
  }

  private updateSchedule(
    plant: UserPlant,
    schedule: CareSchedule,
    intervalDays: number,
    isEnabled: boolean,
  ): void {
    if (
      !Number.isInteger(intervalDays) ||
      intervalDays < 1 ||
      intervalDays > 3650 ||
      this.isUpdatingSchedule(
        plant.id,
        schedule.actionType,
      )
    ) {
      this.careActionError.set(
        'Enter an interval between 1 and 3650 days.',
      );
      return;
    }

    const key = this.createCareActionKey(
      plant.id,
      schedule.actionType,
    );
    this.updatingScheduleKey.set(key);
    this.careActionError.set(null);

    this.myPlantsApi
      .updateCareSchedule(
        plant.id,
        schedule.actionType,
        {
          intervalDays,
          isEnabled,
        },
      )
      .pipe(
        finalize(() => {
          this.updatingScheduleKey.set(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updatedSchedule) => {
          this.userPlants.update((plants) =>
            plants.map((currentPlant) =>
              currentPlant.id === plant.id
                ? {
                    ...currentPlant,
                    careSchedules:
                      currentPlant.careSchedules.map(
                        (currentSchedule) =>
                          currentSchedule.id ===
                            updatedSchedule.id
                            ? updatedSchedule
                            : currentSchedule,
                      ),
                  }
                : currentPlant,
            ),
          );
          this.intervalDrafts.update((drafts) => ({
            ...drafts,
            [key]: updatedSchedule.intervalDays,
          }));
        },
        error: (error: HttpErrorResponse) => {
          console.error(
            'Unable to update care schedule.',
            error,
          );
          this.careActionError.set(
            error.status === 404
              ? 'The care schedule could not be found.'
              : 'The care schedule could not be updated.',
          );
        },
      });
  }

  private getLocalCalendarDayNumber(
    date: Date,
  ): number {
    return Math.floor(
      Date.UTC(
        date.getFullYear(),
        date.getMonth(),
        date.getDate(),
      ) /
      86_400_000,
    );
  }

  archivePlant(plant: UserPlant): void {
    this.myPlantsApi
      .archive(plant.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.userPlants.update(
            (plants) => plants.filter((p) => p.id !== plant.id),
          );
          if (this.showArchived()) {
            this.archivedPlants.update((plants) =>
              [...plants, { ...plant, isActive: false }]
                .sort((left, right) =>
                  left.nickname.localeCompare(right.nickname),
                ),
            );
          }
        },

        error: (error: HttpErrorResponse) => {
          console.error('Unable to archive plant.', error);

          this.errorMessage.set(
            'The plant could not be removed.',
          );
        },
      });
  }
}
