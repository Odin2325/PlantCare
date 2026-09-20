export type CalendarEntryKind = 'Scheduled' | 'Completed';

export interface CalendarEntry {
  id: string;
  userPlantId: string;
  plantName: string;
  actionType: string;
  startsAtUtc: string;
  kind: CalendarEntryKind;
}

export interface CalendarSubscriptionStatus {
  isActive: boolean;
  createdAtUtc: string | null;
}

export interface CalendarSubscriptionCreated {
  subscriptionUrl: string;
  createdAtUtc: string;
}

export interface CalendarShare {
  id: string;
  ownerEmail: string;
  recipientEmail: string;
  createdAtUtc: string;
  isOwnedByCurrentUser: boolean;
}

export interface ExternalCalendarStatus {
  isConfigured: boolean;
  isConnected: boolean;
  accountEmail: string | null;
  lastSyncedAtUtc: string | null;
}

export interface ExternalCalendarSyncResult {
  created: number;
  updated: number;
  deleted: number;
  syncedAtUtc: string;
}
