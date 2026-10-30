# Care schedule preview

Prepared 2026-10-30. Staging acceptance pending.

My plants now previews interval, weekday and pause/resume changes before an
explicit Apply schedule changes action. The preview shows the current and proposed
recurrence, enabled state and next due date in the device time zone. It explains
that application recalculates against the latest history, may produce an overdue
date and cannot recall provider-queued messages. Seasonal preferences remain
manual guidance; no interval is prescribed or automatically applied.

The owner-protected, antiforgery-protected POST preview runs the same domain
recurrence calculation on a detached copy and never saves. The existing PUT saves
only after confirmation. Editing any draft or cancelling removes the preview.
Failed requests retain the drafts; a detected care change in the current view
requires another preview. Other devices may change care after preview, so the
shown due date is an estimate, not a reserved occurrence. No schema change is
required. Previewing does not create history or alter reminders.

## Catalog batch

Eight fruit cultivars extend strawberries (Florence, Symphony, Pegasus),
raspberries (Joan J, All Gold), blueberries (Chandler) and apples (Fiesta,
James Grieve). The batch brings the reviewed dataset to 230 entries and 88
cultivars. Existing 222 reviewed records and all 156 licensed originals are
preserved. New photos are explicitly unverified species references, including
fruit colour. All Gold harvest guidance describes golden rather than red fruit.

Sources: [RHS strawberries](https://www.rhs.org.uk/fruit/strawberries/grow-your-own),
[RHS raspberries](https://www.rhs.org.uk/fruit/raspberries/grow-your-own),
[RHS Chandler](https://www.rhs.org.uk/plants/258502/vaccinium-chandler-f/details),
[RHS apples](https://www.rhs.org.uk/fruit/apples/grow-your-own).
UK relative seasons do not establish fixed German harvest dates; apple rootstock
and pollination partners still require local selection.

## Validation scope

Focused local validation covers non-persistence, owner/archive/erasure and cookie
boundaries, interval/weekday/pause application, DST equivalence, cancellation,
failed drafts, stale in-view care, catalog import and the affected browser journey.
The browser profile sample covers all eight additions plus three regression
profiles; API assertions still cover every catalog profile. Scheduling changes
select full CI once, including SQL security and browser/PWA checks. Activation
requires successful candidate CI, affected browser acceptance, a current backup,
existing-worker update checks and a synthetic future-care fixture on staging.
