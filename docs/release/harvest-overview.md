# Garden harvest overview

Deployed and accepted on staging 2026-10-26 at `492a05a60ed70e218f0873758bc5aacc9a3e0ea9`.
[Full CI run](https://github.com/Odin2325/PlantCare/actions/runs/40397840998) passed before activation.

Garden now has a Harvest overview view alongside Plants and plans. It shows
recorded harvests for active owned crops and trees, with inclusive date filters
(up to 366 days) and the existing category, search and planting-plan filters.
The default is this calendar year through today on the device.

Totals include all matching records, keeping items, grams and kilograms separate.
Records without quantities are counted separately. The recent list contains at
most the latest 100 records across the garden for the requested range; filtering
can therefore leave fewer rows than the total. Plant links use nicknames first
and open the private plant detail and harvest log. Archived plants are excluded
from the overview; their individual harvest logs remain available.

The endpoint is an authenticated, owner-scoped read. Erasure filters also apply.
It does not change records, plans, care history, reminders, grants or the schema.
Fixed-scale decimal strings preserve exact summary quantities in the browser.

## Catalog increment

Twelve cultivars extend twelve crop types: Lapins sweet cherry, Golden Delicious
apple, Williams’ Bon Chrétien pear, Czar plum, Charlotte potato, Autumn King 2
carrot, Detroit 2 beetroot, Cherry Belle radish, Salad Bowl lettuce, Hurst Green
Shaft pea, Telegraph Improved cucumber and Confetti coriander.

The catalog now has 192 reviewed entries, including 50 cultivars. All prior 180
entries are preserved. The 156 credited image originals are unchanged; all new
cultivar photos are explicitly labelled species references. Source links are
stored per entry. UK source guidance does not establish fixed German harvest
months, suitability for every site or guaranteed yields.

## Validation

Focused local checks cover harvest API privacy, complete exact totals beyond the
row cap, date validation, archive/category exclusion and account-erasure markers;
Garden component totals/filter/retry behavior; catalog import/upserts; browser
navigation, mobile reflow and accessibility. Full CI is required for the new
private API before staging deployment. Existing accepted harvest persistence
recovery at 2a50bb1 is the baseline; this read-only change adds no migration.

## Staging acceptance

All 18 focused local browser checks passed across desktop Chromium, mobile
Chromium and Firefox, including accessibility and 320px reflow. The focused
native checks passed: five harvest API tests, seven catalog import tests and
seven Garden component tests. Full CI then passed before activating the pinned
API/worker images. No full local suite was repeated.

Nine public browser cases passed. Public HTTPS verification matched all 192
reviewed profiles and 156 photo hashes; the previous 181 public records retained
their exact IDs and fields. The existing-worker update preserved the approved
synthetic account session, plant IDs, completed history and offline action icons.
A uniquely named synthetic future-care fixture verified 3.003 kg and 3 g as
separate totals, four records including one without quantity, date/category
filters and nickname detail links. It was archived afterwards, and the overview
excluded it while preserving the original collection and care history.

Verified pre/post-deployment encrypted backups completed to the approved external
drive. Local and VPS backup health and scheduled-task state were healthy after
acceptance. This does not change their existing PC/drive/SSH availability limits.
The accepted harvest recovery at `2a50bb1` remains the persistence baseline; a new
restore was unnecessary for this read-only increment and was not performed.
