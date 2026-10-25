# Personal planting plans and kitchen-garden additions

Accepted on staging 2026-10-25 as `e075dae96b06d75911e6b9207612c44e0b34d215`, with [successful full CI](https://github.com/Odin2325/PlantCare/actions/runs/40226127753). No production cutover or release tag.

## Behavior

Personal plant details now have three optional dates: Planned sowing, Planned
planting out and Expected harvest. Dates are user-entered targets, not a prediction,
a record of completed work or a new reminder schedule. They are independent of
acquisition, care schedules, care history and calendar feeds. Partial plans are
allowed, including purchased plants with no sowing date. Provided dates must be
in chronological order; equal dates are allowed. Clear dates changes the form;
only Save planting plan removes the saved values. Failed saves retain the draft.
Archived plants show their dates read-only and must be restored before editing.

Garden cards show all three dates and link to the editor. A With plan dates /
No plan dates filter combines with category and search; clearing resets them all.
The view remains a collection of saved active crops and trees, grouped by location.

## Persistence and privacy

Migration `20261025190809_AddGrowingPlans` adds only three nullable SQL `date`
columns to UserPlants. Existing rows start with no plan; no dates are inferred
from acquisition or care. Values use calendar dates without time-zone conversion.
The owner-scoped PUT endpoint inherits authentication and antiforgery protection.
Other owners and archived plants receive 404. Plan data appears in private,
password-protected account exports, remains attached to archived plants and is
erased with the owning plant/account under existing deletion and backup policies.

Deploy only forward migrations. The previous compatible runtime can ignore the
additive columns without clearing them. The existing erasure and snooze protocols
are unchanged. The isolated VPS recovery check now exercises planting-plan
save/reload and preservation after a rejected API activation on capable runtimes;
older historical runtimes skip only that new capability probe.

## Catalog additions

Twelve additions bring the catalog to **164 entries**, including **28 cultivars**,
**57 harvest guides**, **250 aliases across 125 entries** and **150 licensed photo
originals**. All previous 152 catalog records are unchanged.

New general entries: Parsley, Chives, Rosemary, Thyme, Spearmint, Kohlrabi, Kale
and Cauliflower. New cultivars: Parsley (Moss Curled), Kohlrabi (Kolibri), Kale
(Redbor) and Cauliflower (Igloo). Each cultivar has a distinct botanical identity;
its shared species/group photo is visibly marked as an unverified cultivar
reference. Kohlrabi is a swollen stem, not classified as a root or tuber.

Category memberships overlap: 107 houseplants, 35 vegetables, 14 fruit, eight trees,
eight roots/tubers and eight herbs. Existing growth references and sourced
non-toxic assessments remain unchanged. Absence of a warning remains unknown.

Care and photo references:

- [Parsley](https://plants.ces.ncsu.edu/plants/petroselinum-crispum/),
  [Chives](https://plants.ces.ncsu.edu/plants/allium-schoenoprasum/),
  [Rosemary](https://plants.ces.ncsu.edu/plants/salvia-rosmarinus/),
  [Thyme](https://plants.ces.ncsu.edu/plants/thymus-vulgaris/),
  [Spearmint](https://plants.ces.ncsu.edu/plants/mentha-spicata/).
- [Kohlrabi](https://plants.ces.ncsu.edu/plants/brassica-oleracea-kohlrabi-group/),
  [Kale](https://plants.ces.ncsu.edu/plants/brassica-oleracea-kale-group/),
  [Cauliflower photo group](https://plants.ces.ncsu.edu/plants/brassica-oleracea-cauliflower-broccoli-group/),
  [Cauliflower care](https://www.rhs.org.uk/vegetables/cauliflower/grow-your-own).
- [Moss Curled](https://www.rhs.org.uk/plants/112445/petroselinum-crispum-moss-curled/details),
  [Kolibri](https://www.rhs.org.uk/vegetables/kohl-rabi/grow-your-own),
  [Redbor](https://www.rhs.org.uk/plants/135076/brassica-oleracea-acephala-group-redbor/details),
  [Igloo](https://www.rhs.org.uk/vegetables/choosing-mini-vegetables).

All eight new originals were visually inspected, retained without image edits,
and recorded with original titles, credits, permanent source URLs, licenses and
SHA-256 hashes. Growing guidance uses local conditions and seed/nursery labels,
not fixed German planting months or guaranteed maturity dates.

## Deployment acceptance

- Full exact-candidate CI selected `full`: 182 backend tests, 58 frontend tests,
  retention safety, SQL runtime grants, real-cookie account isolation, password
  resets/session security, push failures/retries and all 60 browser/PWA cases passed.
  The existing browser journey now includes planting-plan save/reload/clear and
  unchanged care. No local full-suite rerun was added.
- Focused local validation: 31 backend and ten frontend tests, catalog imports,
  five CI-policy checks, all 150 photo hashes, 12 catalog cases, three Garden cases
  after fixing the filter label, and one real authenticated desktop journey.
  Desktop/phone previews and a second-time-zone date-only check passed.
- Pre-deployment encrypted VPS backup and isolated recovery passed. The new
  migration was applied to the restored copy; plan dates survived save/reload and
  rejected API activation recovery, with care history, session and keys intact.
  Source data was not modified by that drill. Replacement-host recovery is still
  outside this acceptance.
- Guarded activation checked bundle/image pins, compatible erasure/snooze
  protocols, restricted runtime access, migration and trusted HTTPS health.
- Public focused browser checks: 15 passed across desktop/mobile Chromium and
  Firefox. All 153 previous public records were exactly preserved, all 164 expected
  profiles matched, and all 150 photo hashes passed.
- A held existing-worker session accepted Update now and preserved sign-in, active
  plant IDs and completed history. Logout passed.
- The approved test account added one future-only care fixture. Live date saving,
  reload, clearing, Garden planned/unplanned filtering and private password-protected
  export passed; acquisition and care stayed unchanged. Only that fixture was
  archived, retaining its plan under the normal policy. Original collection/history
  remained intact. No external test mail or push was sent.
- Post-deployment encrypted backup, default local/VPS backup checks and both
  scheduled task states were healthy. No schedules or thresholds were changed.

Routine catalog-only work keeps the faster path already accepted in the Garden
increment. This migration/private-write feature correctly required full validation.
A next increment can display saved target dates in a planning calendar or add
explicit harvest records; neither behavior is implied by these date fields.
