# Garden planting calendar and herb/fruit additions

Accepted on staging 2026-10-26 as `dc25f44eadd8f8e45a7eeb0b9a210234dd8fc740`, with [successful full CI](https://github.com/Odin2325/PlantCare/actions/runs/40229274697). No production cutover or release tag.

## Behavior

Garden now includes a monthly agenda of the saved target dates on active crops
and trees. Previous month, Next month, This month and Go to month controls let
owners browse without changing their plans. Sowing, planting-out and expected
harvest events use the personal nickname when available, otherwise the common
name; each complete event link opens that plant's details. Category, search and
plan filters apply to both the calendar and the collection cards.

Dates remain date-only values, including leap days and month/year boundaries.
The agenda groups multiple plants and actions on the same day and displays an
explicit empty-month state. Archived plants are excluded. Plans remain targets,
not completed work, predictions, care reminders or calendar-sharing events.
No persistence, account, export, reminder or migration changes are required.

## Catalog

Eight additions bring the reviewed catalog to **172 entries**, **34 cultivars**,
**65 harvest guides**, **253 aliases across 127 entries** and **152 licensed photo
originals**. The prior 164 entries are unchanged. New entries:

- Dill and Dill (Dukat).
- Coriander and Coriander (Calypso), also searchable as Cilantro.
- Apple tree (Bramley's Seedling), Pear tree (Concorde), Fig tree (Brunswick)
  and Plum tree (Opal).

The six cultivar entries use existing species photographs, explicitly marked
as reference photos whose cultivar is unverified. The two new herb originals
were visually reviewed and retained with credits, licenses and SHA-256 hashes.
No pet-safety certification is inferred from missing toxicity evidence. Fruit
instructions cover rootstock/site constraints and appropriate pollination
requirements without guaranteeing a harvest or a fixed German planting month.
Category memberships overlap: 107 houseplants, 35 vegetables, 18 fruit, 12 trees,
eight roots/tubers and 12 herbs. Growth-reference and non-toxic counts are unchanged.

Sources:

- [Dill care/photo](https://plants.ces.ncsu.edu/plants/anethum-graveolens/) and
  [Dukat identity](https://www.brookgreen.org/plant-collection/name/anethum-graveolens-dukat).
- [Coriander care/photo](https://plants.ces.ncsu.edu/plants/coriandrum-sativum/) and
  [Calypso](https://www.rhs.org.uk/herbs/coriander/grow-your-own).
- [Bramley's Seedling](https://www.rhs.org.uk/plants/224215/malus-domestica-bramley/details),
  [Concorde](https://www.rhs.org.uk/fruit/pears/grow-your-own),
  [Brunswick](https://www.rhs.org.uk/plants/62891/ficus-carica-brunswick-f/details),
  [Opal](https://www.rhs.org.uk/fruit/plums/grow-your-own).

## Validation and deployment

- Focused local checks: seven catalog-import and ten frontend tests passed,
  with the two calendar tests rerun after the final input adjustment. All 152
  photo files passed attribution/license/hash checks. Desktop and phone layouts,
  date-only display in another time zone and 15 distinct affected browser cases
  passed after correcting the fruit count and refreshing the final local image.
- Exact-candidate full CI: 182 backend tests, 60 frontend tests, backup safety,
  SQL access/account isolation/session security/push checks and all 60 browser/PWA
  cases passed. The current policy selects full for Garden source; no full local
  suite or redundant restore drill was added.
- A fresh encrypted pre-deployment VPS backup was verified. The accepted prior
  planting-plan recovery drill covers the unchanged persistence and protocols.
  Guarded activation verified pinned images, compatible protocols, restricted
  database access and both public health endpoints.
- Public staging acceptance: six focused cases passed across desktop/mobile
  Chromium and Firefox; all 165 prior public records were exactly unchanged,
  all 172 reviewed profiles matched and all 152 image hashes passed.
- A held existing-worker session accepted Update now, preserved sign-in, active
  plant IDs and completed history, and logged out successfully.
- The approved synthetic account verified all three target-date links, nickname
  display and month filtering against a newly created future-only fixture.
  Plan values, care schedules and original plants/history were preserved; only
  the uniquely identified test fixture was archived. No test email or push sent.
- On resuming after the usage pause, stale/failed scheduled backups had made
  the existing retention guard defer cleanup. Rerunning the existing local and
  VPS tasks restored fresh verified backups and healthy retention status.
- Post-deployment encrypted backup and default local/VPS health checks passed;
  both scheduled tasks were healthy. No schedules or thresholds changed.

## Next feature

Record an actual harvest separately from target dates and care completion,
alongside more reviewed everyday crops. Publication decisions and the remaining
release gates stay in the milestone plan.
