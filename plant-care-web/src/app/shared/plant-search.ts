// Match familiar-name punctuation without changing the displayed name or saved query.
export function normalizePlantSearch(value: string): string {
  return value.normalize('NFKD').replace(/\p{M}/gu, '')
    .toLocaleLowerCase('en').replace(/['\u2018\u2019]/g, '')
    .replace(/[-\u2010-\u2015]/g, ' ').replace(/\s+/g, ' ').trim();
}
