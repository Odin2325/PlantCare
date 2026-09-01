import { CareActionType } from '../../my-plants/models/user-plant.model';

export type CareDueStatus =
  | 'Overdue'
  | 'DueToday'
  | 'Upcoming';

export interface CareDue {
  userPlantId: string;
  plantName: string;
  speciesCommonName: string;
  actionType: CareActionType;
  dueAtUtc: string;
  status: CareDueStatus;
}
