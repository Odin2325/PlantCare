import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';

import {
  takeUntilDestroyed,
} from '@angular/core/rxjs-interop';

import {
  finalize,
} from 'rxjs';

import {
  DashboardApiService,
} from '../../data-access/dashboard-api.service';

import {
  CareDue,
} from '../../models/dashboard.model';

import {
  MyPlantsApiService,
} from '../../../my-plants/data-access/my-plants-api.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.css',
  changeDetection:
    ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  private readonly dashboardApi =
    inject(DashboardApiService);

  private readonly myPlantsApi =
    inject(MyPlantsApiService);

  private readonly destroyRef =
    inject(DestroyRef);

  readonly careDue =
    signal<CareDue[]>([]);

  readonly isLoading =
    signal(true);

  readonly errorMessage =
    signal<string | null>(null);

  readonly completingCare =
    signal<string | null>(null);

  constructor() {
    this.loadDashboard();
  }

  readonly overdue =
    computed(() =>
      this.careDue().filter(
        item =>
          item.status === 'Overdue',
      ),
    );

  readonly dueToday =
    computed(() =>
      this.careDue().filter(
        item =>
          item.status === 'DueToday',
      ),
    );

  readonly upcoming =
    computed(() =>
      this.careDue().filter(
        item =>
          item.status === 'Upcoming',
      ),
    );

  loadDashboard(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dashboardApi
      .getCareDue(7)
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
        }),
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: careDue => {
          this.careDue.set(careDue);
        },

        error: error => {
          console.error(
            'Unable to load dashboard.',
            error,
          );

          this.errorMessage.set(
            'The dashboard could not be loaded.',
          );
        },
      });
  }

  completeCare(item: CareDue): void {
    const key = this.getCareKey(item);

    if (this.completingCare() !== null) {
      return;
    }

    this.completingCare.set(key);

    this.myPlantsApi
      .completeCareAction(
        item.userPlantId,
        item.actionType,
      )
      .pipe(
        finalize(() => {
          this.completingCare.set(null);
        }),
        takeUntilDestroyed(
          this.destroyRef,
        ),
      )
      .subscribe({
        next: () => {
          this.loadDashboard();
        },

        error: error => {
          console.error(
            'Unable to complete care action.',
            error,
          );

          this.errorMessage.set(
            'The care action could not be completed.',
          );
        },
      });
  }

  getCareKey(item: CareDue): string {
    return `${item.userPlantId}-${item.actionType}`;
  }

  getActionLabel(
    actionType: string,
  ): string {
    switch (actionType) {
      case 'Watering':
        return 'Water';

      case 'Fertilizing':
        return 'Fertilize';

      default:
        return actionType;
    }
  }

  getDueLabel(
    item: CareDue,
  ): string {
    const dueDate =
      new Date(item.dueAtUtc);

    return dueDate.toLocaleDateString(
      undefined,
      {
        month: 'short',
        day: 'numeric',
      },
    );
  }
}
