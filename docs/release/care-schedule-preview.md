# Care schedule preview

Deployed and accepted on staging 2026-10-30 at `07848557db0ac3cc43f3d0c839a2af64de690138`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/41040578725) passed.

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

## Completed acceptance

Focused checks passed: eleven API/import tests, six collection-page tests and one
DST equivalence test. Fifteen catalog/Garden browser cases passed initially;
three category cases found a stale fruit-cultivar count and passed after its
correction. The affected desktop core journey passed with interval/weekday
preview/application, pause/resume, a travelling browser's retained time zone,
care/history and the existing account controls. The final candidate passed full
CI, including SQL runtime/security and all browser/PWA checks. Local full suites
were not repeated.

Public comparison preserved all 223 prior catalog records exactly and verified
all 230 reviewed profiles and 156 photo hashes over trusted HTTPS. The real
existing-worker update preserved the approved synthetic account's session,
collection IDs, completed history and cached care icons.

Only a future-care synthetic fixture was used for live writes. Preview failure,
retry, cancel, unchanged server data before confirmation, apply failure/retry,
committed interval and reload passed. Desktop/phone preview layouts were
inspected without horizontal overflow. The fixture was archived, and all original
active plants and completed history were unchanged. Failure injection blocked
service workers for deterministic routing; the real-worker update was verified
separately. No external test emails or pushes were required.

No database migration or stored format changed. The current schema's successful
isolated recovery drill remains the 2026-10-30 seasonal-preference baseline; a
new replacement-host recovery claim is not made. A fresh current-runtime backup
preceded activation. The final deployed-candidate encrypted backup and integrity
check passed through the existing scheduled task, and both default backup-health
checks were healthy afterward.

The post-deployment backup initially stopped before capture because the Windows
SSH agent service was stopped. Starting the service restored its existing identity
and server access without credentials being requested. The retry completed and
both scheduled tasks returned success. No startup policy or schedule changed.
Acceptance notes are committed locally under the owner's new batched-delivery
cadence; they do not trigger an additional CI run or deployment.
