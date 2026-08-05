import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PlantSpecies } from '../../models/plant-species.model'

@Injectable({
  providedIn: 'root',
})
export class PlantSpeciesApiService {
  private readonly httpClient = inject(HttpClient);

  private readonly endpoint =
    `${environment.apiUrl}/api/plant-species`;

  getAll(): Observable<PlantSpecies[]> {
    return this.httpClient.get<PlantSpecies[]>(
      this.endpoint,
    );
  }

  getById(id: string): Observable<PlantSpecies> {
    return this.httpClient.get<PlantSpecies>(
      `${this.endpoint}/${id}`,
    );
  }
}
