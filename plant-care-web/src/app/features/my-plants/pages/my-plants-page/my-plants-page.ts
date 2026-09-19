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
  private readonly myPlantsApi =
    inject(MyPlantsApiService);

  private readonly destroyRef =
    inject(DestroyRef);

  readonly authService =
    inject(AuthService);

  readonly userPlants =
    signal<UserPlant[]>([]);

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

  constructor() {
    this.loadPlants();
  }

  reload(): void {
    this.loadPlants();
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
