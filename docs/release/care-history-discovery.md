# Care history discovery and five more plants

Accepted on staging 2026-10-25. Application candidate `e41e6fc108d3abee779a2fe10404bc19e54e557e`.
[Exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40215337639)
passed all three jobs. This does not create a release tag or production cutover.

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
editing/deletion. Catalog checks pin the new count and names. The deployment gates
also cover exact-candidate CI, isolated recovery, trusted staging browser
acceptance and backups; the accepted results are recorded below.

## Accepted execution

All 171 native backend and 44 frontend tests passed. The initial browser run
caught an old single-result assumption for African violet and insufficient
contrast on the small recorded-at timestamp after editing. The corrected candidate
scopes the species card by its exact common-name heading and darkens that timestamp.
The superseded candidate was never activated. All 51 corrected local browser
cases passed across desktop/mobile Chromium and Firefox, including real filtering,
draft protection and accessibility. Exact-candidate CI passed all three jobs.

All 33 public catalog cases passed across those browser configurations. All 101
previous public records, including IDs and profiles, are exactly unchanged. All
105 reviewed profiles and 118 served photo hashes match committed content.
Read-only synthetic desktop and phone previews were visually reviewed without
horizontal overflow; these previews did not access a real account.

A held real service-worker session accepted Update now and preserved login,
personal IDs and completed history, existing catalog filters, comparison and
offline action icons. A separate read-only live check used an existing historical
plant in the approved dedicated account: action/date/notes/order filters, empty
results, clearing and draft controls passed. History and active collection were
identical before and after. Logout passed. No temporary plant, history mutation,
external test email or extra push was needed. These are browser checks, without
a new physical-phone acceptance claim.

Encrypted off-server VPS backups passed before and after activation. The candidate
passed isolated recovery with login/history/keys, worker restart/deduplication
and failed-activation recovery. Pinned images, compatible protocols, restricted
runtime grants and trusted HTTPS readiness/operations passed. Final local/VPS
backup health was clear and scheduled tasks had no failure result. The final VPS
snapshot identifies this application candidate. No infrastructure charge is added.
