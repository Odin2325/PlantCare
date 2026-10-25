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
Final candidate and deployment results will be recorded after those checks pass.
