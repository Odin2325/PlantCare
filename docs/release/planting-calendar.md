# Garden planting calendar and herb/fruit additions

## Behavior

Garden now includes a monthly agenda of the saved target dates on active crops
and trees. Previous month, Next month, This month and Go to month controls let
owners browse without changing their plans. Sowing, planting-out and expected
harvest events use the personal nickname when available, otherwise the common
name; each complete event link opens that plant's details. Category, search and
plan filters apply to both the calendar and the collection cards.

Dates remain date-only values, including leap days and month/year boundaries.
The agenda groups multiple plants and actions on the same day and displays an
explicit empty-month state. Archived plants are excluded. Plans remain targets,
not completed work, predictions, care reminders or calendar-sharing events.
No persistence, account, export, reminder or migration changes are required.

## Catalog

Eight additions bring the reviewed catalog to **172 entries**, **34 cultivars**,
**65 harvest guides**, **253 aliases across 127 entries** and **152 licensed photo
originals**. The prior 164 entries are unchanged. New entries:

- Dill and Dill (Dukat).
- Coriander and Coriander (Calypso), also searchable as Cilantro.
- Apple tree (Bramley's Seedling), Pear tree (Concorde), Fig tree (Brunswick)
  and Plum tree (Opal).

The six cultivar entries use existing species photographs, explicitly marked
as reference photos whose cultivar is unverified. The two new herb originals
were visually reviewed and retained with credits, licenses and SHA-256 hashes.
No pet-safety certification is inferred from missing toxicity evidence. Fruit
instructions cover rootstock/site constraints and appropriate pollination
requirements without guaranteeing a harvest or a fixed German planting month.
Category memberships overlap: 107 houseplants, 35 vegetables, 18 fruit, 12 trees,
eight roots/tubers and 12 herbs. Growth-reference and non-toxic counts are unchanged.

Sources:

- [Dill care/photo](https://plants.ces.ncsu.edu/plants/anethum-graveolens/) and
  [Dukat identity](https://www.brookgreen.org/plant-collection/name/anethum-graveolens-dukat).
- [Coriander care/photo](https://plants.ces.ncsu.edu/plants/coriandrum-sativum/) and
  [Calypso](https://www.rhs.org.uk/herbs/coriander/grow-your-own).
- [Bramley's Seedling](https://www.rhs.org.uk/plants/224215/malus-domestica-bramley/details),
  [Concorde](https://www.rhs.org.uk/fruit/pears/grow-your-own),
  [Brunswick](https://www.rhs.org.uk/plants/62891/ficus-carica-brunswick-f/details),
  [Opal](https://www.rhs.org.uk/fruit/plums/grow-your-own).

## Validation and deployment

Pending exact-candidate browser and staging acceptance. Focused local tests cover
calendar date validation/navigation, grouped event links, filter updates,
read-only behavior and catalog imports/assets. The existing CI policy selects
full validation for Garden source paths; this increment does not change that
policy. The accepted prior planting-plan recovery drill remains applicable to
unchanged persistence; do not claim a new replacement-host recovery drill.
