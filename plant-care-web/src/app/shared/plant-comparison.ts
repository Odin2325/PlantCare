// Comparison URLs contain only public catalog identifiers, never personal plants.
export function parseComparisonIds(value: string | null): string[] {
  return [...new Set((value ?? '').slice(0, 200).split(',').map(id => id.toLowerCase())
    .filter(id => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(id)))].slice(0, 3);
}
