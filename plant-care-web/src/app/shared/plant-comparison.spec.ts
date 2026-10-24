import { expect, it } from 'vitest';
import { parseComparisonIds } from './plant-comparison';
it('bounds public URL selections, removes duplicates and ignores malformed identifiers', () => {
  const ids = [1, 2, 3, 4].map(n => `${n}0000000-0000-0000-0000-000000000000`);
  expect(parseComparisonIds(null)).toEqual([]);
  expect(parseComparisonIds(`invalid,${ids[0]},${ids[0]},${ids[1]},${ids[2]},${ids[3]}`)).toEqual(ids.slice(0, 3));
  expect(parseComparisonIds('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA')).toEqual(['aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa']);
  expect(parseComparisonIds('x'.repeat(10000))).toEqual([]);
});
