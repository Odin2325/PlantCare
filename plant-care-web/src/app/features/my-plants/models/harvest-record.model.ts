export interface HarvestRecord { id: string; harvestedOn: string; quantity: number | null; unit: 'items' | 'g' | 'kg' | null; notes: string | null; recordedAtUtc: string; }
export interface RecordHarvest { harvestedOn: string; quantity: number | null; unit: string | null; notes: string | null; }
