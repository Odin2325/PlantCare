# English catalog browsing

Deployed to staging 2026-10-21 after the 15-entry English starter catalog. This step makes
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
the production frontend build and all 17 frontend regression tests. A separate
real-browser check confirmed that Back retains filters and typing replaces the
current history entry; a below-fold reference photo decoded when scrolled into
view. Desktop and mobile layouts were visually reviewed.

The pinned staging runtime is `61d1382b7903813b085a0300b2d19f637b5a938a`, following
`20cb8e5560b9b6dc9b2fea3f2a54980f8d91c295`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/39644304979) passed for
that exact candidate, including backend/frontend, real SQL security/push checks,
browser journeys, accessibility and PWA updates. The complete activation helper
passed with the public-input permission correction, readiness, operations and
restricted database-access checks. No new database migration was introduced.

All six catalog browser checks then passed against public trusted HTTPS in all
three projects. The 15 public photo hashes match the committed originals. All
16 shared species IDs, common names and scientific names match the pre-update
snapshot, including the preserved legacy demo.

A browser held the previous frontend open across the real staging deployment.
Accepting **Update now** preserved the approved test account's session, personal
plant IDs and completed calendar event IDs. Its calendar link, personal detail
accessibility and logout checks passed. This is browser automation evidence,
not a new physical Android acceptance claim.

Fresh encrypted off-server backups passed before and after activation. The
post-update backup ran after the public browser checks to avoid overlapping its
intentional application pause. Local and VPS backup-health checks both reported
healthy with no problems after the update.
