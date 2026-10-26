export interface HarvestOverview {
  from: string;
  to: string;
  rows: { id: string; userPlantId: string; harvestedOn: string; quantity: number | null; unit: string | null; notes: string | null }[];
  plants: { userPlantId: string; recordCount: number; unquantifiedCount: number; quantities: Record<string, string> }[];
}
