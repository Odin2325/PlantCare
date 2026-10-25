# Pet assessment clarity and eight houseplants

Prepared 2026-10-25. The Cats and dogs filter now includes Assessment unknown,
selected by `pets=unknown`. Catalog cards explicitly label plants without a
warning or a linked cat-and-dog assessment as Pet assessment unknown. The neutral
label is separate from both the warning and sourced non-toxic labels. Unknown
assessment does not imply safety or toxicity; the thirteen sourced non-toxic
classifications remain unchanged.

The option combines with search, light, growth references and sorting. It persists
through details, comparison, reload and bookmarks. Clear filters resets it while
retaining the separate comparison selection. Unsupported values still use All
plants. No backend schema, public account data or tracking service is added.

## Reviewed content

Eight English entries bring the catalog to 91 reviewed plants, 154 English aliases
across 78 entries and 104 licensed original photos.

| Common name | Care and photo source |
| --- | --- |
| Ripple peperomia | [Peperomia caperata](https://plants.ces.ncsu.edu/plants/peperomia-caperata/) |
| Friendship plant | [Pilea involucrata](https://plants.ces.ncsu.edu/plants/pilea-involucrata/) |
| Baby's tears | [Soleirolia soleirolii](https://plants.ces.ncsu.edu/plants/soleirolia-soleirolii/) |
| Maidenhair fern | [Adiantum raddianum](https://plants.ces.ncsu.edu/plants/adiantum-raddianum/) |
| African milk tree | [Euphorbia trigona](https://plants.ces.ncsu.edu/plants/euphorbia-trigona/) |
| Strawberry begonia | [Saxifraga stolonifera](https://plants.ces.ncsu.edu/plants/saxifraga-stolonifera/) |
| Lucky bamboo | [Dracaena sanderiana](https://plants.ces.ncsu.edu/plants/dracaena-sanderiana/) |
| Gold dust dracaena | [Dracaena surculosa](https://plants.ces.ncsu.edu/plants/dracaena-surculosa/) |

Original summaries distinguish moist-root ferns and baby's tears from the
African milk tree's drying succulent mix. Lucky bamboo's guidance distinguishes
soil growing from weekly water replacement for water-grown canes. The species
is a dracaena, rather than true bamboo. Strawberry begonia is a saxifrage.
African milk tree and both dracaenas carry sourced toxicity warnings; other
additions remain unassessed. Intervals remain editable moisture-check prompts.

Two galleries bring growth references to 24 examples across twelve species.
[Rubber plant](https://plants.ces.ncsu.edu/plants/ficus-elastica/) shows a young
variegated specimen and a separate upright potted form; colour differences do
not describe an age progression. [Boston fern](https://plants.ces.ncsu.edu/plants/nephrolepis-exaltata/)
shows a frond underside with spore clusters and a separate leafy form. Examples
show features of different specimens, without an age timetable or diagnosis.

## Photos and compatibility

Ten added originals were visually reviewed; all are below 1 MiB and retain
photographers, supplied titles, CC BY/BY-SA or CC0 licenses. See the
[manifest](../../deploy/catalog-images.json) and
[credits](../../plant-care-web/public/images/plants/README.md). The maidenhair and
strawberry begonia photos show outdoor specimens; friendship plant shows foliage
and tiny flowers. Lucky bamboo's reference is water-grown. Neither photo implies
all growing methods or cultivars look identical. Restricted NC/ND files and
transient signed download links are excluded from committed content.

All previous care fields, aliases, covers and personal references remain. Only
rubber plant and Boston fern gain galleries and a review date update. Existing
scientific-name upserts preserve IDs; the import format and runtime protocols
are unchanged. No paid service, upload storage, public release tag or apex
cutover is introduced.

## Acceptance requirements

Repeatable-import expectations pin the 91 plants, 154 aliases and twelve galleries.
Browser checks cover the unknown filter, combined navigation and comparison,
neutral labels, all new details/photos, gallery credits, accessibility and 320px
reflow. Before accepting staging, verify every prior catalog ID/care record,
all 104 image hashes, a real frontend update preserving the approved synthetic
account's data, exact-candidate CI and the backup/recovery/health gates.


## Staging acceptance

Candidate `361af0b5b3c96f8e033db330c78240846edf8a01` is deployed after
[successful exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40207608227).
All 171 backend tests, 38 frontend tests, production builds and 104-photo integrity
checks passed. All 51 local browser cases passed across desktop/mobile Chromium
and Firefox, including the combined unknown-assessment case. The initial native
frontend test command used an unsupported Node version; the available supported
Node 24.22 runtime passed the unchanged frontend suite. All three exact-candidate
CI jobs passed, including the SQL Server checks and all 51 browser cases.

All 33 public catalog cases passed across the same three browser configurations,
including unknown labels, filter navigation, photos/credits, accessibility and
320px reflow. All 84 prior catalog IDs and care records are retained; only rubber
plant and Boston fern gained the planned galleries/review dates. All 91 reviewed
profiles and 104 public image hashes match the committed content. Catalog and
gallery desktop/phone layouts were visually reviewed.

A real service-worker-controlled staging session accepted Update now, loaded the
new application script, exercised the unknown filter and expanded gallery discovery,
retained login, personal plant IDs and completed history, retained offline action
icons and logged out correctly. This is browser evidence rather than a new
physical-phone check. No extra external test emails or pushes were sent.

Fresh encrypted off-server VPS backups passed before and after activation.
Isolated application recovery retained login/history/keys and passed worker
restart/deduplication and failed-activation recovery. Bundle checksums, image
pins, compatible protocols, restricted runtime access, trusted HTTPS readiness
and operations passed. Final local/VPS backup health was clear, both existing
scheduled tasks reported successful results, and the final snapshot identifies
this candidate. No public release or production cutover is implied.
