import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CareNotification } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);

  getNotifications(): Observable<CareNotification[]> {
    return this.http.get<CareNotification[]>('/api/notifications');
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(`/api/notifications/${id}/read`, null);
  }

  getPushConfig(): Observable<{ isEnabled: boolean; publicKey: string }> { return this.http.get<{ isEnabled: boolean; publicKey: string }>('/api/push/config'); }
  savePushSubscription(subscription: { endpoint: string; p256dh: string; auth: string }): Observable<{ id: string }> { return this.http.post<{ id: string }>('/api/push/subscriptions', subscription); }
  removePushSubscription(id: string): Observable<void> { return this.http.delete<void>(`/api/push/subscriptions/${id}`); }
}
