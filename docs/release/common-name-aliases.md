# Familiar alternative plant names

Implemented 2026-10-22. Catalog and My plants search include reviewed English
alternative common names without changing the primary common name, scientific
name, personal nickname or stable IDs. Species details show Also known as, with
a reminder that common names can identify different plants. Compact catalog
cards retain their original heading, photo and secondary scientific name.

## Reviewed aliases

These 16 alias entries for eight existing species were checked against the common-name
lists on the corresponding NC State Extension pages on 2026-10-22. Cultivar-only
labels and scientific synonyms are not presented as English common-name aliases.
The existing care-reference link and profile review date identify their source.

| Primary name | Alternative names | Source |
| --- | --- | --- |
| Golden pothos | Devil's ivy; Pothos | [Epipremnum aureum](https://plants.ces.ncsu.edu/plants/epipremnum-aureum/) |
| Snake plant | Mother-in-law's tongue | [Dracaena trifasciata](https://plants.ces.ncsu.edu/plants/dracaena-trifasciata/) |
| ZZ plant | Zanzibar gem; Eternity plant | [Zamioculcas zamiifolia](https://plants.ces.ncsu.edu/plants/zamioculcas-zamiifolia/) |
| Spider plant | Ribbon plant; Spider ivy | [Chlorophytum comosum](https://plants.ces.ncsu.edu/plants/chlorophytum-comosum/) |
| Jade plant | Money plant; Lucky plant; Jade tree | [Crassula ovata](https://plants.ces.ncsu.edu/plants/crassula-ovata/) |
| Rubber plant | Indian rubber plant | [Ficus elastica](https://plants.ces.ncsu.edu/plants/ficus-elastica/) |
| Aloe vera | Barbados aloe; Medicinal aloe | [Aloe vera](https://plants.ces.ncsu.edu/plants/aloe-vera/) |
| Corn plant | Cornstalk plant; Dragon tree; Ribbon plant | [Dracaena fragrans](https://plants.ces.ncsu.edu/plants/dracaena-fragrans/) |

Shared names intentionally return separate species: Ribbon plant finds corn plant
and spider plant; Money plant finds jade plant and the existing Chinese money
plant name. Choosing a result still uses its species ID. Names do not establish
identification or imply that care, toxicity or uses are interchangeable.

## Search and compatibility

Search folds case, apostrophe variants, hyphens/dashes, whitespace and accents for
matching only. Queries such as Devils ivy and mother in laws tongue work while
the typed query, source spelling and display names are retained. Catalog search
continues to combine with light/sort filters and preserve URL state across detail
navigation/reload. My plants search keeps nickname, species, notes, location and
tag matching, plus its separate exact location/tag filters. It does not rename
plants or edit notes, schedules, history or notification text.

Aliases are optional within the existing catalog JSON column, limited to eight
distinct trimmed names of at most 80 characters per profile. Old profiles without
the field remain readable. Scientific-name upsert matching and the compatible
`houseplants-en-v1` bundle format remain; no SQL migration or image changes are
needed. Future imports still require source review rather than automatic alias
generation or translation. German display names are outside this English-only scope.

## Verification

All 149 backend tests passed, including old-profile compatibility, invalid alias
rejection, JSON round trips, repeatable full-dataset import and API alias output.
All 27 frontend tests passed, including punctuation normalization and owned-plant
alias search combined with collection filters and nickname preservation. The
production image built, the migration check found no pending changes, and all
22 original image files/titles/credits/licenses/checksums passed the asset check.
Staging and update acceptance is recorded below.

All 15 local catalog browser checks passed in desktop/mobile Chromium and Firefox,
including punctuation variants, shared-name ambiguity, source-name display, filter
state, reload/navigation, accessibility and 320px reflow. Desktop and phone detail
layouts were visually reviewed. A structural comparison confirmed that all 20
primary/scientific names, care fields, photos and growth references are unchanged;
only aliases and the affected profiles' review dates changed.

## Staging acceptance

Candidate `2ac81f1479a2c0219961a16d35f561ea1df5fef4` is live on staging, following
`22539196d11db01e5769f36db292e59e11f94813`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/39785295389) passed
for the exact commit, including backend/frontend, real SQL Server security and
push checks, browser journeys, accessibility and PWA behavior. Uploaded bundle
hashes, actual API/worker image IDs, controlled import, restricted runtime access,
readiness and operations checks passed during activation.

Public verification matched all 16 alias entries against the reviewed source
dataset. All 21 shared catalog IDs/common/scientific names and care fields remain;
all 22 served image hashes, titles, authors and license metadata match. Existing
growth references remain intact. The importer adds no new species or personal
records for this feature.

An existing service-worker-controlled session received the real Update now prompt
and changed from `main-AQVOAFGY.js` to `main-3OR3FFE7.js`. The approved test
account's cookie, personal plant IDs and completed calendar IDs were preserved;
the calendar event still opened its individual plant detail page, accessibility
passed and logout succeeded. This is browser evidence rather than a new physical
Android test. Open the installed app online and choose Update now when offered;
reinstallation is unnecessary.

All 15 catalog browser checks passed again against public trusted HTTPS in
desktop Chromium, mobile Chromium and Firefox. They cover alias punctuation,
unchanged primary headings, Also known as text, shared-name results with separate
scientific names, search/light filter navigation and reload, accessibility and
320px reflow, alongside the existing photo/care/growth-reference checks.

Fresh encrypted off-server VPS backups passed before and after activation. The
post-deployment capture ran after live browser/update checks finished. Both local
and VPS backup health checks reported healthy with no problems, and final public
readiness/operations checks passed. The private backup candidate pointer identifies
this deployed release.
