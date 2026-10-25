# Flowering references and five houseplants

Prepared 2026-10-25. This content increment adds African violet leaf-rosette and
flowering examples and Thanksgiving cactus bud and open-flower examples. The
Growth references filter now finds twenty examples across ten species. Photos
show different specimens, without a chronological growth record, age target or
health diagnosis. Existing cover photos are reused alongside two new originals.

## Reviewed additions

Five English entries bring the reviewed catalog to 83 plants, 138 English aliases
across 70 entries and 94 licensed original photo files.

| Common name | Reviewed care and photo source |
| --- | --- |
| Air plant | [Tillandsia ionantha](https://plants.ces.ncsu.edu/plants/tillandsia-ionantha/) |
| Staghorn fern | [Platycerium bifurcatum](https://plants.ces.ncsu.edu/plants/platycerium-bifurcatum/) |
| Mistletoe cactus | [Rhipsalis baccifera](https://plants.ces.ncsu.edu/plants/rhipsalis-baccifera/) |
| Aluminum plant | [Pilea cadierei](https://plants.ces.ncsu.edu/plants/pilea-cadierei/) |
| Purple heart | [Tradescantia pallida](https://plants.ces.ncsu.edu/plants/tradescantia-pallida/) |

The air plant has no ordinary potting-soil requirement and needs thorough leaf
watering followed by draining and drying. Staghorn fern uses an airy epiphytic
mix or mount; its mature shield fronds can naturally brown. Mistletoe cactus
requires more consistent moisture than a desert cactus. Purple heart has a
sourced ingestion/sap warning. Other additions without an explicit pet assessment
remain unknown; the thirteen existing sourced non-toxic classifications remain.
Suggested check intervals remain editable, rather than instructions to add water
regardless of conditions. No feeding reminder is enabled automatically.

Air plant and staghorn photos show mounted specimens; mistletoe cactus shows
stems with berries, without an indoor fruiting promise. Purple heart's foliage
photo illustrates a named form. Aluminum plant is distinguished from watermelon
peperomia in its description. Reference galleries were reviewed against
[African violet](https://plants.ces.ncsu.edu/plants/streptocarpus-ionanthus/) and
[Thanksgiving cactus](https://plants.ces.ncsu.edu/plants/schlumbergera-truncata/).

Seven added photographs were visually inspected and retain their original bytes,
titles, photographers, CC BY/BY-SA or CC0 licenses. Each is below 1 MiB. The
[manifest](../../deploy/catalog-images.json) records hashes and sizes; the
[credits](../../plant-care-web/public/images/plants/README.md) carry source and
license links. Temporary signed download URLs and restricted NC/ND photographs
are excluded from committed content.

## Compatibility and acceptance

Existing IDs, care guidance, aliases and covers are preserved. Only African violet
and Thanksgiving cactus gain optional galleries and a review date update. The
scientific-name upsert, existing data format and runtime protocols are unchanged.
No migration, personal upload, new paid service or public cutover is introduced.
This uses the existing frontend; online clients can retrieve the new catalog data
without a reinstall or a changed application script.

Repeated-import and browser expectations cover the new totals, galleries,
combined discovery/pet filters, light filtering, photo delivery and attribution.
Staging acceptance must verify existing records, served image hashes, an existing
service-worker session with personal data intact, exact-candidate CI and the
backup/recovery/health gates. Deployment evidence will be recorded after checks.

## Staging acceptance

Candidate `c2de2d3bf211e34711cbce0a0127bbce6ccc25e9` is deployed after
[successful exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40193127022).
All 171 backend tests, 38 frontend tests, production builds and 94-image integrity
checks passed. All 48 local browser cases passed across desktop/mobile Chromium
and Firefox. The candidate's three CI jobs passed, including the SQL Server
security checks and all 48 browser cases.

All 30 public catalog cases passed across the same three browser configurations.
Every one of the 79 previous public catalog IDs and care records is preserved;
only the two planned galleries and their review dates changed. All 83 reviewed
profiles and all 94 public photo hashes match the committed dataset. Desktop
and phone gallery layouts were visually reviewed.

An existing service-worker-controlled staging session retrieved the new content
with the same frontend script, retained login, personal plant IDs and completed
calendar history, retained offline care-action icons and logged out correctly.
No reinstall or new frontend activation is required for this content update.
This is browser evidence, without a new physical-device check.

Fresh encrypted off-server VPS backups passed before and after activation. The
candidate passed isolated application recovery, login/history and worker restart
and deduplication checks, including failed-activation recovery. Bundle hashes,
image pins, compatible protocols, restricted runtime access and trusted HTTPS
readiness and operations passed. Both backup health checks finished healthy,
and both existing scheduled tasks reported successful results. The final VPS
snapshot identifies this candidate. No public release tag or apex cutover is made.
