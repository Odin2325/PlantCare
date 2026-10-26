# Harvest trends and cultivar batches

Deployed and accepted on staging 2026-10-26 at `753f64760269497abea70847a744b124f22c0924`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/40406726888) passed before activation.

Garden → Harvest overview now compares harvests by calendar month and personal
plant within the applied date range and existing Garden filters. Empty months
are included, partial months count only selected dates, and nicknames come first.
Each table shows record counts, records without a quantity and exact quantities
kept separate as items, grams and kilograms. No conversions, yield predictions,
percent-change claims or rankings across unlike crops/units are made.

Monthly totals are streamed from every matching owned active Garden harvest,
including records outside the recent 100-row list. Owner/category/archive and
erasure filters are retained. The API adds month-and-plant summaries to the
existing response, with fixed decimal strings. This is read-only and adds no
migration, grant, scheduled work or account export/deletion changes. An older
server response without monthly summaries displays an unavailable message rather
than calculating incomplete trends from the recent rows.

## Catalog batches

Eighteen cultivars across fourteen crop types bring the catalog to 210 reviewed
entries and 68 named cultivars. All previous 192 entries remain unchanged.

- Berries: Polka and Glen Ample raspberries; Cambridge Favourite and Honeoye
  strawberries; Duke and Spartan blueberries.
- Orchard fruit: Discovery apple, Doyenné du Comice pear, Marjorie’s Seedling plum.
- Vegetables: Medania spinach, Cobra and Purple Teepee beans, Sweet Dumpling
  squash, Solent Wight garlic and Red Baron onion.
- Herbs: Domino dill, Miss Jessopp’s Upright rosemary and Silver Posie thyme.

Primary RHS growing guides/profiles are linked per entry. The 156 credited photo
originals remain unchanged. New cultivar images are explicitly labelled species
references, without claiming their specimens show the named cultivar. No new pet
safety classification or fixed German growing dates are inferred.

## Validation plan

Run focused harvest API, catalog import and harvest component tests, plus affected
Garden/catalog browser cases and visual/mobile accessibility checks. The private
query change selects full CI before deployment. Reuse the accepted 2a50bb1
harvest persistence recovery baseline; this change requires no new restore.

## Staging acceptance

Focused local validation passed: six harvest API tests, seven catalog import
checks, four harvest component tests and eighteen Garden/catalog browser checks
across desktop Chromium, phone Chromium and Firefox. Mobile visual inspection,
320px reflow and axe accessibility checks passed. No full local suite was run.
Full CI passed before pinned API/worker activation.

Nine public browser cases passed. All 210 reviewed profiles and 156 photo hashes
matched, with the prior 193 public records unchanged in ID and fields. An
existing-worker PWA update preserved the synthetic account session, collection,
completed care history and cached action icons.

A dedicated approved synthetic account recorded four harvests across two months
on a uniquely named future-care test plant. Live checks verified 1.001 kg in the
earlier month, 2.002 kg plus 3 g in the current month, one record without quantity,
combined per-plant totals, nickname links, date/category filters and archive
exclusion. The fixture was archived; original plants and care history were
preserved. No confirmation/reset emails or push tests were needed.

Pre/post-deployment encrypted backups were verified on the approved external
drive. Both local and VPS backup health checks and scheduled-task status passed.
The accepted 2a50bb1 persistence recovery remains the baseline; this read-only
change did not require or perform another restore. Existing backup availability
and public-release limitations remain tracked separately.
