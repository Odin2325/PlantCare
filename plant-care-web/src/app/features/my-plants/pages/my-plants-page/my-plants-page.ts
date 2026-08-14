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
import { RouterLink } from '@angular/router';

import { AuthService } from '../../../../core/auth/services/auth.service';
import { MyPlantsApiService } from '../../data-access/my-plants-api.service';
import { UserPlant } from '../../models/user-plant.model';

@Component({
  selector: 'app-my-plants-page',
  standalone: true,
  imports: [
    RouterLink,
  ],
  templateUrl: './my-plants-page.html',
  styleUrl: './my-plants-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPlantsPage {
  private readonly myPlantsApi =
    inject(MyPlantsApiService);

  readonly authService = inject(AuthService);

  readonly userPlants =
    signal<UserPlant[]>([]);

  readonly isLoading = signal(true);

  readonly errorMessage =
    signal<string | null>(null);

  constructor() {
    this.loadPlants();
  }

  reload(): void {
    this.loadPlants();
  }

  private loadPlants(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.myPlantsApi
      .getAll()
      .pipe(
        takeUntilDestroyed(),
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
}
