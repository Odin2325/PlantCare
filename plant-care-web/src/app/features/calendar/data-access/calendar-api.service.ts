import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CalendarEntry, CalendarShare, CalendarSubscriptionCreated, CalendarSubscriptionStatus } from '../models/calendar.model';

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

  getShares(): Observable<CalendarShare[]> { return this.http.get<CalendarShare[]>('/api/calendar/shares'); }
  createShare(recipientEmail: string): Observable<CalendarShare> { return this.http.post<CalendarShare>('/api/calendar/shares', { recipientEmail }); }
  revokeShare(id: string): Observable<void> { return this.http.delete<void>(`/api/calendar/shares/${id}`); }
  getSharedEntries(id: string, from: Date, to: Date): Observable<CalendarEntry[]> {
    return this.http.get<CalendarEntry[]>(`/api/calendar/shares/${id}/entries`, { params: { fromUtc: from.toISOString(), toUtc: to.toISOString() } });
  }
}
