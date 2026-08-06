import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { PlantSpecies } from '../../../../models/plant-species.model';
import { PlantSpeciesApiService } from '../../../../core/services/plant-species-api.service';

@Component({
  selector: 'app-plant-catalog-page',
  standalone: true,
  imports: [],
  templateUrl: './plant-catalog-page.html',
  styleUrl: './plant-catalog-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantCatalogPage {
  private readonly plantSpeciesApi =
    inject(PlantSpeciesApiService);

  readonly plantSpecies = signal<PlantSpecies[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    this.loadPlantSpecies();
  }

  reload(): void {
    this.loadPlantSpecies();
  }

  formatEnumName(value: string): string {
    return value.replace(
      /([a-z])([A-Z])/g,
      '$1 $2',
    );
  }

  private loadPlantSpecies(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.plantSpeciesApi.getAll().subscribe({
      next: (plantSpecies) => {
        this.plantSpecies.set(plantSpecies);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        console.error('Unable to load the plant catalog.', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          url: error.url,
          error: error.error,
        });

        this.errorMessage.set(
          `The plant catalog could not be loaded. HTTP status: ${error.status}`,
        );

        this.isLoading.set(false);
      },
    });
  }
}
