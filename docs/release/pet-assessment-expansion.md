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
