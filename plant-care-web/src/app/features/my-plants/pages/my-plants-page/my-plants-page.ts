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

  getCareStatus(
    schedule: CareSchedule,
  ): CareScheduleStatus {
    if (!schedule.nextDueAtUtc) {
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
}
