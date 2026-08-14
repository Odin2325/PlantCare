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
}

export interface AddUserPlantRequest {
  plantSpeciesId: string;
  nickname: string;
  location: string | null;
  acquiredOn: string | null;
  notes: string | null;
}
