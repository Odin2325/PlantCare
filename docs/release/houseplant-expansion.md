# Larger English houseplant catalog

Deployed to staging 2026-10-24. Twenty additions increase the reviewed dataset from 23 to 43
entries, with 47 original photo files including the existing growth references.
This is a curated indoor selection, not a measured popularity ranking for Germany.
The previous 23 dataset records and photo files are unchanged. The legacy demo is
preserved separately so personal plant references continue to work.

| Common name | Scientific name / scope | Care and licensed-photo source |
| --- | --- | --- |
| Fiddle-leaf fig | Ficus lyrata | [NC State](https://plants.ces.ncsu.edu/plants/ficus-lyrata/) |
| Chinese banyan | Ficus microcarpa | [NC State](https://plants.ces.ncsu.edu/plants/ficus-microcarpa/) |
| Madagascar dragon tree | Dracaena reflexa var. angustifolia | [NC State](https://plants.ces.ncsu.edu/plants/dracaena-reflexa-var-angustifolia/) |
| Dwarf umbrella tree | Heptapleurum arboricola | [NC State](https://plants.ces.ncsu.edu/plants/heptapleurum-arboricola/) |
| Money tree | Pachira aquatica | [NC State](https://plants.ces.ncsu.edu/plants/pachira-aquatica/) |
| Spineless yucca | Yucca gigantea | [NC State](https://plants.ces.ncsu.edu/plants/yucca-gigantea/) |
| Boston fern | Nephrolepis exaltata | [NC State](https://plants.ces.ncsu.edu/plants/nephrolepis-exaltata/) |
| Bird's nest fern | Asplenium nidus | [NC State](https://plants.ces.ncsu.edu/plants/asplenium-nidus/) |
| Prayer plant | Maranta leuconeura | [NC State](https://plants.ces.ncsu.edu/plants/maranta-leuconeura/) |
| Round-leaf calathea | Goeppertia orbifolia | [NC State](https://plants.ces.ncsu.edu/plants/goeppertia-orbifolia/) |
| Rattlesnake plant | Goeppertia insignis | [NC State](https://plants.ces.ncsu.edu/plants/goeppertia-insignis/) |
| Dieffenbachia | Dieffenbachia seguine | [NC State](https://plants.ces.ncsu.edu/plants/dieffenbachia-seguine/) |
| Chinese evergreen | Aglaonema houseplant species and hybrids | [NC State genus profile](https://plants.ces.ncsu.edu/plants/aglaonema/) |
| Arrowhead plant | Syngonium podophyllum | [NC State](https://plants.ces.ncsu.edu/plants/syngonium-podophyllum/) |
| Inch plant | Tradescantia zebrina | [NC State](https://plants.ces.ncsu.edu/plants/tradescantia-zebrina/) |
| Wax plant | Hoya carnosa | [NC State](https://plants.ces.ncsu.edu/plants/hoya-carnosa/) |
| String of hearts | Ceropegia woodii | [NC State](https://plants.ces.ncsu.edu/plants/ceropegia-woodii/) |
| Ponytail palm | Beaucarnea recurvata | [NC State](https://plants.ces.ncsu.edu/plants/beaucarnea-recurvata/) |
| Nerve plant | Fittonia albivenis | [NC State](https://plants.ces.ncsu.edu/plants/fittonia-albivenis/) |
| Heartleaf philodendron | Philodendron hederaceum | [NC State](https://plants.ces.ncsu.edu/plants/philodendron-hederaceum/) |

Original concise care guidance distinguishes moist-root ferns/prayer plants from
drying succulent-type plants. Intervals remain editable prompts to check conditions,
without automatically enabling fertilizer schedules. No fixed growth timeline,
flowering promise or air-purification claim is introduced. Old scientific names in
descriptions remain searchable. Forty English aliases bring the total to 62 across
31 entries; shared names such as money plant return multiple distinct plants.

The Chinese evergreen entry deliberately covers the genus's indoor species/hybrids
rather than labelling a genus photograph as one exact species. Its photo is titled
Aglaonema modestum. Other selected photos include a Chinese banyan bonsai and named
variegated forms of wax plant and heartleaf philodendron. These are reference images,
not a promise that every cultivar has the pictured colour or growth habit.

All 20 added photos were visually reviewed and remain unmodified original files
below 1 MiB each. Two sources supplied no image title; the manifest preserves null
titles and credits describe them as untitled. Permanent source/license links,
photographers, sizes and hashes are in [the manifest](../../deploy/catalog-images.json)
and [photo credits](../../plant-care-web/public/images/plants/README.md).

Four additional cat-and-dog references expand the non-toxic filter from five to
nine entries: [Boston fern](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/boston-fern),
[rattlesnake plant](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/prayer-plant),
[wax plant](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/honey-plant)
and [ponytail palm](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/pony-tail).
ASPCA's Prayer Plant page specifies Calathea insignis, a former name of Goeppertia
insignis; it is not used to classify Maranta merely because they share a common name.
The Boston fern page uses the older spelling of the cultivated Bostoniensis form.
Missing pet references on other entries do not add them to the non-toxic filter.
The warning flags include sap irritants; they do not represent a severity ranking.
Additional warning references include [Yucca at ASPCA](https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/yucca)
and [Ficus species at the Merck Veterinary Manual](https://www.merckvetmanual.com/multimedia/table/poisonous-houseplants-and-ornamentals).

## Verification

The asset check passed for all 47 originals, attributions, titles and file budgets.
The repeated-import regression checks all 43 profiles, 62 aliases, nine sourced
classifications and preserved personal care data. Existing growth references remain
eight across four species. The import format stays `houseplants-en-v1`; exact
content is identified by the committed candidate and bundle hashes.

## Staging acceptance

Candidate `5e6225a7946b131c7216a4deb5edf37f89b05345` passed
[exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40098922124),
including all 39 browser cases and the SQL Server security/reminder checks.
The atomic staging import preserved all 24 previous API catalog records, including
IDs, care metadata and the legacy demo, while adding the 20 reviewed entries.
All 43 deployed profiles matched the committed dataset and all 47 public photo
files matched their committed hashes over trusted HTTPS.

All 21 public catalog cases passed across desktop Chrome, mobile Chrome and
Firefox: common-name and alias search, combined light/pet filters, detail navigation,
each new detail page/photo/credit, existing flowering/growth references, reflow and
sampled accessibility. The real frontend update preserved the synthetic account's
session, personal IDs and completed history. See [reminder snoozing](reminder-snoozing.md)
for the accompanying migration, recovery and live reminder checks. No public-launch
or additional physical-device acceptance is implied.
