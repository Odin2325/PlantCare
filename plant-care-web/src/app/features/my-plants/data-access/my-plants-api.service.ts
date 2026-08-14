import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddUserPlantRequest,
  UserPlant,
} from '../models/user-plant.model';

@Injectable({
  providedIn: 'root',
})
export class MyPlantsApiService {
  private readonly httpClient = inject(HttpClient);

  private readonly endpoint = '/api/my-plants';

  getAll(): Observable<UserPlant[]> {
    return this.httpClient.get<UserPlant[]>(
      this.endpoint,
    );
  }

  getById(id: string): Observable<UserPlant> {
    return this.httpClient.get<UserPlant>(
      `${this.endpoint}/${id}`,
    );
  }

  add(
    request: AddUserPlantRequest,
  ): Observable<UserPlant> {
    return this.httpClient.post<UserPlant>(
      this.endpoint,
      request,
    );
  }
}
