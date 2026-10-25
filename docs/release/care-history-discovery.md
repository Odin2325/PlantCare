# Care history discovery and five more plants

Prepared 2026-10-25. Staging activation and acceptance are pending; the previous
accepted runtime remains the collection-care increment until those checks pass.

Care history loads the 100 most recent entries using the existing owner-scoped
endpoint and supported limit. Users can combine care action, case-insensitive
notes search and inclusive completion dates in the device's time zone, then order
results newest or oldest first. The loaded-history limit is explicit: a filtered
empty result is not a claim about older saved records. Clear history filters
resets the view. Search text stays in memory, outside URLs and browser storage.
Filters never modify care events, schedules or reminders. During editing or a
pending event mutation the filter controls are disabled to preserve the draft.
Nickname headings fall back to the species common name when no nickname exists.

## Reviewed content

Five entries bring the reviewed dataset to 105 plants, with 118 original photos
and 179 English aliases across 90 entries. All previous 100 records are unchanged.
The 24 growth references across 12 species and 13 individually sourced non-toxic
assessments are unchanged. New entries have an unknown pet assessment; the legacy
boolean alone does not establish safety. Suggested intervals are editable check
prompts, not a universal watering prescription. No feeding schedule is enabled.

Care descriptions and photo metadata were reviewed against NC State Extension:

- [Grape ivy](https://plants.ces.ncsu.edu/plants/cissus-alata/)
- [Rabbit's foot fern](https://plants.ces.ncsu.edu/plants/davallia-fejeensis/)
- [Flame violet](https://plants.ces.ncsu.edu/plants/episcia-cupreata/)
- [False African violet](https://plants.ces.ncsu.edu/plants/streptocarpus-saxorum/)
- [Kangaroo vine](https://plants.ces.ncsu.edu/plants/cissus-antarctica/)

The flame violet cover identifies the Silver Sheen cultivar. These are reference
specimens rather than guaranteed appearances or an identification service.
Original permitted CC BY/CC BY-SA bytes, permanent sources, titles, photographers,
license links and hashes are recorded in the image manifest and visible credits.
All five originals are below 1 MiB and have been visually reviewed. No NC/ND-only
photo is included. Repeated import matches scientific names and preserves IDs.

## Validation scope

Unit checks cover combined filters, local-day boundaries, null notes, both sort
orders, unchanged source history, clearing, reversed ranges, empty states and
edit controls. Browser journeys exercise real history filtering and draft
protection across desktop/mobile Chromium and Firefox alongside existing event
editing/deletion. Catalog checks pin the new count and names. Exact-candidate CI,
isolated recovery, trusted staging browser acceptance and backups remain required
before this prepared increment is described as deployed.
