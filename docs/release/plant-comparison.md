# Plant comparison and another 15 houseplants

Implemented for validation on 2026-10-24 following the owner's request for more
plants and the next feature. Staging acceptance is recorded after the release gates.

## Behavior

The catalog lets users select up to three public species, then open Compare
selected plants. Selection is independent of search/light/pet filters; clearing
filters keeps the selection, and Clear comparison removes it. Details, reload
and Back to plant catalog preserve the selection and filter query parameters.

The comparison shows common names first, scientific names, credited photos,
light, watering guidance, soil/drainage, humidity/room conditions, fertilizer and
pet warnings with existing care-reference links and review dates. It uses the
curated dataset without inventing suitability scores or fixed watering rules.
Unknown pet assessment is explicit; a false warning flag alone never produces a
non-toxic claim. Non-toxic classifications still carry the ingestion caution.

Public UUIDs in `compare` are bounded to three, deduplicated and normalized;
malformed values are ignored. Unavailable choices, fewer than two plants and API
failure have explanatory states with recovery links or retry. URL selections
contain no personal plant IDs, account data or bearer tokens. No database schema,
runtime permission, storage service or dependency is added.

Exact scientific-name searches identify that species even when another plant's
description mentions it. Common-name and alias searches retain multiple matches
where names are ambiguous. Existing source/license links remain visible.

## Catalog batch

Fifteen additions bring the reviewed dataset to 58 entries, 92 aliases across 46
entries and 62 original photo files. All previous 43 dataset records and their
image bytes remain unchanged. The legacy demo stays separate. Growth references
remain eight across four species. The format remains `houseplants-en-v1`.

| Common name | Scientific name / scope | Botanical care and photo source |
| --- | --- | --- |
| Swiss cheese vine | Monstera adansonii | [NC State](https://plants.ces.ncsu.edu/plants/monstera-adansonii/) |
| Satin pothos | Scindapsus pictus | [NC State](https://plants.ces.ncsu.edu/plants/scindapsus-pictus/) |
| Mini monstera | Rhaphidophora tetrasperma | [NC State](https://plants.ces.ncsu.edu/plants/rhaphidophora-tetrasperma/) |
| African mask plant | Alocasia × mortfontanensis | [NC State](https://plants.ces.ncsu.edu/plants/alocasia-x-mortfontanensis/) |
| Rex begonia | Begonia rex and its hybrids | [NC State](https://plants.ces.ncsu.edu/plants/begonia-rex-types/) |
| Asparagus fern | Asparagus setaceus | [NC State](https://plants.ces.ncsu.edu/plants/asparagus-setaceus/) |
| Kentia palm | Howea forsteriana | [NC State](https://plants.ces.ncsu.edu/plants/howea-forsteriana/) |
| Lady palm | Rhapis excelsa | [NC State](https://plants.ces.ncsu.edu/plants/rhapis-excelsa/) |
| Coffee plant | Coffea arabica | [NC State](https://plants.ces.ncsu.edu/plants/coffea-arabica/) |
| Clivia | Clivia miniata | [NC State](https://plants.ces.ncsu.edu/plants/clivia-miniata/) |
| String of pearls | Curio rowleyanus | [NC State](https://plants.ces.ncsu.edu/plants/curio-rowleyanus/) |
| Mexican snowball | Echeveria elegans | [NC State](https://plants.ces.ncsu.edu/plants/echeveria-elegans/) |
| Zebra haworthia | Haworthiopsis attenuata | [NC State](https://plants.ces.ncsu.edu/plants/haworthiopsis-attenuata/) |
| Watermelon peperomia | Peperomia argyraea | [NC State](https://plants.ces.ncsu.edu/plants/peperomia-argyraea/) |
| Panda plant | Kalanchoe tomentosa | [NC State](https://plants.ces.ncsu.edu/plants/kalanchoe-tomentosa/) |

Original English summaries distinguish moist-root foliage plants from succulent
care. Check intervals are editable starting prompts, not a botanical promise of
watering on an exact day. The Rex entry is explicitly a species/hybrid group;
the African mask photo shows Polly. Former names are explained in descriptions.
The Kentia photo shows nursery stock, lady palm a mature specimen and coffee an
outdoor fruiting branch; these do not promise that an indoor plant will fruit or
match a cultivar's appearance.

All 15 photos were visually reviewed and kept as original bytes below 1 MiB each.
Credits, permanent source/license links and hashes are in
[the manifest](../../deploy/catalog-images.json) and
[photo credits](../../plant-care-web/public/images/plants/README.md). They use
CC BY, CC BY-SA or CC0; no restrictive NC/ND photo is included.

The sourced cat-and-dog filter grows from nine to twelve entries with
[Kentia palm](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/kentia-palm),
[watermelon peperomia](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/watermelon-peperomia)
and [zebra haworthia](https://plants.ces.ncsu.edu/plants/haworthiopsis-attenuata/).
The last university profile explicitly tags cats and dogs as non-toxic, as well
as describing the plant as non-toxic to pets. Watermelon peperomia's ASPCA page
uses its former spelling, Peperomia argyreia. No Lady Palm safety claim is inferred
from an ambiguous common name or a different scientific spelling.
The satin pothos warning is also supported by
[ASPCA](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/satin-pothos).
Coffee's warning is a precaution based on its coffee seeds and
[Cornell Veterinary Medicine's caffeine warning for dogs and cats](https://www.vet.cornell.edu/hospitals/pharmacy/consumer-clinical-care-guidelines-animals/small-animal-toxins).

## Validation

The repeated-import regression covers 58 profiles, 92 aliases, twelve sourced pet
classifications, eight growth references and preserved personal data. Asset
integrity passed for all 62 originals. Local/staging browser and update evidence
will be recorded once completed. No public-launch or new physical-phone delivery
acceptance is implied by this increment.
