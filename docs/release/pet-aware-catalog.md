# Pet-aware houseplant catalog expansion

Prepared 2026-10-24. Three English entries expand the reviewed catalog from 20
to 23 plants. This is an indoor-plant selection, not a German popularity ranking.
The legacy demo and existing personal plant references remain.

| Common name | Scientific name | Care and photo source | Cat-and-dog classification source |
| --- | --- | --- | --- |
| Parlor palm | Chamaedorea elegans | [NC State Extension](https://plants.ces.ncsu.edu/plants/chamaedorea-elegans/) | [ASPCA](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/bamboo-palm) |
| Cast iron plant | Aspidistra elatior | [NC State Extension](https://plants.ces.ncsu.edu/plants/aspidistra-elatior/) | [ASPCA](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/cast-iron-plant) |
| Baby rubber plant | Peperomia obtusifolia | [NC State Extension](https://plants.ces.ncsu.edu/plants/peperomia-obtusifolia/) | [ASPCA](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/baby-rubber-plant) |

Original care text covers light, checking moisture, drainage and seasonal feeding.
Suggested check intervals are editable starting points, not watering instructions.
No fertilizer schedule is enabled automatically. The cast iron photograph shows a
variegated cultivar. Baby rubber plant guidance distinguishes Peperomia from the
toxic Ficus elastica rubber plant. Six additional English aliases support search;
common names remain primary and scientific names secondary.

Three visually reviewed, unmodified photos add about 1.51 MiB in total, each below
1 MiB. Credits are Cindy Christian (CC BY 4.0), rrei320 (CC BY 2.0) and Jerzy Opiola
(CC BY-SA 4.0). Original titles, sources, licenses, byte counts and hashes are in
[the manifest](../../deploy/catalog-images.json) and
[photo credits](../../plant-care-web/public/images/plants/README.md).

## Classification and filter behavior

Cats and dogs offers All plants, Reviewed as non-toxic and Toxicity warning.
The non-toxic filter requires both a false toxicity flag and an explicit source
in optional `nonToxicToCatsAndDogsSourceUrl` metadata. A legacy false flag or missing
warning alone does not qualify. This concerns cats and dogs only, without promising
edible plants or covering other animals. Details link the classification source;
the UI explains that plant material can still cause stomach upset if eaten.

The three new plants and existing spider plant and African violet qualify.
The latter two now link their
[spider plant](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/spider-plant)
and [African violet](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/african-violet)
references. African violet's former Saintpaulia name is already documented.
Their care fields, photos, aliases and growth references are unchanged; other
existing records are unchanged.

Pet filtering combines with name/light/sort, survives details, reload and back
navigation through public `pets` parameters, and resets with Clear filters.
Unsupported values fall back to All plants. These parameters contain no user data.
Domain validation rejects malformed URLs and contradictory toxic/non-toxic profiles;
changing a species to toxic clears a previous non-toxic source. Old profiles remain
readable without the optional field.

## Verification and deployment

The existing JSON column and atomic scientific-name upsert need no migration.
Schedules, history and notifications are not edited by the import. Growth references
remain eight across four species. All 27 image files passed attribution/hash and
size checks. Backend tests passed: 54 domain, one application and 106 API tests,
including repeat imports, source validation, API metadata and existing care data.
All 32 frontend tests and the production image build passed. EF reports no pending
model changes. Desktop/mobile layouts were visually reviewed after image decoding;
the new filter journey passed in desktop/mobile Chromium and Firefox. Final full
catalog-suite and staging acceptance are recorded below.

## Staging acceptance

Candidate `b33c68d1eaf5e600a7cde158a333153bd501c128` is deployed on staging,
following `6d6d4d52fa6231d335b9b22705f4984ca14c5c76`.
[Exact-candidate CI 40092880770](https://github.com/Odin2325/PlantCare/actions/runs/40092880770)
completed successfully, including all three jobs and the real SQL security/push,
browser, sharing and PWA checks. Bundle hashes, actual API/worker image identities,
restricted runtime access, readiness and operations passed during activation.

All 18 catalog browser checks passed against public trusted HTTPS in desktop
Chromium, mobile-sized Chromium and Firefox. They cover the new filter, combined
controls, retained navigation/reload state, unknown-value fallback, working new
photos and source links, sampled accessibility and 320px reflow. Locally, 17 checks
passed on the first run; one existing Firefox growth-gallery accessibility scan
timed out and then passed in isolation. Full CI and all public checks passed without
weakening that scan. This is browser evidence, not new physical Android acceptance.

The public comparison preserved all 21 previous species IDs and care records,
including the legacy demo. All 23 reviewed profiles match the committed dataset;
all 27 served photo hashes and five care-action icon hashes match. Existing growth
references and original aliases remain, with six additional aliases in this batch.
The only previous profile changes are the two documented classification sources
and review dates.

A service-worker-controlled approved test account held the previous frontend
across activation, accepted the actual Update now offer and loaded the new frontend.
Its login, personal plant IDs and completed calendar history were preserved; the
new filter returned the five reviewed plants. All five care icons loaded offline,
and logout passed. Users should open the installed app online and choose Update
now when offered; reinstalling is unnecessary. No additional test emails or phone
pushes were sent for this feature.

Pre-deployment checks found the Windows SSH agent stopped and the latest scheduled
VPS backup failed. Starting the existing agent restored its stored identity and
trusted connection. The actual existing VPS backup task was retried successfully
(result 0), producing the fresh encrypted pre-deployment backup. A second encrypted
VPS backup passed after all live browser checks finished. Local and VPS health
checks both reported healthy with no problems at 12:33 UTC on 2026-10-24; final
public readiness/operations passed. No schedules, thresholds or agent startup mode
were changed. The normal dependency on a running/unlocked agent remains.

Reminder snoozing is the next separate feature increment. It must distinguish
delaying a reminder from recording completed care and handle already delivered
pushes, preferences, retries, account isolation and concurrent completion.
