# Growth-photo discovery and ten houseplants

Prepared 2026-10-25. The catalog adds a Growth references filter with All plants
and With growth photos options, plus a text label on cards with galleries. It
combines with name, light, pet and sort filters. `growth=available` persists through
details, comparison, reload and bookmarks; unsupported values use All plants.
Clear filters resets it while retaining the separate comparison selection.

This helps readers find the sixteen existing examples across eight species.
Examples are reference photographs, without a verified chronological sequence,
age timetable or health diagnosis. Personal photo uploads remain future work.

## Reviewed content

Ten English entries bring the dataset to 78 plants, 128 aliases across 65 entries
and 87 original licensed photos. All 68 previous profiles, including the Unicode
hybrid scientific name, are preserved exactly. Existing personal references are
retained by the scientific-name upsert. No migration or paid service is added.

Original summaries were reviewed against these NC State Extension profiles:

| Common name | Botanical source |
| --- | --- |
| Iron cross begonia | [Begonia masoniana](https://plants.ces.ncsu.edu/plants/begonia-masoniana/) |
| Peacock plant | [Goeppertia makoyana](https://plants.ces.ncsu.edu/plants/goeppertia-makoyana/) |
| Rose-painted calathea | [Goeppertia roseopicta](https://plants.ces.ncsu.edu/plants/goeppertia-roseopicta/) |
| Stromanthe | [Stromanthe thalia](https://plants.ces.ncsu.edu/plants/stromanthe-thalia/) |
| Pinstripe plant | [Goeppertia ornata](https://plants.ces.ncsu.edu/plants/goeppertia-ornata/) |
| Blushing philodendron | [Philodendron erubescens](https://plants.ces.ncsu.edu/plants/philodendron-erubescens/) |
| Purple shamrock | [Oxalis triangularis](https://plants.ces.ncsu.edu/plants/oxalis-triangularis/) |
| Persian violet | [Exacum affine](https://plants.ces.ncsu.edu/plants/exacum-affine/) |
| Creeping fig | [Ficus pumila](https://plants.ces.ncsu.edu/plants/ficus-pumila/) |
| Gardenia | [Gardenia jasminoides](https://plants.ces.ncsu.edu/plants/gardenia-jasminoides/) |

The stromanthe photo is presented as Tricolor; other forms may be green. Calathea
patterns and gardenia flowers vary by cultivar. Creeping fig's photo is outdoors,
and Persian violet's image is a flower detail. Neither implies indoor mature size
or a flowering guarantee. Purple shamrock's resting period needs reduced water;
Persian violet is often short-lived after flowering. Suggested care intervals
remain editable moisture-check prompts. No air-purification claims are included.

No new cat-and-dog safety classification is inferred: thirteen existing sourced
entries remain. Other plants without warnings retain unknown assessment unless
there is an explicit linked source. Non-toxic does not mean suitable for eating.

All ten added photos were visually reviewed. They retain original bytes, supplied
titles, photographers and CC BY/BY-SA licenses; each is below 1 MiB. See the
[manifest](../../deploy/catalog-images.json) and
[credits](../../plant-care-web/public/images/plants/README.md). Restricted NC/ND
photos and temporary signed download URLs are excluded from repository content.

## Acceptance requirements

Repeated-import tests pin the 78-entry total and the exact hybrid name. Browser
checks cover all ten additions, combined discovery filters, empty results,
malformed URL fallback, comparison preservation, gallery navigation, keyboard
labels, sampled accessibility and 320px reflow. Public acceptance must verify all
69 previous API records, all 87 photo hashes, a real frontend update preserving
the approved synthetic account's plants/history, and backup/recovery/health gates.
Final deployment acceptance follows below.


## Staging acceptance

Candidate `259dddd60c18c2b2a63efc5d3c22a32a6dacf8b6` is deployed after
[successful exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40185988745).
All 171 backend tests, 38 frontend tests, production builds and 87-image integrity
checks passed. All 48 local/CI browser cases passed, including the new combined
filter/navigation case. An initial browser fixture used bright light for Spider
plant; it was corrected to the existing reviewed medium-light setting. The plant's
care guidance was preserved. The calendar stylesheet's existing budget warning
remains non-blocking.

All 30 public catalog/discovery/comparison cases passed across desktop/mobile
Chromium and Firefox, including sampled accessibility and 320px reflow. Desktop
and phone filter layouts were visually reviewed. Every one of the 69 previous
public catalog records is unchanged, including IDs, care and gallery metadata.
All 78 reviewed profiles and 87 served photo hashes match committed content.

A real service-worker-controlled staging session held across activation accepted
Update now, loaded the new frontend and filter, retained login, personal plant IDs
and completed calendar history, retained offline care-action icons and logged out
correctly. This is automated browser evidence, without a new physical-phone check.
Existing delivered reminders are unchanged; no extra external email/push was sent.

Fresh encrypted off-server VPS backups passed before and after activation. The
candidate passed isolated application recovery, sign-in/history, worker restart
and deduplication, and failed-activation recovery. Bundle checksums, image pins,
compatible protocols, restricted runtime grants and trusted HTTPS readiness and
operations passed. Both backup health checks finished healthy with no problem
codes; both existing scheduled tasks were enabled with successful results.
The final VPS snapshot identifies this candidate. No v1 tag or apex cutover is made.
