import { describe, expect, it } from 'vitest';
import { normalizePlantSearch } from './plant-search';

describe('Plant name search normalization', () => {
  it.each([
    ["Devil’s ivy", 'devils ivy'], ["DEVIL'S  IVY", 'devils ivy'],
    ["Mother-in-law's tongue", 'mother in laws tongue'],
    ['  MOTHER–IN–LAW’S TONGUE  ', 'mother in laws tongue'],
    ['Epipremnum aureum', 'epipremnum aureum'], ['Café shelf', 'cafe shelf'], ['', ''],
  ])('matches %s as %s without changing the stored label', (name, query) => {
    expect(normalizePlantSearch(name)).toBe(query);
  });
});
