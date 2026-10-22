# Curated growth references

Implemented 2026-10-22, initially for Monstera deliciosa and Chlorophytum comosum.
The English-only gallery appears on species details and personal plant details,
after care information. Catalog cards retain their compact single photograph.
These are different specimens illustrating features, not a personal photo journal,
a universal age/size timeline or a health diagnosis. No owner photo uploads,
storage service, automatic assessment or schedule changes are introduced.

## Sources and image rights

[NC State Extension's Monstera profile](https://plants.ces.ncsu.edu/plants/monstera-deliciosa/)
identifies the young plant photograph and describes the climbing habit, support
needs and characteristic split/perforated leaves. The two examples show a young
plant with whole leaves and a separate specimen with split leaves.
[Its spider plant profile](https://plants.ces.ncsu.edu/plants/chlorophytum-comosum/)
describes plantlets, propagation and light-dependent plantlet production. The
gallery shows hanging plantlets and a close-up of root beginnings. Neither example
assigns an age, promises a deadline or treats missing features as poor health.

Two existing covers are reused and two original, unmodified JPEGs are added:
young Monstera by Cindy Christian and spider plant root beginnings by Hope
Duckworth, both CC BY 4.0. Each added image is below 1 MiB. Original titles,
photographers, source pages, licenses, byte counts and SHA256 checksums are in the
[manifest](../../deploy/catalog-images.json), [dataset](../../deploy/catalog.houseplants.json)
and [photo credits](../../plant-care-web/public/images/plants/README.md).
All four examples were visually reviewed; temporary signed download URLs stay private.

## Compatibility and verification

Growth references are optional metadata within the existing catalog JSON column.
No database migration is required. Existing profiles without that field remain
readable; all photo paths, license pairs and reference text are validated during
the atomic scientific-name upsert. The 20 reviewed species and existing primary
photos remain; personal plant IDs, notes, care schedules and history are preserved.
The compatible bundle format remains `houseplants-en-v1`; the candidate SHA and
payload hashes identify exact content.

The asset check now validates every cover and growth photograph, including shared
photo reuse. Automated checks cover legacy profile compatibility, invalid reference
rejection, repeated imports and API serialization, missing-image credits, gallery
visibility, working photographs/source links, keyboard focus, accessibility and
320px reflow. Deployment and final test evidence will be recorded after acceptance.

Local verification passed: 144 backend tests, 19 frontend tests and all 12 catalog
browser checks across desktop/mobile Chromium and Firefox. The production image
built, the migration check found no pending changes, all 22 photo files matched
their manifest hashes, and desktop/phone gallery layouts were visually reviewed.
