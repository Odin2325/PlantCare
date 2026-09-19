import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CalendarEntry } from '../models/calendar.model';

@Injectable({ providedIn: 'root' })
export class CalendarApiService {
  private readonly http = inject(HttpClient);

  getEntries(from: Date, to: Date): Observable<CalendarEntry[]> {
    return this.http.get<CalendarEntry[]>('/api/calendar', { params: {
      fromUtc: from.toISOString(),
      toUtc: to.toISOString(),
    }});
  }
}
