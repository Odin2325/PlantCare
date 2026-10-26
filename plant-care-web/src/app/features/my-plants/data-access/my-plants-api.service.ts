import { HarvestRecord, RecordHarvest } from '../models/harvest-record.model';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddUserPlantRequest,
  GrowingPlanRequest,
  CareActionType,
  CareSchedule,
  CareEventHistory,
  CompleteCareActionRequest,
  CompleteCareActionResult,
  UpdateCareScheduleRequest,
  UpdateUserPlantRequest,
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

  updateCareEvent(
    userPlantId: string,
    careEventId: string,
    request: CompleteCareActionRequest,
  ): Observable<CareEventHistory> {
    return this.httpClient.put<CareEventHistory>(
      `${this.endpoint}/${userPlantId}/care/history/${careEventId}`,
      request,
    );
  }

  deleteCareEvent(
    userPlantId: string,
    careEventId: string,
  ): Observable<void> {
    return this.httpClient.delete<void>(
      `${this.endpoint}/${userPlantId}/care/history/${careEventId}`,
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

  updateCareSchedule(
    userPlantId: string,
    actionType: CareActionType,
    request: UpdateCareScheduleRequest,
  ): Observable<CareSchedule> {
    return this.httpClient.put<CareSchedule>(
      `${this.endpoint}/${userPlantId}/care/${actionType}/schedule`,
      request,
    );
  }

  addCareSchedule(
    userPlantId: string,
    actionType: CareActionType,
    intervalDays: number,
  ): Observable<CareSchedule> {
    return this.httpClient.post<CareSchedule>(
      `${this.endpoint}/${userPlantId}/care/${actionType}/schedule`,
      { intervalDays },
    );
  }

  archiveCareSchedule(
    userPlantId: string,
    actionType: CareActionType,
  ): Observable<void> {
    return this.httpClient.delete<void>(
      `${this.endpoint}/${userPlantId}/care/${actionType}/schedule`,
    );
  }

  updateGrowingPlan(id: string, plan: GrowingPlanRequest): Observable<UserPlant> {
    return this.httpClient.put<UserPlant>(`${this.endpoint}/${id}/growing-plan`, plan);
  }

  getAll(): Observable<UserPlant[]> {
    return this.httpClient.get<UserPlant[]>(
      this.endpoint,
    );
  }

  getArchived(): Observable<UserPlant[]> {
    return this.httpClient.get<UserPlant[]>(
      `${this.endpoint}/archived`,
    );
  }

  getById(id: string): Observable<UserPlant> {
    return this.httpClient.get<UserPlant>(
      `${this.endpoint}/${id}`,
    );
  }

  archive(userPlantId: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.endpoint}/${userPlantId}`);
  }

  restore(userPlantId: string): Observable<void> {
    return this.httpClient.post<void>(
      `${this.endpoint}/${userPlantId}/restore`,
      null,
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

  update(
    userPlantId: string,
    request: UpdateUserPlantRequest,
  ): Observable<UserPlant> {
    return this.httpClient.put<UserPlant>(
      `${this.endpoint}/${userPlantId}`,
      request,
    );
  }
  getHarvests(id:string):Observable<HarvestRecord[]>{return this.httpClient.get<HarvestRecord[]>(`${this.endpoint}/${id}/harvests`);}
  recordHarvest(id:string,record:RecordHarvest):Observable<HarvestRecord>{return this.httpClient.post<HarvestRecord>(`${this.endpoint}/${id}/harvests`,record);}
  deleteHarvest(id:string,recordId:string):Observable<void>{return this.httpClient.delete<void>(`${this.endpoint}/${id}/harvests/${recordId}`);}

}
