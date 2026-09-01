import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddUserPlantRequest,
  CareActionType,
  CareEventHistory,
  CompleteCareActionRequest,
  CompleteCareActionResult,
  UserPlant,
} from '../models/user-plant.model';

@Injectable({
  providedIn: 'root',
})
export class MyPlantsApiService {
  private readonly httpClient = inject(HttpClient);

  private readonly endpoint = '/api/my-plants';

  getCareHistory(
    userPlantId: string,
    take = 50,
  ): Observable<CareEventHistory[]> {
    return this.httpClient.get<CareEventHistory[]>(
      `${this.endpoint}/${userPlantId}/care/history`,
      {
        params: {
          take,
        },
      },
    );
  }

  completeCareAction(
    userPlantId: string,
    actionType: CareActionType,
    request: CompleteCareActionRequest = {
      completedAtUtc: null,
      notes: null,
    },
  ): Observable<CompleteCareActionResult> {
    return this.httpClient.post<CompleteCareActionResult>(
      `${this.endpoint}/${userPlantId}/care/${actionType}/complete`,
      request,
    );
  }

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
