# English houseplant starter catalog

Deployed to staging 2026-10-21. The owner asked for common houseplants kept in Germany and
then explicitly required an English-only application. Germany guides selection;
all catalog names, guidance, search labels, alternative text and UI are English.
This is a curated starter selection, not a statistically ranked German top 15.

## Content

The [dataset](../../deploy/catalog.houseplants.json) contains Swiss cheese plant,
golden pothos, snake plant, ZZ plant, spider plant, rubber plant, weeping fig,
corn plant, peace lily, moth orchid, aloe vera, jade plant, Chinese money plant,
areca palm and white bird of paradise. Scientific names identify species or, for
peace lilies and moth orchids, explicitly labelled houseplant hybrid groups.
The areca entry uses Chrysalidocarpus lutescens and mentions the familiar older
name Dypsis lutescens. Guidance does not imply that all cultivars behave alike.

Concise original care summaries were checked against the linked NC State
Extension profiles; each row stores a care-source URL and review date. Selection
was informed by [toom's published German search review](https://toom.de/zimmerpflanzentrends-2026/)
and [RHS houseplant guidance](https://www.rhs.org.uk/plants/swiss-cheese-plants).
No retailer pictures or promotional descriptions were copied.

Watering intervals are editable check-in starting points chosen for this product,
not universal botanical watering frequencies. Instructions prioritize moisture,
drainage and slower winter growth. Fertilizer instructions are included without
automatically creating year-round fertilizer schedules. Unverified fixed
temperature ranges were left unspecified rather than filling them with false
precision. Growers should adjust guidance for the actual cultivar and conditions.

Pet flags use the university profiles, with the ASPCA references for
[spider plant](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/spider-plant),
[moth orchids](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/phalaenopsis-orchid)
and [areca palm](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/areca-palm),
and NC State's Pilea profile for that entry. Absence of a toxicity badge does not
mean a plant should be eaten or that pesticides/potting materials are pet-safe.
This is reference content, not diagnosis, treatment or human-use advice.

## Photos, rights and delivery

All 15 actual reference photographs come from the corresponding university plant
pages with explicit CC BY or CC BY-SA licenses. Noncommercial/no-derivatives
photos were excluded. Each original file is below 1 MiB; bytes are unmodified.
Titles, authors, source pages, license links, sizes and SHA256 hashes are recorded
in [catalog-images.json](../../deploy/catalog-images.json), with human-readable
[photo credits](../../plant-care-web/public/images/plants/README.md).

Photos are served by PlantCare, not hotlinked from the source. Temporary signed
download URLs/keys are not committed. Images are displayed with contain sizing,
lazy loading, English alternative text, visible title/author/license links and a
missing-image fallback. The sample is a visual reference, not a promise of a
growth timeline; white bird of paradise shows a mature flowering specimen.
The image licenses remain in force independently of the original app's
rights-reserved notice. No generated plant illustrations are used.

## Persistence and import

An additive nullable `PlantSpecies.CatalogProfile` column stores immutable profile
metadata as JSON. Images remain static files; the database holds references and
attribution. Legacy species work without metadata. Catalog details and personal
collection/detail responses include the corresponding profile.

The existing `--import-catalog` keeps its add-only behavior. The new controlled
`--upsert-catalog /operator/catalog.houseplants.json` validates the full input and
updates by scientific-name key within one transaction. Reimport preserves IDs.
It updates shared species information/defaults only, never existing personal
nicknames, notes, plant references, schedules or history. Existing demo data is
retained instead of silently converting someone else's plant into a real species.

The pinned bundle includes dataset/photo-manifest checksums and marks the dataset
as `houseplants-en-v1`. Staging activation imports it after migration/provisioning;
the pre-activation isolated VPS recovery drill tests the same import before
starting the target app. Old erasure-aware runtime bundles remain recoverable.

## Verification record

- Backend regression passed 142 tests (91 integration, 50 domain, 1 application),
  including repeat imports, complete dataset/credits, profile persistence,
  preserved personal schedules and invalid remote-photo/license rejection.
- Docker frontend production build passed. All 17 frontend tests passed, including
  image-failure attribution/fallback and recovery when a photo path changes.
- EF reported no pending model changes after the additive migration.
- Local SQL Server import and catalog browser checks passed in desktop Chromium,
  mobile Chromium and Firefox. Both full CI runs passed for candidate
  `20cb8e5560b9b6dc9b2fea3f2a54980f8d91c295`:
  [run 39631970202](https://github.com/Odin2325/PlantCare/actions/runs/39631970202)
  and [run 39631974194](https://github.com/Odin2325/PlantCare/actions/runs/39631974194).
- The isolated VPS recovery drill restored an encrypted pre-update backup into
  the candidate, applied the migration and catalog, and passed account/session,
  worker restart, care-history, failed-activation recovery and logout checks.
- The pinned API/worker images are active at https://staging.plantacare.org.
  Readiness, operations and restricted runtime database checks passed. The public
  API contains 15 reviewed entries plus the preserved legacy demo entry. All 15
  served photo files match the committed SHA256 hashes over trusted HTTPS.
- A browser held the previous release open through activation. Accepting
  **Update now** loaded the new release while preserving the approved test
  account's session, personal plant IDs and completed calendar history; the
  calendar link and personal detail accessibility check also passed.
- Public catalog search, image loading/credits, care details, no-results feedback
  and sampled accessibility passed in all three browsers. Desktop/phone layouts
  were visually reviewed. These public checks were rerun after the backup's
  intentional application pause. No new physical Android test is claimed.
- A fresh encrypted VPS backup completed after activation. Local and VPS backup
  health were checked separately from the feature tests.

The first activation rejected an unreadable catalog file and restored the
previous runtime. Windows transfer modes had made the public `deploy` directory
owner-only. The activation helper now sets that directory to 0755 and its four
public bundle inputs to 0644 after checksum validation. The same correction was
applied on staging; the complete activation then passed. Credential-file modes
are not part of that correction.
