import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CalendarEntry, CalendarSubscriptionCreated, CalendarSubscriptionStatus } from '../models/calendar.model';

@Injectable({ providedIn: 'root' })
export class CalendarApiService {
  private readonly http = inject(HttpClient);

  getEntries(from: Date, to: Date): Observable<CalendarEntry[]> {
    return this.http.get<CalendarEntry[]>('/api/calendar', { params: {
      fromUtc: from.toISOString(),
      toUtc: to.toISOString(),
    }});
  }

  getSubscriptionStatus(): Observable<CalendarSubscriptionStatus> {
    return this.http.get<CalendarSubscriptionStatus>('/api/calendar/subscription');
  }

  createSubscription(): Observable<CalendarSubscriptionCreated> {
    return this.http.post<CalendarSubscriptionCreated>('/api/calendar/subscription', null);
  }

  revokeSubscription(): Observable<void> {
    return this.http.delete<void>('/api/calendar/subscription');
  }
}
