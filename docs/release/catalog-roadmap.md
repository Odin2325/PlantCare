# Plant catalog and image roadmap

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

The first 15 entries have local licensed photos on catalog and personal plant
pages, English alternative text, visible source/license credits and missing-image
fallbacks. Original files and attribution are checked together in CI. Add future
species through the same reviewed dataset and photo-manifest procedure. Keep
image delivery within the owner's hosting budget.

## Implemented: catalog browsing

Compact cards, light filtering, name sorting and preserved navigation state are
tracked in [catalog-browsing.md](catalog-browsing.md). All copy remains English.

## Implemented: curated growth references

Monstera and spider plant now have attributed examples of recognizable growth
features on species and personal plant detail pages. See
[growth-references.md](growth-references.md) for source review, limitations and
verification. These show different specimens, without fixed timelines or diagnosis.
Expand curated references gradually; a personal photo journal remains a separate
future feature requiring its own storage, privacy and deletion design.

## Planned: familiar names first

Owner request 2026-10-22: users should identify plants quickly through English
common names, with scientific names secondary. The catalog already leads with
`commonName`; audit remaining plant headings, selectors, search results, care
events and notifications for consistency. For an owner's plant, retain the
nickname when present, then fall back to the English common name. Keep scientific
names as secondary identification and searchable text, preserving scientific-name
catalog import matching and stable species IDs. Review familiar English aliases
where a single common name is ambiguous or less recognizable. Do not introduce
German display names or replace personal nicknames.

Acceptance: catalog cards/details and plant selectors lead with a common name;
personal care views and reminder text lead with nickname or common name;
scientific names remain available without dominating the label. Search continues
to find both common and scientific names. Existing links, IDs and history remain.

## Planned: plant pictures in care notifications

Owner request 2026-10-22: show the plant needing care as the notification icon
where supported. Use the corresponding reviewed species photo initially, with
the PlantCare app icon as fallback for missing images, failed image loads or
platform limitations. This does not require a personal photo-upload feature.
Preserve photo rights/attribution and use only existing local public image assets;
do not put private data or credentials in image URLs. Retain recognizable reminder
text and the accepted Dashboard destination when a reminder is tapped.

Investigate browser/OS rendering before implementation: a supplied photo might
appear in a notification's main content while the OS retains a separate app icon
or monochrome status-bar badge. Do not promise replacement of every system icon.
Define the chosen image treatment with real installed Android/Chrome testing,
including multiple plants, absent/broken photos, the app closed and tapping the
notification. Browser-only tests cannot establish the physical notification layout.
This is planned work, not implemented or deployed by this roadmap update.
