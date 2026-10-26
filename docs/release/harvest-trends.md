# Harvest trends and cultivar batches

Prepared 2026-10-26. Staging acceptance pending.

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
