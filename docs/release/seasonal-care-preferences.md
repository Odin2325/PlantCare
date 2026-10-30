# Seasonal care preferences

Prepared 2026-10-30. Deployment acceptance pending.

Personal plant details now save an optional growing environment (indoor, balcony,
greenhouse or outdoor) and a manually chosen care season (spring, summer, autumn
or winter). General review prompts follow those choices, with linked RHS watering
guidance and a link to the existing schedule controls in My plants. Choices are
not inferred from a location, calendar month or hemisphere. No fixed care interval
is prescribed. Saving or clearing these preferences does not change care due
dates, intervals, history, reminders, notes or planting plans.

Preferences belong to the plant owner. Archived plants keep readable context and
require restoration before edits. Private export includes both fields; account
deletion and restored-account erasure protections continue to cover the plant.
The additive migration adds two nullable bounded strings to UserPlants. Existing
rows stay unset. Runtime grants require no expansion. Use compatible forward
recovery; do not downgrade the schema to recover an application activation.

## Catalog increment

Twelve cultivars extend potatoes (Desiree, Maris Piper), carrots (Adelaide,
Flyaway), beetroot (Pablo, Forono), radishes (Scarlet Globe, Amethyst), kale
(Nero di Toscana), cauliflower (Clapton), spinach (Lazio) and peas (Jaguar).
The reviewed catalog totals 222 entries and 80 cultivars, preserving all previous
210 entries. The 156 licensed photo originals remain unchanged, and all new
cultivar photos are labelled species references. Primary source links are stored
per entry; UK guidance does not establish fixed German sowing or harvest dates.

## Acceptance plan

Focused domain, owner/cookie/CSRF/export/deletion, component, catalog import and
browser checks verify persistence, clearing, failed drafts, archive behavior and
unchanged care. The migration and private write select full CI. Before staging
activation, require a fresh encrypted backup, isolated recovery of the exact
candidate including preference persistence/export and failed-activation survival,
then existing-worker update and dedicated synthetic-account live acceptance.
Deployment, recovery and final backup health are not yet accepted by this file.
