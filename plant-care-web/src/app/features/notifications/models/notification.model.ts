export interface CareNotification {
  id: string;
  userPlantId: string;
  plantName: string;
  actionType: string;
  dueAtUtc: string;
  createdAtUtc: string;
  readAtUtc: string | null;
}

export interface NotificationPreference {
  inAppEnabled: boolean;
  pushEnabled: boolean;
  wateringEnabled: boolean;
  fertilizingEnabled: boolean;
  mistingEnabled: boolean;
  pruningEnabled: boolean;
  repottingEnabled: boolean;
  reminderLeadTimeHours: number;
}
