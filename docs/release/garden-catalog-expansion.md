# Garden crops and universal cultivar entries

Prepared 2026-10-25; **deployment acceptance pending**. The accepted staging
application is still `e41e6fc108d3abee779a2fe10404bc19e54e557e`, with 105 reviewed
entries. The work below is in the workspace pending final validation.

## Prepared behavior

The authenticated Garden tab groups saved active crops and trees by their existing
location. It supports nickname/common-name/location/tag search and category
filters, shows the next enabled care date and links to plant details and history.
Harvest guidance is read-only; care completion remains on Dashboard. Houseplants
and uncategorized legacy plants stay in My plants. No new storage or migration
is needed.

One Plant catalog supports category navigation tabs for Houseplants, Fruit, Vegetables, Roots & tubers, Herbs
and Trees. Categories can overlap: a fig is both Fruit and Trees. They describe
garden use, not botanical fruit/vegetable classification or food safety. Category
and Entry type combine with search, light, pet assessment, growth references and
sorting, and survive detail/comparison navigation and reload through URL state.
Clear filters retains the separate comparison selection.

Entry type distinguishes general species/groups from named cultivars across the
entire catalog. `cultivarName` is optional and independent of the category; it is
not restricted to tomatoes or peppers. Each cultivar is a separate selectable
catalog entry with a distinct full botanical designation, preserving the existing
import matching procedure and per-entry care plans. This is a curated starting
selection, not a claim to include every cultivar in existence. A scientific name
with a quoted cultivar identifies that horticultural entry, not a new species.

Growing and harvest guidance appears on public and personal detail pages. It uses
local frost risk, rainfall, supplier labels and rootstock/pollination considerations
instead of promising fixed German sowing or harvest dates. Suggested intervals
remain editable checks, not automatic watering or feeding instructions. No harvest
completion action, garden-bed planner or weather integration is introduced.

## Content inventory

The prepared dataset contains **152 entries**, including **24 named cultivars**,
and references **142 licensed original photos**. The 47 additions comprise 45 food
crop entries and two houseplant cultivars. Existing 105 entries only gain the
Houseplant category; their care, scientific identities, sources, cover photos,
review dates, galleries, aliases and pet assessments remain unchanged.

Categories overlap: 107 Houseplants, 29 Vegetables, 14 Fruit, eight Trees, eight
Roots & tubers, two Herbs. The 24 growth references across twelve species and
thirteen sourced non-toxic classifications remain unchanged. There are 237
English aliases across 117 entries.

General entries include tomato, sweet pepper, chili pepper, fig, apple, pear,
plum, strawberry, raspberry, blueberry, lettuce, potato, carrot, cucumber, squash,
onion, basil, spinach, garden peas, beans, radish, beetroot and garlic. Named cultivars cover:

| Plant | Prepared cultivars |
| --- | --- |
| Tomato | Tiny Tim, Roma |
| Pepper | Early Jalapeno, California Wonder |
| Lettuce | Iceberg, Little Gem |
| Apple | Elstar |
| Pear | Conference |
| Plum | Victoria |
| Fig | Brown Turkey |
| Strawberry | Elsanta |
| Raspberry | Autumn Bliss |
| Blueberry | Bluecrop |
| Potato | Annabelle |
| Carrot | Early Nantes 2 |
| Cucumber | Marketmore |
| Zucchini | Black Beauty |
| Onion | Sturon |
| Basil | Genovese |
| Garden pea | Kelvedon Wonder |
| Radish | French Breakfast 3 |
| Beetroot | Boltardy |
| Swiss cheese plant | Thai Constellation |
| Rubber plant | Tineke |

Twenty-four newly downloaded photos are original, unmodified, commercially usable
CC BY/CC BY-SA/CC0 files below 1 MiB. They were visually reviewed before use.
Credits, original titles, licenses, permanent source URLs and hashes are retained
in the image manifest and credits file. Iceberg lettuce has an identified iceberg
photo rather than a loose-leaf lettuce photo.

Twenty-three cultivar entries reuse an attributed species/group reference photo.
`photoIsSpeciesReference` adds a visible notice beside that photo on cards and
details, including personal plant details. The specimen is explicitly not verified
as the named cultivar. These references do not establish cultivar appearance or
identification. The reference flag requires both a photo and a cultivar name.
Cultivar-specific galleries are not copied from general species entries.

## Primary care and identification references

