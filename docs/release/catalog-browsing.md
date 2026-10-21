# English catalog browsing

Prepared 2026-10-21 after the 15-entry English starter catalog. This step makes
that catalog easier to scan before expanding its content further.

- Compact photo cards retain names, descriptions, source/license credits, pet
  toxicity warnings, light requirements and suggested watering-check intervals.
  Full soil, humidity, fertilizer and placement instructions remain on details.
- Search combines with the listed light-requirement category and English name
  sorting (A–Z or Z–A). Light categories are exact catalog requirements, not a
  claim that every plant tolerates every room in that category.
- Search/filter/sort state is encoded in public `q`, `light` and `sort` query
  parameters. Detail links and the catalog return link retain this state, as do
  reloads and browser back. Filter edits replace the current history entry so
  typing does not add one back-button stop per character. Clear filters restores
  the full A–Z catalog. Unsupported light/sort URL values use those defaults.
- Query-only navigation keeps focus in the active control. Moving to another
  page still focuses its heading. Controls have visible labels and focus states;
  a live result count and recoverable empty state explain filter combinations.
- All copy remains English. Shared species, photos, accounts, schedules and care
  history have no schema/content changes in this step.

## Verification

The catalog browser suite covers real typing/focus, combined light/search,
reverse sorting, retained controls after details and reload, empty combinations,
reset, unknown URL values, sampled axe accessibility and 320 CSS pixel reflow in
Chromium desktop/mobile and Firefox. All six local browser checks passed, as did
the production frontend build and all 17 frontend regression tests. Deployment
evidence is recorded when staging checks complete; preparation does not assert
deployment.
