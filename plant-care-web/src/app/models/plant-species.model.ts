export type SunlightRequirement =
  | 'Unknown'
  | 'LowLight'
  | 'MediumIndirectLight'
  | 'BrightIndirectLight'
  | 'PartialSun'
  | 'FullSun';

export interface PlantSpecies {
  catalogProfile?: PlantCatalogProfile | null;
  id: string;
  commonName: string;
  scientificName: string | null;
  description: string;
  sunlightRequirement: SunlightRequirement;
  sunlightInstructions: string;
  defaultWateringIntervalDays: number;
  wateringInstructions: string;
  defaultFertilizingIntervalDays: number | null;
  fertilizingInstructions: string | null;
  soilInstructions: string;
  humidityInstructions: string | null;
  minimumTemperatureCelsius: number | null;
  maximumTemperatureCelsius: number | null;
  isToxicToPets: boolean;
}

export interface PlantCatalogProfile {
  careSourceUrl: string;
  reviewedOn: string;
  photo: PlantCatalogPhoto | null;
}

export interface PlantCatalogPhoto {
  title?: string | null;
  path: string;
  altText: string;
  credit: string;
  sourceUrl: string;
  licenseName: string;
  licenseUrl: string;
}
