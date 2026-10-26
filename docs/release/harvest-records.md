# Personal harvest records and fruit catalog expansion

Prepared 2026-10-26; staging activation and exact-candidate acceptance are pending.

## Behavior and limits

Personal plant details have a separate Harvest records section. Owners can save
an actual date, optional quantity in whole items or up to three decimal places
of grams/kilograms, and up to 1000 characters of notes. Quantity must be positive
and no more than 1000000; its unit is required only when quantity is supplied.
Removing a record requires an explicit confirmation. Failed writes retain the
draft or record, with retry feedback; duplicate in-flight form submissions are
blocked. Records are shown newest harvest date first, up to the latest 100;
older records remain saved and appear in the private account export. Different
units are not summed. Editing can be done by removing and recording a correction.

The form uses the device's current calendar day and rejects future dates. The
server stores a date without time-zone conversion and permits the next UTC day
for users ahead of UTC, while rejecting dates beyond that tolerance. Targets,
care schedules/history, notifications and shared calendars remain unchanged.
Archived plants retain readable records; restore them before adding/removing.

## Persistence and privacy

Migration `20261026194824_AddHarvestRecords` adds a separate HarvestRecords table
with a cascading foreign key to UserPlants and a date/plant index. Existing data
is unchanged. Authentication, owner checks and global antiforgery protect writes;
unknown and other-owner plants/records return 404. Tombstoned owners are filtered
out. Account exports include all owned harvests; account/plant erasure cascades
physically to the new records, including archived plants. API access is limited
to SELECT/INSERT/DELETE on this table; the reminder worker receives no access.
Backups include the table in the existing database snapshot and retention policy.

Apply migrations forward only. A previous runtime can ignore the new table and
cannot display/export harvests until the feature runtime is restored; do not use
a schema downgrade to recover. Erasure and reminder-snooze protocols are unchanged.
The isolated recovery check now verifies a capable candidate's harvest create/read,
private export, unchanged care and preservation after a rejected API activation.

## Catalog

Eight additions bring the reviewed catalog to **180 entries**, **38 cultivars**,
**73 harvest guides**, **262 aliases across 131 entries** and **156 licensed photo
originals**. All prior 172 records are unchanged. New general entries are Sweet
cherry tree, Sour cherry tree, Peach tree and Grape vine; the cultivars are Stella,
Morello, Peregrine and Pinot Noir respectively. Category memberships overlap:
107 houseplants, 35 vegetables, 26 fruit, 18 trees, eight roots/tubers and 12 herbs.
Growth-reference and sourced non-toxic counts are unchanged.

All four original images were visually reviewed, remain unedited and retain their
licenses, credits, original captions and SHA-256 hashes. Cultivar photos are
explicitly unverified species references. The generic peach photo shows a dwarf
Bonanza specimen, disclosed in its description; it does not establish Peregrine's
size or fruit appearance. Care uses local site/rootstock/weather and cultivar labels
rather than fixed German months or guaranteed crop dates.

Sources:

- [Sweet cherry care/photo](https://plants.ces.ncsu.edu/plants/prunus-avium/),
  [sour cherry](https://plants.ces.ncsu.edu/plants/prunus-cerasus/),
  [peach](https://plants.ces.ncsu.edu/plants/prunus-persica/),
  [grape](https://plants.ces.ncsu.edu/plants/vitis-vinifera/).
- [Stella and cherry growing](https://www.rhs.org.uk/fruit/cherries/grow-your-own),
  [Morello](https://www.rhs.org.uk/plants/63690/prunus-cerasus-morello-c/details),
  [Peregrine and peach growing](https://www.rhs.org.uk/fruit/peaches/grow-your-own),
  [Peregrine assessment](https://www.rhs.org.uk/plants/pdfs/agm-lists/agmtreefruits.pdf),
  [Pinot Noir](https://www.rhs.org.uk/plants/68269/vitis-vinifera-pinot-noir-o-b/details),
  [grape dog warning](https://www.rhsplants.co.uk/plants/_/grape-pinot-noir/classid.2000047520/).

## Verification

Focused backend and frontend checks, local SQL/browser acceptance, exact-candidate
full CI and pre-deployment isolated migration/recovery are required. This increment
changes a private write, erasure/export coverage and database grants; it therefore
uses full CI under the existing policy. Routine catalog-only changes retain their
focused path. No production cutover, release tag, new email or push test is implied.
