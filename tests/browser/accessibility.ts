import AxeBuilder from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';

export async function auditAccessibility(page: Page, label: string) {
  let result;
  try {
    result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
  } catch { throw new Error(`Accessibility scan could not complete: ${label}; raw page data withheld.`); }
  // Never log HTML, input values, URLs or full axe results from authenticated pages.
  expect(result.violations.map(item => ({ rule: item.id, impact: item.impact, count: item.nodes.length })), label).toEqual([]);
}
