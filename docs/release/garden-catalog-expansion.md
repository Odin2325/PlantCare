# Garden crops and universal cultivar entries

Accepted on staging 2026-10-25. Application candidate `6171a92921429f65d759a382806357cce21b2798`, with [full CI](https://github.com/Odin2325/PlantCare/actions/runs/40221816892). The reviewed catalog contains 152 entries, including 24 named cultivars.

## Behavior

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

The dataset contains **152 entries**, including **24 named cultivars**,
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

| Plant | Cultivars |
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

The CI policy uses `scripts/ci_test_scope.py`. Recognized catalog-only
changes select catalog domain/import tests, affected frontend tests and focused
browser cases. Builds, image integrity and SQL runtime grants remain checked.
Security, reminder, backup, migration, dependency, unknown-path, deleted-file and
test-policy changes select full validation. Missing/invalid Git comparison bases
also fail closed to full. Workflow dispatch requests full validation by default.
This policy/workflow change received full CI before relying on its focused path.

Approval review resumed successfully on 2026-10-25. The final 152-entry workspace
passed six domain checks, seven importer/operator tests, seventeen focused
frontend tests, five CI-scope checks and integrity checks for 142 licensed original
photos. These checks were followed by the full CI and staging acceptance below. The
focused browser selection covers fifteen cases across desktop/mobile Chromium
and Firefox.
The six latest photos were visually inspected with their original title/credit.

The final additions use [NC State crop profiles](https://plants.ces.ncsu.edu/plants/beta-vulgaris-garden-beet-group/),
[RHS early peas](https://www.rhs.org.uk/education-learning/children-young-people/family-activities/grow-it/early-peas),
[RHS French Breakfast 3](https://www.rhs.org.uk/plants/119405/raphanus-sativus-french-breakfast-3/details)
and [RHS beetroot](https://www.rhs.org.uk/vegetables/beetroot/grow-your-own).
Each general crop links to its own university profile in the dataset. Generic
radish imagery shows daikon; cultivar references explicitly say the pictured
specimen is not verified as the cultivar. Existing 105 entries only gain categories.

## Deployment acceptance

- Full exact-candidate CI selected `full`: 173 backend tests, 54 frontend tests,
  backup retention, runtime grants, real-cookie account isolation, password reset
  and session security, push failures/retries and all 60 browser/PWA cases passed.
- Focused local acceptance: 13 backend profile/import tests, 17 frontend tests,
  five scope-policy tests and 15 browser cases across desktop/mobile Chromium and
  Firefox. The synthetic Garden fixture blocks service workers; real-worker tests
  remain in full validation. Desktop and phone layouts were visually reviewed.
- Public focused checks: 15 passed, with trusted HTTPS. All 152 expected profiles
  and 142 original photo hashes matched; existing 106 public identities and care
  fields were preserved, adding categories and optional metadata defaults only.
- Held real-worker session accepted Update now. Original active plant IDs and
  completed history survived, as did sign-in and logout behavior.
- An approved isolated account added a Tiny Tim tomato with a future-only care
  date. Garden nickname/location/harvest links and personal cultivar guidance
  passed. Only that fixture was archived; original collection/history stayed intact.
- Encrypted VPS backups passed before and after activation. Default local/VPS
  health checks and scheduled tasks were checked. No account emails or pushes were
  sent for this acceptance.
- No schema, permission or recovery-protocol change. The existing e41e6fc recovery
  drill remains the baseline; it was not unnecessarily repeated for a read-only
  route and additive catalog metadata. Activation still checked image pins,
  compatible protocols, restricted runtime access and readiness/operations health.

The following docs-only CI run exercises the catalog scope. Its result is recorded
separately after completion. Future auth, reminders, backups, migrations,
dependencies, workflows and test-policy changes keep full validation. Unknown,
deleted or missing-base changes also fail closed to full.

Category navigation stays inside the catalog; Garden is a personal collection
view. A next feature can add planting dates and a small sowing/harvest planner.
Personal photo uploads require their own storage/privacy/deletion design. No apex
cutover or release tag is included in this increment.
