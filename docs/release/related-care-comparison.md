# Direct related-variety comparisons and herb/bean batch

Implemented locally on 2026-10-31. No push, pipeline or deployment in this increment.

Each related-variety card now has Compare care. It opens the existing comparison
page with the current plant first and the chosen relative second. This starts a
two-plant comparison, replacing any previous comparison selection in that new URL;
the other browsing query parameters are retained. Browser Back returns to the
profile and its previous comparison context. The link has an accessible name
containing both plant names. No personal plant, account or care record is written.

The reviewed purple-basil botanical form is explicitly grouped with sweet basil
for related-variety discovery. Other botanical and crop-group boundaries retain
their existing behavior; common-name guessing is not added.

## Cultivar additions

| English display name | Reviewed identity/guidance source |
| --- | --- |
| Basil (Dark Opal) | [RHS Dark Opal](https://www.rhs.org.uk/plants/43433/ocimum-basilicum-var-purpurascens-dark-opal/details) |
| Greek basil (Bush) | [RHS Bush form and synonyms](https://www.rhs.org.uk/plants/283901/ocimum-basilicum-bush/details) |
| Chives (Forescate) | [RHS Forescate](https://www.rhs.org.uk/plants/92585/allium-schoenoprasum-forescate/details) |
| French bean (The Prince) | [RHS dwarf The Prince](https://www.rhs.org.uk/plants/119131/phaseolus-vulgaris-the-prince/details) |
| Borlotti bean (Borlotto Lingua di Fuoco) | [RHS climbing Borlotto Lingua di Fuoco](https://www.rhs.org.uk/plants/199448/phaseolus-vulgaris-borlotto-lingua-di-fuoco/details) |

Each entry includes two English search aliases and source-linked growing/harvest
notes. Greek basil represents the source-listed Bush form, not every plant sold
under that broad common name. Dwarf and climbing beans have different support
guidance; the borlotti entry distinguishes young-pod and mature-seed harvests.
Existing bean and allium pet warnings are preserved. Suggested watering checks
remain conditional on soil moisture, weather and growing conditions. No automatic
feeding schedule or fixed German sowing/harvest date is introduced.

The [Dark Opal photo source](https://commons.wikimedia.org/wiki/File:Ocimum_basilicum_%27Dark_Opal%27_kz01.jpg)
explicitly identifies the cultivar in the UMCS Botanical Garden, Lublin. The
CC BY-SA 4.0 licence and image were reviewed. The local asset is the
Wikimedia-provided 960-pixel thumbnail without further edits, with original title,
author, source, licence, change disclosure, byte size and checksum in the public
credits and image manifest. The other four additions use the approved cultivar
photo pending placeholder.

Current local totals: **249 reviewed entries / 107 cultivars / 189 photo assets**,
including **34 distinct cultivar photos and 73 pending**. All preceding 244 catalog
records and 188 image manifest records are unchanged. Staging totals are unchanged.

## Focused validation

- Two affected related-variety unit tests passed, covering botanical grouping,
  link construction, navigation updates and isolated retry behavior.
- The affected read-only browser case passed in desktop Chromium, mobile Chromium
  and Firefox. It verifies the exact two-plant comparison, retained search/category,
  Browser Back restoring the former selection, image decoding, pending placeholders,
  accessibility and 320-pixel reflow. The mobile screenshot was inspected.
- The frontend production build passed with the existing unrelated My plants and
  Calendar CSS-budget warnings.
- The single repeated catalog-import integration test passed. Asset and coverage
  validation passed for 189 files and all 107 cultivar records. Scientific names
  remain unique and existing records were checked for preservation.

The broader crop browser sample and expected totals are updated for the next
checkpoint. No full suite was run. The accumulated batch still requires full
validation before deployment, including the earlier CI-policy/workflow changes.
