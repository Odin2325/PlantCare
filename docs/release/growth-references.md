# Curated growth references

Follow-up 2026-10-24: [expanded galleries and ten houseplants](growth-reference-expansion.md)
add jade plant, moth orchid, peace lily and wax plant examples. Current coverage
is sixteen references across eight species, with the acceptance below retained as
historical evidence for the earlier batches.

## Expansion: golden pothos and aloe vera

Prepared 2026-10-22: four additional examples bring coverage to four species and
eight references. [NC State Extension's pothos profile](https://plants.ces.ncsu.edu/plants/epipremnum-aureum/)
supports the trailing/climbing habit and retained juvenile leaf shape in containers.
The compact potted example and trailing specimen illustrate different habits,
not a chronological before/after pair; variegation is not an age indicator.
[The aloe profile](https://plants.ces.ncsu.edu/plants/aloe-vera/)
supports the leaf rosette and mature plant's flowering stalk. The examples do not
promise flowering at a particular age or classify non-flowering plants as unhealthy.

Two covers are reused; new originals are pothos by Matt Stevens (Pitt County),
CC BY 4.0, and aloe by Tim Kortekamp, CC BY-SA 4.0. All four selected photographs
were visually reviewed. The 24 unique catalog images remain unmodified, each below
1 MiB, with original titles, attribution, licenses and hashes in the manifest.
All care fields, aliases, primary photographs and existing growth references are
preserved. This is catalog data in the existing JSON column, without a migration.
Candidate `dd3bc5e5432c8a397a9fdd39cdc597c198926dd2` is deployed on staging,
following `2ac81f1479a2c0219961a16d35f561ea1df5fef4`.
[Full exact-commit CI](https://github.com/Odin2325/PlantCare/actions/runs/39793037120)
passed, including real SQL Server isolation/session security, push failures and
retries, browser journeys, accessibility and PWA updates. Bundle hashes, pinned
image identities, restricted runtime access, readiness and operations health
passed during activation.

Local checks passed: 149 backend tests, all 15 catalog browser checks across
desktop/mobile Chromium and Firefox, and the 24-image attribution/hash check.
Desktop and phone layouts were visually reviewed; galleries also pass 320px
reflow and sampled accessibility checks. Existing dataset fields were compared
with the previous commit, allowing only these four references and review dates.

Public metadata verification preserved all 21 shared catalog IDs/names and care
fields, including the legacy demo. All 24 served image hashes and complete growth
reference metadata match the source dataset; aliases and primary photos remain.
A service-worker-controlled session held across activation retained its login,
personal plant IDs and completed calendar history, browsed the catalog and logged
out successfully. The frontend remains `main-3OR3FFE7.js`; reopening online fetches
the updated catalog without an app reinstall. This is automated browser evidence,
not a new physical Android check.

All 15 catalog browser checks passed again against public trusted HTTPS across
desktop/mobile Chromium and Firefox, including all four galleries, source/license
links, sampled accessibility and 320px reflow. Fresh encrypted off-server VPS
backups passed before and after activation; the latter ran after browser checks
finished. Both local and VPS backup health checks were healthy at 18:57 UTC on
2026-10-22, with no problem codes. Final public readiness and operations checks
passed. The backup candidate pointer identifies this deployed release.

## Initial implementation

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
320px reflow. Final deployment acceptance is recorded below.

Local verification passed: 144 backend tests, 19 frontend tests and all 12 catalog
browser checks across desktop/mobile Chromium and Firefox. The production image
built, the migration check found no pending changes, all 22 photo files matched
their manifest hashes, and desktop/phone gallery layouts were visually reviewed.

## Staging acceptance

Candidate `c23787c071074105a637e5f20dee06a0ea6f4cf9` is live on staging, following
`0394b3b123a919870dbea59cdaeaca714cbcd5a9`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/39774587225) passed
for that exact commit, including real SQL Server authentication, isolation,
password-reset/session security, push retries, browser journeys, accessibility
and PWA updates. Bundle checksums, actual API/worker image IDs, restricted runtime
permissions, readiness and operations health passed during activation.

All 12 catalog browser checks passed again against public trusted HTTPS in
desktop Chromium, mobile Chromium and Firefox. Both reference galleries rendered
their photographs and source/license links, passed sampled accessibility and
320px reflow, and remained absent for species without curated references.
All 22 served image hashes match the manifest. All 21 shared catalog IDs and
common/scientific names are unchanged, including the legacy demo; the original
20 species' care fields and primary photo metadata are unchanged.

A service-worker-controlled session on the previous release received the real
Update now prompt, then changed from `main-HJEIUYQK.js` to `main-AQVOAFGY.js`.
The approved test account's authentication cookie, plant IDs and completed
calendar IDs were preserved; a calendar event still opened the individual
plant detail page, its accessibility check passed, and logout succeeded. This
is browser evidence, not a new physical Android acceptance result. Open the
installed app online and choose Update now when offered; reinstalling is unnecessary.

Fresh encrypted off-server VPS backups passed before and after activation. The
post-deployment backup ran after all public browser checks finished. Both local
and VPS backup health checks reported healthy with no problems afterward; final
public readiness and operations checks passed. The backup candidate pointer now
identifies this deployed release.
