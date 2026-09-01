export type CareActionType =
  | 'Unknown'
  | 'Watering'
  | 'Fertilizing'
  | 'Misting'
  | 'Pruning'
  | 'Repotting';

export interface CareSchedule {
  id: string;
  actionType: CareActionType;
  intervalDays: number;
  lastCompletedAtUtc: string | null;
  nextDueAtUtc: string | null;
  isEnabled: boolean;
}

export interface CareEvent {
  id: string;
  careScheduleId: string;
  completedAtUtc: string;
  recordedAtUtc: string;
  notes: string | null;
}

export interface CompleteCareActionResult {
  schedule: CareSchedule;
  event: CareEvent;
}

export interface CompleteCareActionRequest {
  completedAtUtc: string | null;
  notes: string | null;
}

export interface CareEventHistory {
  id: string;
  careScheduleId: string;
  actionType: CareActionType;
  completedAtUtc: string;
  recordedAtUtc: string;
  notes: string | null;
}

export interface UserPlant {
  id: string;
  plantSpeciesId: string;
  speciesCommonName: string;
  speciesScientificName: string | null;
  nickname: string;
  location: string | null;
  acquiredOn: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  defaultWateringIntervalDays: number;
  defaultFertilizingIntervalDays: number | null;

  careSchedules: CareSchedule[];
}

export interface AddUserPlantRequest {
  plantSpeciesId: string;
  nickname: string;
  location: string | null;
  acquiredOn: string | null;
  notes: string | null;
}
