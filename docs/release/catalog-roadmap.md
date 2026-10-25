# Plant catalog and image roadmap

Follow-up 2026-10-25: [growth-photo discovery and ten additions](growth-photo-discovery.md)
are deployed: 78 reviewed entries, 128 aliases across 65 entries and 87 licensed
original photos. The new combined gallery filter finds the existing sixteen
references across eight species. Thirteen sourced cat-and-dog classifications
remain; no missing warning is treated as proof of safety.

Follow-up 2026-10-24: [ten plants and expanded growth references](growth-reference-expansion.md)
are deployed: 68 reviewed entries, 112 aliases across 56 entries, thirteen sourced
cat-and-dog classifications and 77 original photo files. Sixteen growth references
now cover eight species; existing care fields and personal references remain.

Follow-up 2026-10-24: [plant comparison and fifteen additions](plant-comparison.md)
are deployed to staging: 58 reviewed entries, 92 aliases across 46 entries, twelve
sourced cat-and-dog classifications and 62 original photo files. Comparison keeps
up to three public selections through filters/details/reload and shows the
existing care guidance and references without inventing suitability scores.

Follow-up 2026-10-24: [twenty further houseplants](houseplant-expansion.md) expand
the reviewed dataset to 43 entries with 47 photo files. The existing 23 records
are unchanged; see that record for staging acceptance. Search now has 62 English
aliases across 31 entries, and the sourced non-toxic cat-and-dog filter covers nine.

Follow-up 2026-10-24: [pet-aware expansion](pet-aware-catalog.md) prepares three
additional indoor plants, five sourced cat-and-dog classifications and a combined
pet filter. See that record for exact verification/deployment status. Reminder
snoozing is deployed as a [separate feature increment](reminder-snoozing.md).

Owner request recorded 2026-10-14. Follow-up 2026-10-21: an English-only starter
catalog of 15 common houseplants and licensed photographs is implemented; see
[houseplant-starter-catalog.md](houseplant-starter-catalog.md) for verification and
deployment status. Existing `Demo houseplant` references are preserved as legacy
synthetic content; they are not converted into reviewed botanical guidance.

Follow-up 2026-10-22: five flowering houseplants are live on staging, bringing the
reviewed dataset to 20 entries. See [flowering-houseplant-batch.md](flowering-houseplant-batch.md)
for care sources, photo rights and deployment status.

## Release content: reviewed real species

The first 15 entries are implemented. Expand the attributed catalog of common
houseplants and flowering plants. Review scientific/common names, light needs,
watering guidance, temperature/humidity, seasonality and care notes against
reliable botanical sources. Record source links and review dates, and distinguish
species from cultivars where care differs. Suggested intervals are editable
starting points: environment, potting mix and season change actual needs.

Use the controlled catalog import/admin procedure, preserving existing plant
references and owner care history. Validate duplicates, required fields and
updates in staging before importing. Reviewed catalog content remains a release
requirement, rather than waiting for the image features.

## Implemented: species photos

All 78 reviewed entries have local licensed photos on catalog and personal plant
pages, English alternative text, visible source/license credits and missing-image
fallbacks. Original files and attribution are checked together in CI. Add future
species through the same reviewed dataset and photo-manifest procedure. Keep
image delivery within the owner's hosting budget.

## Implemented: catalog browsing

Compact cards, light filtering, name sorting and preserved navigation state are
tracked in [catalog-browsing.md](catalog-browsing.md). All copy remains English.

## Implemented: curated growth references

Monstera, spider plant, golden pothos, aloe vera, jade plant, moth orchid, peace lily and wax plant now have attributed examples of recognizable growth
features on species and personal plant detail pages. See
[growth-references.md](growth-references.md) for source review, limitations and
verification. These show different specimens, without fixed timelines or diagnosis.
Expand curated references gradually; a personal photo journal remains a separate
future feature requiring its own storage, privacy and deletion design.

## Verified: familiar names first

Owner request 2026-10-22: users should identify plants quickly through English
common names, with scientific names secondary. The catalog already leads with
`commonName`; the follow-up audit confirmed common-name-first species headings,
nickname-first personal care views and secondary scientific names. Push reminders
now also fall back to the common name for legacy blank nicknames. Scientific-name
catalog import matching, searchable names and stable species IDs are preserved.
The original 16 reviewed English alternative names for eight species established
catalog and collection search; see [common-name-aliases.md](common-name-aliases.md)
for punctuation matching and ambiguity. The latest [expansion](houseplant-expansion.md)
brought that total to 62 aliases across 31 entries. The latest
[comparison batch](plant-comparison.md) increases it to 92 across 46 entries.

Acceptance: catalog cards/details and plant selectors lead with a common name;
personal care views and reminder text lead with nickname or common name;
scientific names remain available without dominating the label. Search continues
to find both common and scientific names. Existing links, IDs and history remain.

## Implemented: care action pictures in notifications

Owner revision 2026-10-22: the worker now uses original cartoon-style action icons
instead of species photos. Reminder text and the Dashboard tap destination remain.
See [care-notification-icons.md](care-notification-icons.md) for mapping, tests and
deployment status. Actual rendering is platform-dependent. The earlier plant-photo
acceptance is historical; physical acceptance of the new icons is still pending.
