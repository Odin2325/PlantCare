# Garden harvest overview

Prepared 2026-10-26. Deployment acceptance is pending.

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
