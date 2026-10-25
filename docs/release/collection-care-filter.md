# Collection care filtering and 100 houseplants

Prepared 2026-10-25. My plants now filters the active collection by Needs care,
Upcoming care only or No active due dates. Needs care includes any enabled
schedule that is overdue or due today. A plant with both due and future tasks
appears in Needs care, rather than Upcoming only. Paused schedules, missing due
dates and invalid dates do not count as due. The filter uses the same device
calendar days as the existing visible care badges.

The page refreshes its clock each minute, including across midnight, and tears
down the timer when leaving the page. Status combines with name/alias search,
location, tags and sorting. A result count and Clear collection filters help
recover from empty results. Clearing resets view controls, without editing
schedules, tags, history or plant records. Collection filters remain local to the
page session; room names and personal search text are not added to shareable URLs.
No migration, notification change, paid service or upload storage is introduced.

## Reviewed additions

Nine English entries bring the catalog to 100 reviewed plants, 172 aliases across
87 entries and 113 licensed original photographs.

| Common name | Care and photo source |
| --- | --- |
| Crown of thorns | [Euphorbia milii](https://plants.ces.ncsu.edu/plants/euphorbia-milii/) |
| Desert rose | [Adenium obesum](https://plants.ces.ncsu.edu/plants/adenium-obesum/) |
| Silver jade | [Crassula arborescens](https://plants.ces.ncsu.edu/plants/crassula-arborescens/) |
| Sweetheart hoya | [Hoya kerrii](https://plants.ces.ncsu.edu/plants/hoya-kerrii/) |
| Jewel orchid | [Ludisia discolor](https://plants.ces.ncsu.edu/plants/ludisia-discolor/) |
| Dragon-tail plant | [Epipremnum pinnatum](https://plants.ces.ncsu.edu/plants/epipremnum-pinnatum/) |
| Tree philodendron | [Philodendron bipinnatifidum](https://plants.ces.ncsu.edu/plants/philodendron-bipinnatifidum/) |
| Ti plant | [Cordyline fruticosa](https://plants.ces.ncsu.edu/plants/cordyline-fruticosa/) |
| Sago palm | [Cycas revoluta](https://plants.ces.ncsu.edu/plants/cycas-revoluta/) |

Original guidance distinguishes drying succulent mixes from the jewel orchid's
moist terrestrial mix. Sweetheart hoya's single-leaf limitation is explicit.
Tree philodendron follows the reviewed source's scientific name; its alternate
Thaumatophyllum name stays searchable in the description. Dragon-tail plant's
photo shows Albo, rather than Cebu Blue. Sago palm's highly poisonous parts,
spiny leaf-detail reference and distinction from true palms are explicit.
Photo size is not a mature-size, flowering or growth-timeline promise.

The nine original photos were visually reviewed, retain supplied titles and
photographers, and have CC BY/BY-SA or CC0 licenses. All are below 1 MiB; see the
[manifest](../../deploy/catalog-images.json) and
[credits](../../plant-care-web/public/images/plants/README.md). Restricted NC/ND
files and temporary signed download URLs are excluded from committed content.
All previous 91 profiles, including their care and galleries, remain exactly
unchanged. Growth references remain 24 across twelve species; thirteen sourced
non-toxic cat-and-dog classifications remain. Missing assessments stay unknown.

## Acceptance requirements

Unit checks cover mixed due/future schedules, paused/invalid/undated schedules,
combined filters, read-only clearing and local-midnight rollover/timer disposal.
The real authenticated browser journey exercises care filtering before and after
completion. Catalog checks include all nine new details/photos/credits. Before
accepting staging, verify prior catalog IDs/care, all 113 served photo hashes,
exact-candidate CI, a real frontend update with personal data intact, and the
backup/recovery/health gates. No public launch or physical-phone acceptance is
implied by browser checks.