General entries use the corresponding NC State Extension Plant Toolbox pages:
[tomato](https://plants.ces.ncsu.edu/plants/solanum-lycopersicum/),
[sweet pepper](https://plants.ces.ncsu.edu/plants/capsicum-annuum-grossum-group/),
[hot pepper](https://plants.ces.ncsu.edu/plants/capsicum-annuum-longum-group/),
[pepper cultivars](https://plants.ces.ncsu.edu/plants/capsicum-annuum/),
[fig](https://plants.ces.ncsu.edu/plants/ficus-carica/),
[apple](https://plants.ces.ncsu.edu/plants/malus-domestica/),
[pear](https://plants.ces.ncsu.edu/plants/pyrus-communis/),
[plum](https://plants.ces.ncsu.edu/plants/prunus-domestica/),
[strawberry](https://plants.ces.ncsu.edu/plants/fragaria-x-ananassa/),
[raspberry](https://plants.ces.ncsu.edu/plants/rubus-idaeus/),
[blueberry](https://plants.ces.ncsu.edu/plants/vaccinium-corymbosum/),
[lettuce](https://plants.ces.ncsu.edu/plants/lactuca-sativa/),
[potato](https://plants.ces.ncsu.edu/plants/solanum-tuberosum/),
[carrot](https://plants.ces.ncsu.edu/plants/daucus-carota-subsp-sativus/),
[cucumber](https://plants.ces.ncsu.edu/plants/cucumis-sativus/),
[squash](https://plants.ces.ncsu.edu/plants/cucurbita-pepo/),
[onion](https://plants.ces.ncsu.edu/plants/allium-cepa/) and
[basil](https://plants.ces.ncsu.edu/plants/ocimum-basilicum/).

Cultivar profiles link to their reviewed RHS source in each entry, with
[University of Delaware Extension](https://www.udel.edu/academics/colleges/canr/cooperative-extension/fact-sheets/basil/)
for Genovese basil and the
[RHS strawberry reference](https://www.rhs.org.uk/advice/grow-your-own/features/fascinating-facts-strawberries)
for Elsanta. Secondary references include
[RHS potato cultivation](https://www.rhs.org.uk/vegetables/potatoes/grow-your-own),
[RHS carrot cultivation](https://www.rhs.org.uk/vegetables/carrots/grow-your-own),
[ASPCA apple toxicity](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/apple)
and [BfR bitter courgette guidance](https://www.bfr.bund.de/cm/349/caution-when-eating-bitter-tasting-courgettes.pdf).
No new non-toxic pet classifications are inferred from human food use.

## Proportional validation

`scripts/Test-LocalBrowserJourney.ps1 -CatalogOnly` selects the two garden tests
plus existing combined-light/navigation and comparison checks across desktop
Chromium, mobile Chromium and Firefox (12 cases). The existing default command
still runs the full browser suite. No account email or care action is needed for
the focused catalog path.

The prepared CI policy uses `scripts/ci_test_scope.py`. Recognized catalog-only
changes select catalog domain/import tests, affected frontend tests and focused
browser cases. Builds, image integrity and SQL runtime grants remain checked.
Security, reminder, backup, migration, dependency, unknown-path, deleted-file and
test-policy changes select full validation. Missing/invalid Git comparison bases
also fail closed to full. Workflow dispatch requests full validation by default.
This policy/workflow change itself must get full CI once before relying on it.

Approval review resumed successfully on 2026-10-25. The final 152-entry workspace
passed six domain checks, seven importer/operator tests, seventeen focused
frontend tests, five CI-scope checks and integrity checks for 142 licensed original
photos. Exact-candidate full CI and deployment remain pending. The focused browser
selection covers fifteen cases across desktop/mobile Chromium and Firefox;
final results are recorded in the acceptance update below.
The six latest photos were visually inspected with their original title/credit.

The final additions use [NC State crop profiles](https://plants.ces.ncsu.edu/plants/beta-vulgaris-garden-beet-group/),
[RHS early peas](https://www.rhs.org.uk/education-learning/children-young-people/family-activities/grow-it/early-peas),
[RHS French Breakfast 3](https://www.rhs.org.uk/plants/119405/raphanus-sativus-french-breakfast-3/details)
and [RHS beetroot](https://www.rhs.org.uk/vegetables/beetroot/grow-your-own).
Each general crop links to its own university profile in the dataset. Generic
radish imagery shows daikon; cultivar references explicitly say the pictured
specimen is not verified as the cultivar. Existing 105 entries only gain categories.

## Remaining acceptance

1. Run the catalog domain/import tests and affected frontend tests, including
   PlantPhoto; run the five CI-selection tests and static asset validation.
2. Build the local application and run `-CatalogOnly`. Review desktop/phone layouts
   and resolve actual failures before committing.
3. Commit/push and verify exact-candidate CI; the policy changes require its full
   path this time. Later catalog-only increments can use the focused path.
4. Export a pinned candidate, take the pre-deployment encrypted VPS backup and hold
   an approved existing test-account PWA session before activation.
5. Deploy with protocol/grant/HTTPS guards. Verify previous public records differ
   only by their declared category/default metadata additions, preserve all care
   fields and IDs, and compare every expected profile and photo hash.
6. Run the focused public catalog cases and held Update now/session/history smoke
   check; verify a personal cultivar guide in an approved isolated test fixture if
   needed. Do not mutate the owner's plants or send unnecessary test messages.
7. Take the post-deployment backup, check backup/task/HTTPS health, then record
   accepted candidate and actual results. No apex cutover or release tag is part
   of this increment.

Category navigation remains inside the catalog. Garden is a personal collection
view, avoiding duplicate top-level tabs for every crop category. Bed layout,
sowing dates and harvest recording can build on it in a later increment.
