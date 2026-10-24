# Growth-reference expansion and ten houseplants

Prepared 2026-10-24. This increment expands the English catalog from 58 to 68
reviewed entries and the growth galleries from eight examples across four species
to sixteen across eight. The candidate below is deployed on staging.

## Sources and content

Original English summaries were reviewed against these NC State Extension pages.
Suggested intervals prompt a moisture check; actual watering depends on the plant,
pot and conditions. Umbrella sedge explicitly needs wet roots, unlike succulents.

| Common name | Reviewed botanical source |
| --- | --- |
| Polka dot plant | [Hypoestes phyllostachya](https://plants.ces.ncsu.edu/plants/hypoestes-phyllostachya/) |
| Purple passion plant | [Gynura aurantiaca](https://plants.ces.ncsu.edu/plants/gynura-aurantiaca/) |
| Scarlet star | [Guzmania lingulata](https://plants.ces.ncsu.edu/plants/guzmania-lingulata/) |
| Japanese aralia | [Fatsia japonica](https://plants.ces.ncsu.edu/plants/fatsia-japonica/) |
| Norfolk Island pine | [Araucaria heterophylla](https://plants.ces.ncsu.edu/plants/araucaria-heterophylla/) |
| Croton | [Codiaeum variegatum](https://plants.ces.ncsu.edu/plants/codiaeum-variegatum/) |
| English ivy | [Hedera helix](https://plants.ces.ncsu.edu/plants/hedera-helix/) |
| Umbrella sedge | [Cyperus alternifolius](https://plants.ces.ncsu.edu/plants/cyperus-alternifolius/) |
| Lipstick plant | [Aeschynanthus radicans](https://plants.ces.ncsu.edu/plants/aeschynanthus-radicans/) |
| Goldfish plant | [Nematanthus gregarius](https://plants.ces.ncsu.edu/plants/nematanthus-gregarius/) |

The Japanese aralia photo is an outdoor specimen; the ivy photo shows the Gold
Child cultivar. Scarlet star bracts vary between cultivars, and its main rosette
declines after flowering while offshoots continue. These distinctions are explicit
in the entries. No indoor flowering dates or universal mature sizes are promised.

Norfolk Island pine's cat-and-dog classification uses the exact species in the
[ASPCA Australian Pine entry](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/australian-pine),
including its Norfolk Island pine synonym. Other entries without a sourced
assessment remain unknown; a false warning flag does not imply safety. The total
is thirteen sourced non-toxic cat-and-dog entries. Twenty new aliases bring search
coverage to 112 aliases across 56 entries.

New galleries use [jade plant](https://plants.ces.ncsu.edu/plants/crassula-ovata/)
branching form and trunk, [moth orchid](https://plants.ces.ncsu.edu/plants/phalaenopsis/)
flower stem and flowers, [peace lily](https://plants.ces.ncsu.edu/plants/spathiphyllum/)
leafy clump and spathe, and [wax plant](https://plants.ces.ncsu.edu/plants/hoya-carnosa/)
buds and flowers. Examples show different specimens, not a chronological sequence,
health diagnosis or timetable. The original jade trunk photo title retains that
specimen's stated age, while the English explanation rejects using it as a target.

## Images and compatibility

Fifteen original, unmodified photographs add ten covers and five gallery files;
three existing covers are reused. All 77 unique files are below 1 MiB and have
permanent source pages, original titles where supplied, photographer credits,
CC BY/BY-SA or CC0 rights and hashes in the
[manifest](../../deploy/catalog-images.json) and
[credits](../../plant-care-web/public/images/plants/README.md).
All fifteen additions were visually reviewed. Restricted NC/ND photos were
excluded. Temporary download links are not included in the repository.

Existing care fields, aliases, covers and galleries are preserved. Only four
existing profiles gain optional growth references and their review date. The
scientific-name upsert retains database IDs and personal references, with no
migration, new service, paid storage or personal uploads. The existing
`houseplants-en-v1` format and runtime protocols remain compatible.

Browser acceptance covers all new details/photos/credits, the thirteen-entry
pet filter and all eight galleries, including keyboard source links, sampled
accessibility and 320px reflow. Repeated-import tests pin totals and validate every
photo and profile. Public acceptance must additionally check old catalog IDs/care,
all served hashes, an existing service-worker-controlled session, personal data,
backup/recovery and trusted HTTPS health before claiming deployment complete.

## Staging acceptance

Candidate `7ce5a11df523ce38c094fbe48e6395f05fa38f27` is deployed after
[successful exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40125685319),
including all 45 browser cases and the SQL Server production-container checks.
A preparation-script Windows encoding error in the first candidate had changed
the hybrid multiplication sign and created one duplicate African mask plant. The
new duplicate had zero personal-plant references and was removed transactionally
with the original record and all personal plants preserved. Explicit UTF-8 source
reading restored the name; backend and strict browser count/name regressions now
prevent this recurrence. The first candidate was not accepted as the final release.

Local verification passed the 171-test backend baseline and all seven focused
catalog import regressions after the correction, 38 frontend tests, the production
build, all 45 browser cases and the 77-image attribution/integrity check. The
existing calendar stylesheet budget warning remains; the build passes.

All 27 public catalog/comparison browser cases passed across desktop/mobile
Chromium and Firefox. All four added galleries were visually reviewed at desktop
and phone widths; browser checks cover all eight at 320px with keyboard source
links and sampled accessibility. Public API verification preserved all 59 previous
catalog IDs and care fields, allowing only the four documented galleries/review
dates. All 68 reviewed profiles and 77 served image hashes match committed content.

An existing real service-worker-controlled staging session retained its login,
personal plant IDs and completed calendar history after activation, loaded the
expanded catalog/galleries and thirteen-entry pet filter, and logged out correctly.
The main frontend asset remains unchanged; this content update loads online
without a frontend upgrade or reinstall. Offline action icons remain available.
This is automated browser evidence, not a new physical Android delivery test.

Fresh encrypted off-server VPS backups passed before and after activation. The
new runtime passed isolated application recovery from the predeployment snapshot,
including sign-in, care history, worker restart/deduplication and failed-activation
recovery. Bundle hashes, pinned image IDs, compatible protocols, restricted runtime
grants and final trusted HTTPS readiness/operations passed. Both local and VPS
backup health checks finished healthy with no problem codes. An earlier failed
local scheduled-task result was cleared by a successful retry of the existing
task, ending with result 0; schedules and thresholds were unchanged. The
postdeployment VPS backup identifies this candidate.
No external mail/push was sent for this increment;
local/recovery messages were captured internally. No v1 tag or apex cutover is made.
