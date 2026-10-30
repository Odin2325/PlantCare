# Related cultivars and distinct photographs

Completed locally 2026-10-31; source review began 2026-10-30. This increment is
not pushed or deployed. The working catalog remains 238 reviewed entries and
96 cultivars; staging remains at its earlier accepted application and data.

Public catalog profiles now offer Explore related varieties. Cards link to the
other reviewed cultivars and the general entry, preserving the current search,
category and comparison query parameters. Matching uses the reviewed botanical
name after removing the declared cultivar suffix, plus explicit aliases for the
historical names already in this catalog. Unknown identities are not guessed
from common names. Horticultural crop groups such as kale and cauliflower remain
distinct. Loading/retry is separate from the main profile and add-plant form.
No account, care, reminder or private-data write is added.

## Image policy and coverage

The owner explicitly selected Cultivar photo pending when an exact-cultivar
licensed photograph is unavailable. Generic species photographs are removed from
cultivar records. The shared image component also suppresses old species-reference
cultivar photos, so older saved catalog metadata does not reintroduce them. It
shows the cultivar name in an accessible pending placeholder instead. General
species photos and explicitly supplied growth-reference examples remain usable.

Thirty new source-identified photo assets bring coverage to **31 cultivars**,
including the existing Iceberg image. **65 cultivars remain pending**, including
Tiny Tim. Roma now has a distinct photograph. Coverage also includes named apple,
pear, plum, cherry, potato, cucumber, basil, lettuce, raspberry, strawberry,
blueberry, onion, beetroot and variegated Monstera cultivars. A source's explicit
cultivar identification, matching reuse licence and visual inspection are
required; a plausible appearance alone is insufficient. The first Czar candidate
was rejected because it shows damaged fruit. Its record remains pending.

[The coverage matrix](../../deploy/catalog-cultivar-photos.json) records every
cultivar, source, identity evidence and verified/pending status. Pending means
not yet approved, not that no suitable photograph exists. Continue sourcing these
entries in later local batches, prioritizing Tiny Tim and other common cultivars.

There are **186 distinct photo assets**. All 156 earlier assets and their manifest
records are retained. New photographs use supported CC BY, CC BY-SA or CC0 terms.
Small originals are unchanged; larger photographs use Wikimedia-provided
thumbnails without further editing. [Public credits](../../plant-care-web/public/images/plants/README.md)
and [the manifest](../../deploy/catalog-images.json) retain author, source title,
licence, source-file name, thumbnail/change disclosure, size and SHA-256. Existing
application rights remain reserved and do not cover these photographs.

Only photo fields and superseded photo-disclaimer sentences in growing notes
changed. Catalog identities, botanical care fields, source references, categories,
aliases and growth examples are preserved. Personal records are not imported,
replaced or modified by this local work.

## Local verification and next checkpoint

- Five focused photo/relative-group frontend tests passed.
- The single affected repeated catalog-import integration case passed, including
  nullable pending images and exact coverage totals.
- Asset validation passed for all 186 files and 96 coverage records. Cultivar
  photographs must have distinct paths, hashes and source files; renamed generic
  photographs are rejected. Titles, credits, licences and source URLs must match.
- The new read-only browser case passed in desktop/mobile Chromium and Firefox,
  covering retry isolation, real image decoding, named pending placeholders,
  preserved browsing context, navigation, accessibility and 320-pixel reflow.
  Desktop/phone screenshots and all new source images were inspected locally.
- The frontend production build passed, retaining existing unrelated CSS-budget
  warnings. No application stack or VPS was restarted.
- Five conservative CI-selector tests passed. The cultivar coverage manifest and
  new public browser case join the catalog-only path; related-variety unit checks
  and the browser case run with future catalog pipelines. Changes to the selector,
  workflow and browser runner themselves still force full validation. This policy
  change will therefore receive full CI at the next accumulated-batch checkpoint,
  before any deployment. No pipeline was started for this increment.

The checkpoint still needs committed-candidate CI, real backend/browser
integration, catalog update and image preservation checks, existing-worker update
acceptance and the normal staging backup/health gates. There is no new schema.
