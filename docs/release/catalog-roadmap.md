# Plant catalog and image roadmap

Owner request recorded 2026-10-14. Follow-up 2026-10-21: an English-only starter
catalog of 15 common houseplants and licensed photographs is implemented; see
[houseplant-starter-catalog.md](houseplant-starter-catalog.md) for verification and
deployment status. Existing `Demo houseplant` references are preserved as legacy
synthetic content; they are not converted into reviewed botanical guidance.

Follow-up 2026-10-22: five flowering houseplants are prepared, bringing the
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

## Later exploration: healthy growth progression

Explore curated growth-stage examples after thumbnails. Growth varies with age,
season and conditions, so examples must not imply a universal timeline or serve
as a diagnosis. Decide whether these are reference photos or an owner's personal
photo journal; those are different scopes. Any generated illustration must be
labelled and must not be treated as botanical evidence.
