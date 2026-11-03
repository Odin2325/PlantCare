# Shared visual refresh

Reviewed 2026-11-03. Local source changes; staging activation is a separate
checkpoint. This review covers the existing page families and the newly prepared
shortlist and photo journal. The application remains English.

## Findings and changes

| Surface | Finding | Change |
| --- | --- | --- |
| Shared controls | Notifications, calendar tools and some detail controls relied on browser-default buttons. Other pages each defined their own greens and small corner sizes. | One low-specificity native-control baseline and semantic design tokens in `src/styles.css`; consistent 44px button/input baseline, rounded controls, readable placeholder text and a styled native file picker. Page-specific actions and calendar states keep their semantics. |
| Navigation | Long signed-in navigation competed with the brand and account email, especially between phone and desktop sizes. | Existing PlantCare icon beside the wordmark, explicit active pills, compact spacing and truncated/hideable email. Phone navigation stays on one horizontally scrollable row; keyboard focus reveals off-screen links and Log out. |
| Typography and surfaces | Headings, borders, shadows and muted text varied between feature increments. | Shared botanical green, neutral text/border/tint palette, heading rhythm, softer card corners and restrained shadows. System fonts stay local; no font or styling service is added. |
| Account | Export/deletion were long blocks separated only by horizontal rules. | Distinct white panels with consistent spacing and borders. Destructive actions retain a red treatment, confirmation and disabled states. |
| Catalog/detail and collection | Small-screen add-plant grids, native date fields and empty-schedule controls could overflow. | Shrinkable fields/fieldset, bounded mobile form padding, responsive grid minimums and zero-minimum schedule columns. No content is clipped to hide overflow. |
| Garden/history/journal | Newer shared panels used separate control styles. | Growing plans/context, harvest, guides, relatives, shortlist, snooze and journal adopt the shared tokens. Image credits, pending cultivar placeholders, warning badges and actual links remain visible. |
| Accessibility | Focus declarations were duplicated throughout components. | One global visible keyboard focus outline; compatible component-specific rules remain where combined with other state styles. Reduced-motion support is preserved. |

## Verification

The production frontend build passes. Existing component-size budget warnings for
My plants and Calendar remain; thresholds were not increased. The shared tokens
add some CSS bytes in exchange for a single maintainable palette.

The synthetic browser audit covers eleven signed-in routes at 1440, 768 and 320px:
catalog, species detail, comparison, Dashboard, My plants, Garden, Calendar,
Notifications, personal detail, care history and Account. Five authentication
routes are also checked at 320px. It checks page reflow, accessible names/contrast
with axe, visible field focus, reachable mobile navigation and absence of page
errors or API writes. The fixture uses no real accounts, messages or private data.
It is included in the normal browser runner for the next combined pipeline.

The accumulated batch through `d615936571ca434f3888234cf25f022e17283feb` was
pushed for [one combined CI run](https://github.com/Odin2325/PlantCare/actions/runs/41712620040).
A subsequent local phone-only refinement stacks the collection's schedule action
fields instead of squeezing the selected label. Its production build and Chromium
audit pass; it is retained locally for the next batch, without a second pipeline.

Desktop Chromium, mobile Chromium and Firefox pass. Synthetic desktop/phone
screenshots were visually inspected for the catalog, calendar, Dashboard and
Account. The focused shell unit test and existing Chromium shortlist and photo
journal interaction tests pass, including uncertain-save reconciliation and
disabled/offline states. This does not certify every live dataset, physical
device, screen reader or enabled photo-processing deployment.

No schema, care schedule, reminder delivery, backup policy or photo activation flag
changes belong to this refresh. Keep the separate photo activation gates from
[the upload milestone](photo-journal-upload-ui.md). The next delivery checkpoint
must validate the accumulated candidate, migrations and recovery before staging
activation. Do not substitute these browser mocks for that checkpoint.
