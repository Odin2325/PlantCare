# Personal planting plans and kitchen-garden additions

Prepared 2026-10-25; deployment acceptance pending. The current staging runtime
is still `6171a92921429f65d759a382806357cce21b2798` with 152 reviewed entries.

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
originals**. All previous 152 catalog records are byte-for-value unchanged.

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

## Verification so far

31 focused backend tests passed: plant domain rules, owner/antiforgery boundaries,
private exports and catalog imports. Ten affected frontend tests passed. The 12
catalog browser cases passed; the three affected Garden cases passed on rerun
after fixing an explicit filter label. One real desktop Chromium journey passed
including plan persistence, clearing, unchanged care, export and deletion. Layout
previews passed at desktop and phone sizes, including date-only values in another
time zone. Photo integrity checks passed for all 150 originals; five CI-selection
tests passed.

This increment requires full exact-candidate CI because it adds a migration and a
private write endpoint. Before deployment: export a pinned bundle, take an
encrypted pre-backup, run isolated recovery against that bundle, hold a real PWA
session, then activate with runtime/grant/protocol/health guards. Afterwards verify
public catalog/profile/photo preservation, the held update, a future-only planting
fixture in the approved test account, a post-backup and default backup/task health.
No production cutover, release tag, paid service or extra notification is included.
