import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CareDue } from '../models/dashboard.model';

@Injectable({
  providedIn: 'root',
})
export class DashboardApiService {
  private readonly httpClient =
    inject(HttpClient);

  private readonly endpoint =
    '/api/dashboard';

  getCareDue(
    daysAhead = 7,
  ): Observable<CareDue[]> {
    return this.httpClient.get<CareDue[]>(
      `${this.endpoint}/care`,
      {
        params: {
          daysAhead,
        },
      },
    );
  }
}
