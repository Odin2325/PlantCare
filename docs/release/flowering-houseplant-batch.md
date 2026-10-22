# Flowering houseplant catalog batch

Deployed to staging 2026-10-22. Five English entries expand the reviewed dataset from 15 to
20 plants. The original 15 records and image bytes are unchanged. This is a
curated indoor-plant selection, not a German popularity ranking. Existing legacy
Demo houseplant references remain intact.

| Common name | Scientific name | Care / reference-photo source |
| --- | --- | --- |
| African violet | Streptocarpus ionanthus | [NC State Extension](https://plants.ces.ncsu.edu/plants/streptocarpus-ionanthus/) |
| Flamingo flower | Anthurium andraeanum | [NC State Extension](https://plants.ces.ncsu.edu/plants/anthurium-andraeanum/) |
| Florist kalanchoe | Kalanchoe blossfeldiana | [NC State Extension](https://plants.ces.ncsu.edu/plants/kalanchoe-blossfeldiana/) |
| Thanksgiving cactus | Schlumbergera truncata | [NC State Extension](https://plants.ces.ncsu.edu/plants/schlumbergera-truncata/) |
| Florist's cyclamen | Cyclamen persicum | [NC State Extension](https://plants.ces.ncsu.edu/plants/cyclamen-persicum/) |

The concise care text is original, based on the linked profiles. Suggested
watering checks are editable product starting points, not prescribed watering
frequencies. All five use bright indirect light as their indoor care category;
individual placement notes remain important. Fertilizer defaults stay unset.

African violet notes its former Saintpaulia name and cultivar variation. The
cactus is distinguished from the round-edged Christmas cactus hybrid; guidance
covers autumn nights and forest-cactus moisture needs. Cyclamen guidance covers
summer dormancy, reduced watering, paused routine reminders and stopping feed.
This batch does not add automatic seasonal scheduling or growth-stage imagery.

Pet flags for flamingo flower, florist kalanchoe and cyclamen follow the poison
information on the university pages. African violet is listed as non-toxic by
[ASPCA](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/african-violet).
Thanksgiving cactus is listed as non-toxic by
[University of Georgia Extension](https://site.extension.uga.edu/dekalb/2025/11/growing-pet-safe-plants-this-fall-and-winter/)
and covered by [Clemson's pet-safe plant guidance](https://hgic.clemson.edu/pet-safe-indoor-houseplants-whats-safe-for-cats-and-dogs/).
No edible or risk-free claim is made.

All five photos were visually inspected and come from the corresponding plant
pages with explicit CC BY or CC BY-SA licenses. Original files are unchanged,
each below 1 MiB, with source, title, author, license and SHA256 in
[catalog-images.json](../../deploy/catalog-images.json). See the human-readable
[photo credits](../../plant-care-web/public/images/plants/README.md). Temporary
signed download URLs remain private and are not included in the dataset.

## Import and verification

The existing atomic scientific-name upsert adds these five entries without a
schema change. No personal plant, schedule, nickname or history edits are part
of this batch. The existing `houseplants-en-v1` bundle flag still identifies the
compatible dataset/import format, not a fixed count of entries; exact content is
pinned by the candidate SHA and payload checksums.

The source comparison confirmed the first 15 dataset records were unchanged.
All 20 image files/attributions/checksums passed the asset check, and the complete
backend suite passed 142 tests, including the repeated full-dataset import and
personal schedule preservation tests. The local SQL import and all nine catalog
browser checks passed in desktop/mobile Chromium and Firefox, including the five
new care pages, photos, attribution, pet flags and seasonal guidance. The
production application image built successfully, and desktop/phone layouts were
visually reviewed. Public deployment and identity-comparison results will be
recorded below.

The pinned staging candidate is `0394b3b123a919870dbea59cdaeaca714cbcd5a9`, following
`61d1382b7903813b085a0300b2d19f637b5a938a`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/39769265238) passed
for that exact commit, including frontend/backend, SQL authentication/isolation,
push handling, browser journeys, accessibility and PWA checks. Bundle hashes,
runtime image IDs, activation, restricted database access and public readiness/
operations checks passed. The public catalog now has 20 reviewed entries plus
the preserved demo; the original 16 IDs/common/scientific names are unchanged,
with exactly five added records. All 20 served photo SHA256 hashes match.

This is a data/photo-only release: the main JavaScript filename is unchanged.
A session opened before activation with the existing service worker loaded the
new catalog online afterward. Its private plant IDs and completed calendar IDs
matched before/after, its cookie stayed valid, both florist photos rendered, and
logout passed. New content does not require reinstalling the app. This is browser
evidence, not a new physical Android test or a claimed JavaScript-version change.

All nine catalog browser checks also passed against public trusted HTTPS in
desktop Chromium, mobile Chromium and Firefox, covering the five new entries,
all 20 image responses, search/filter navigation, photo rendering, attribution,
toxicity badges, seasonal guidance and sampled accessibility.

Fresh encrypted off-server backups passed before and after activation. The
post-update capture ran after the live browser checks; both local and VPS backup
health checks reported healthy with no problems afterward.
