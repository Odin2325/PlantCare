export interface CareNotification {
  id: string;
  userPlantId: string;
  plantName: string;
  actionType: string;
  dueAtUtc: string;
  createdAtUtc: string;
  readAtUtc: string | null;
}
