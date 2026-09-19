export type CalendarEntryKind = 'Scheduled' | 'Completed';

export interface CalendarEntry {
  id: string;
  userPlantId: string;
  plantName: string;
  actionType: string;
  startsAtUtc: string;
  kind: CalendarEntryKind;
}
