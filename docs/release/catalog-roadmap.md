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

Monstera, spider plant, golden pothos and aloe vera now have attributed examples of recognizable growth
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
Sixteen reviewed English alternative names for eight species are now implemented
in catalog and collection search; see [common-name-aliases.md](common-name-aliases.md)
for sources, punctuation matching, ambiguity and deployment verification.

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
