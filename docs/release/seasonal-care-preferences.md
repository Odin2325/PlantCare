# Seasonal care preferences

Deployed and accepted on staging 2026-10-30 at `3d3237745496b1e85ff20ee3bd2e14d86ca651ca`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/41028853215) passed before activation.

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

## Acceptance scope

Focused domain, owner/cookie/CSRF/export/deletion, component, catalog import and
browser checks verify persistence, clearing, failed drafts, archive behavior and
unchanged care. The migration and private write select full CI. Before staging
activation, require a fresh encrypted backup, isolated recovery of the exact
candidate including preference persistence/export and failed-activation survival,
then existing-worker update and dedicated synthetic-account live acceptance.
The completed evidence and its limits are recorded below.

## Staging acceptance

Focused local checks passed: one domain test, twelve preference/account tests,
seven catalog import checks, seven frontend tests, eighteen focused catalog/Garden
browser checks and one affected core journey. Phone/desktop visual inspection,
accessibility and mobile reflow passed. Full CI passed 203 backend tests, 72
frontend tests, the SQL Server security/runtime checks and all 63 browser/PWA
cases. No full local suite was repeated.

A fresh encrypted VPS snapshot was restored into a separate local Docker project.
The exact candidate applied the additive migration after fresh erasure-authority
reconciliation. It saved and exported preferences, retained planting/harvest data,
worker deduplication and care history, then survived an intentionally failed API
activation with preferences/session/history/key hashes intact. This does not
establish replacement-host recovery.

Eight public catalog/Garden cases passed initially. The long Firefox profile
probe hit an image wait. Strengthened full-transfer waiting got past that image,
but the expanded remote scenario reached its overall time budget. A focused
public Firefox repeat passed for all twelve new cultivars plus three regression
profiles with the stronger image-completion check and unchanged per-assertion
timeouts. All 222 reviewed
profiles and 156 image hashes matched, preserving all 211 prior public records.
The catalog image assertion was strengthened to wait for a complete transfer
before navigating away. Its test-only follow-up at
`9bd4743718175865eea12e6e96a59233e0f82084` passed
[CI](https://github.com/Odin2325/PlantCare/actions/runs/41036384959); it required
no application redeployment. The existing-worker update retained the approved
synthetic account session,
collection IDs, completed history and cached care-action icons.

The HTTP-failure simulation used direct browser routing with service workers
blocked; the real existing-worker update was verified separately. Live acceptance
used only a uniquely named future-care test plant on the approved
synthetic account. Save/reload, failed-draft retry, clear-then-save and private
export passed. Schedules, due dates, history, notes and planting plans stayed
unchanged. The fixture was archived; its preferences remained readable without
editable controls, and the original collection/history were preserved.

SSH access was restored through the owner's scoped UFW source rule. Fresh local
and VPS encrypted backups completed through their existing scheduled tasks after
stale/failed attempts. An interrupted empty capture directory was preserved
privately rather than deleted. The VPS task's console launch was made hidden;
its trigger, principal and backup/retention settings were retained. Verified
pre/post-deployment backups and final local/VPS health/task checks passed.

A post-deployment attempt encountered an external-drive I/O error during the
read-only repository listing. After the owner reconnected the drive, the repository
was readable again and the existing VPS task completed a new candidate backup
and integrity check. Both scheduled tasks finished successfully and both default
backup-health checks were healthy. No repository repair or deletion was used.
