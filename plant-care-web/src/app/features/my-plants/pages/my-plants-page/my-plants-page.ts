import {
  DatePipe,
} from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
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

type CollectionSort = 'nickname' | 'newest' | 'acquired' | 'next-care';

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

  readonly searchTerm = signal('');
  readonly locationFilter = signal('');
  readonly tagFilter = signal('');
  readonly sortBy = signal<CollectionSort>('nickname');

  readonly locations = computed(() =>
    [...new Set(this.userPlants()
      .map((plant) => plant.location)
      .filter((location): location is string => Boolean(location)))]
      .sort((left, right) => left.localeCompare(right)),
  );

  readonly availableTags = computed(() =>
    [...new Set(this.userPlants().flatMap((plant) => plant.tags))]
      .sort((left, right) => left.localeCompare(right)),
  );

  readonly filteredPlants = computed(() => {
    const query = this.searchTerm().trim().toLocaleLowerCase();
    const location = this.locationFilter();
    const tag = this.tagFilter();
    const plants = this.userPlants().filter((plant) => {
      const searchable = [
        plant.nickname,
        plant.speciesCommonName,
        plant.speciesScientificName ?? '',
        plant.location ?? '',
        plant.notes ?? '',
        ...plant.tags,
      ].join(' ').toLocaleLowerCase();

      return (!query || searchable.includes(query)) &&
        (!location || plant.location === location) &&
        (!tag || plant.tags.includes(tag));
    });

    return [...plants].sort((left, right) => {
      switch (this.sortBy()) {
        case 'newest':
          return right.createdAtUtc.localeCompare(left.createdAtUtc);
        case 'acquired':
          return (right.acquiredOn ?? '').localeCompare(left.acquiredOn ?? '');
        case 'next-care':
          return this.getNextCareTime(left) - this.getNextCareTime(right);
        default:
          return left.nickname.localeCompare(right.nickname);
      }
    });
  });

  readonly tagDrafts = signal<Record<string, string>>({});
  readonly savingTagsPlantId = signal<string | null>(null);

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

  readonly scheduleModeDrafts =
    signal<Record<string, 'Interval' | 'Weekdays'>>({});

  readonly weekDaysDrafts =
    signal<Record<string, number>>({});

  readonly preferredTimeDrafts =
    signal<Record<string, string>>({});

  readonly weekDayOptions = [
    { label: 'Mon', value: 1 },
    { label: 'Tue', value: 2 },
    { label: 'Wed', value: 4 },
    { label: 'Thu', value: 8 },
    { label: 'Fri', value: 16 },
    { label: 'Sat', value: 32 },
    { label: 'Sun', value: 64 },
  ];

  readonly changingOptionalScheduleKey =
    signal<string | null>(null);

  constructor() {
    this.loadPlants();
  }

  setSearchTerm(value: string): void { this.searchTerm.set(value); }
  setLocationFilter(value: string): void { this.locationFilter.set(value); }
  setTagFilter(value: string): void { this.tagFilter.set(value); }
  setSortBy(value: string): void { this.sortBy.set(value as CollectionSort); }

  getTagDraft(plant: UserPlant): string {
    return this.tagDrafts()[plant.id] ?? plant.tags.join(', ');
  }

  setTagDraft(plantId: string, value: string): void {
    this.tagDrafts.update((drafts) => ({ ...drafts, [plantId]: value }));
  }

  saveTags(plant: UserPlant): void {
    const tags = [...new Set(this.getTagDraft(plant).split(',')
      .map((tag) => tag.trim()).filter(Boolean))];
    if (tags.length > 10 || tags.some((tag) => tag.length > 32)) {
      this.errorMessage.set('Use no more than 10 tags, with 32 characters per tag.');
      return;
    }

    this.savingTagsPlantId.set(plant.id);
    this.errorMessage.set(null);
    this.myPlantsApi.update(plant.id, {
      nickname: plant.nickname,
      location: plant.location,
      acquiredOn: plant.acquiredOn,
      notes: plant.notes,
      tags,
    }).pipe(
      finalize(() => this.savingTagsPlantId.set(null)),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (updated) => {
        this.userPlants.update((plants) => plants.map((item) =>
          item.id === updated.id ? updated : item));
        this.tagDrafts.update((drafts) => ({ ...drafts, [plant.id]: updated.tags.join(', ') }));
      },
      error: () => this.errorMessage.set('The plant tags could not be saved.'),
    });
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

  getScheduleModeDraft(plant: UserPlant, schedule: CareSchedule): 'Interval' | 'Weekdays' {
    return this.scheduleModeDrafts()[this.createCareActionKey(plant.id, schedule.actionType)] ?? schedule.scheduleMode;
  }

  setScheduleModeDraft(plant: UserPlant, schedule: CareSchedule, value: string): void {
    const key = this.createCareActionKey(plant.id, schedule.actionType);
    this.scheduleModeDrafts.update(drafts => ({ ...drafts, [key]: value as 'Interval' | 'Weekdays' }));
  }

  getWeekDaysDraft(plant: UserPlant, schedule: CareSchedule): number {
    return this.weekDaysDrafts()[this.createCareActionKey(plant.id, schedule.actionType)] ?? schedule.weekDays;
  }

  toggleWeekDayDraft(plant: UserPlant, schedule: CareSchedule, value: number): void {
    const key = this.createCareActionKey(plant.id, schedule.actionType);
    const current = this.getWeekDaysDraft(plant, schedule);
    this.weekDaysDrafts.update(drafts => ({ ...drafts, [key]: current ^ value }));
  }

  isWeekDaySelected(plant: UserPlant, schedule: CareSchedule, value: number): boolean {
    return (this.getWeekDaysDraft(plant, schedule) & value) !== 0;
  }

  getPreferredTimeDraft(plant: UserPlant, schedule: CareSchedule): string {
    return this.preferredTimeDrafts()[this.createCareActionKey(plant.id, schedule.actionType)] ?? schedule.preferredTimeLocal?.slice(0, 5) ?? '09:00';
  }

  setPreferredTimeDraft(plant: UserPlant, schedule: CareSchedule, value: string): void {
    const key = this.createCareActionKey(plant.id, schedule.actionType);
    this.preferredTimeDrafts.update(drafts => ({ ...drafts, [key]: value }));
  }

  getScheduleRecurrenceText(schedule: CareSchedule): string {
    if (schedule.scheduleMode === 'Interval') return `Every ${schedule.intervalDays} days`;
    const days = this.weekDayOptions.filter(day => (schedule.weekDays & day.value) !== 0).map(day => day.label).join(', ');
    return `${days} at ${schedule.preferredTimeLocal?.slice(0, 5) ?? ''}`;
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
      this.getScheduleModeDraft(plant, schedule),
      this.getWeekDaysDraft(plant, schedule),
      this.getPreferredTimeDraft(plant, schedule),
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
      this.getScheduleModeDraft(plant, schedule),
      this.getWeekDaysDraft(plant, schedule),
      this.getPreferredTimeDraft(plant, schedule),
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
          this.scheduleModeDrafts.set(Object.fromEntries(plants.flatMap(plant => plant.careSchedules.map(schedule => [this.createCareActionKey(plant.id, schedule.actionType), schedule.scheduleMode]))));
          this.weekDaysDrafts.set(Object.fromEntries(plants.flatMap(plant => plant.careSchedules.map(schedule => [this.createCareActionKey(plant.id, schedule.actionType), schedule.weekDays]))));
          this.preferredTimeDrafts.set(Object.fromEntries(plants.flatMap(plant => plant.careSchedules.map(schedule => [this.createCareActionKey(plant.id, schedule.actionType), schedule.preferredTimeLocal?.slice(0, 5) ?? '09:00']))));
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

  private getNextCareTime(plant: UserPlant): number {
    const dates = plant.careSchedules
      .filter((schedule) => schedule.isEnabled && schedule.nextDueAtUtc)
      .map((schedule) => new Date(schedule.nextDueAtUtc!).getTime());
    return dates.length ? Math.min(...dates) : Number.MAX_SAFE_INTEGER;
  }

  private updateSchedule(
    plant: UserPlant,
    schedule: CareSchedule,
    intervalDays: number,
    isEnabled: boolean,
    scheduleMode: 'Interval' | 'Weekdays',
    weekDays: number,
    preferredTimeLocal: string,
  ): void {
    if (
      !Number.isInteger(intervalDays) ||
      intervalDays < 1 ||
      intervalDays > 3650 ||
      (scheduleMode === 'Weekdays' && (weekDays === 0 || !preferredTimeLocal)) ||
      this.isUpdatingSchedule(
        plant.id,
        schedule.actionType,
      )
    ) {
      this.careActionError.set(
        scheduleMode === 'Weekdays'
          ? 'Choose at least one weekday and a reminder time.'
          : 'Enter an interval between 1 and 3650 days.',
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
          scheduleMode,
          weekDays,
          preferredTimeLocal: scheduleMode === 'Weekdays' ? preferredTimeLocal : null,
          timeZoneId: scheduleMode === 'Weekdays' ? Intl.DateTimeFormat().resolvedOptions().timeZone : null,
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
          this.scheduleModeDrafts.update(drafts => ({ ...drafts, [key]: updatedSchedule.scheduleMode }));
          this.weekDaysDrafts.update(drafts => ({ ...drafts, [key]: updatedSchedule.weekDays }));
          this.preferredTimeDrafts.update(drafts => ({ ...drafts, [key]: updatedSchedule.preferredTimeLocal?.slice(0, 5) ?? '09:00' }));
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
