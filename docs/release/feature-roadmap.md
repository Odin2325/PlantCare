# Next feature milestones

Proposed order as of 2026-11-04. These are feature goals, not fixed delivery dates.
Routine increments continue with targeted local checks and commits. Catalog
expansion and exact-cultivar photography continue alongside the feature work.

## Delivery checkpoint completed

The [2026-10-31 combined staging checkpoint](catalog-batch-checkpoint.md) passed
full CI, real public/catalog/image checks, existing-worker update, pinned
activation and fresh pre/post backups. Current staging totals are 257 reviewed
entries and 115 cultivars, with 38 distinct cultivar photos and 77 pending.
No schema changed. The preparation outage and interrupted-backup recovery
follow-up are recorded in that acceptance document.

[Saved shortlists](saved-shortlists.md) and the mobile redesign passed the
[2026-11-04 staging checkpoint](mobile-checkpoint-acceptance.md), including full CI,
migrated recovery and a preserved existing browser session. The [photo-journal design](photo-journal-design.md)
is complete; the [normalizer prototype](photo-normalization.md) and dependency
review are implemented locally. The [standalone process boundary](photo-process-isolation.md)
has passed focused Linux acceptance. The [private transport](photo-worker-connection.md)
has passed client/gateway and end-to-end acceptance, with uploads disabled. Next
is the combined photo-journal checkpoint: [storage/lifecycle](photo-journal-storage.md)
and [gated uploads/UI](photo-journal-upload-ui.md) are implemented locally. Broader pipelines/deployment wait for the next meaningful combined
checkpoint; routine increments retain targeted local checks and commits.

The next local batch includes [Apple compatibility and store preparation](apple-and-store-readiness.md):
safe-area and landscape refinements, reliable unsupported-push guidance, focused
iPhone/iPad WebKit checks and a separate physical-device acceptance list. Store
packaging/betas follow as their own milestones; no small-change deployment or
developer enrollment is bundled into this work.

The following local slice prepares the [separate photo-processing bundle](photo-processing-bundle.md)
from committed source and verifies the production-style socket UID/GID and
read-only API mount. [Real HTTP integration](photo-http-acceptance.md) now passes
locally. Uploads stay off; capacity and host activation acceptance come before the
next combined deployment.

[Local capacity samples](photo-capacity.md) now pass, including a budgeted real
HTTP lifecycle and 20MP decoder sample. Live settings remain unchanged. The next
activation gate is recovery/log-growth policy plus actual host/mixed-workload
acceptance; these samples do not enable uploads or trigger a deployment.

The [snapshot-only recovery option](snapshot-recovery-policy.md) now passes its
local rehearsal, including photo-bearing restore and existing-log-chain refusal.
The [host preparation sequence](photo-host-preparation.md) is ready for review at
the combined checkpoint. Live recovery settings and uploads remain unchanged.

The [gateway restart slice](photo-gateway-restarts.md) now passes local lock,
pending-state, graceful-stop, forced-loss and HTTP lifecycle checks. Actual host
installation and systemd/reboot checks remain for the reviewed batch window.

The [mixed local sample](photo-mixed-workload.md) now passes for 100 reminders,
repeated uploads/exports and overlapping SQL backups under the proposed memory
caps. The [large-input/account-export sample](photo-large-account.md) now also
passes for 8-MiB/20-MP inputs and a 38-photo ZIP at about 99% account byte usage.
The [encrypted local recovery sample](photo-encrypted-recovery.md) now also passes
for SQL/key capture, a fresh repository and verified SQL/media restore. Next are
diverse-media sizing and actual host acceptance, followed by the combined
deployment gates. The [local restored-app check](photo-restored-app.md)
now passes key-dependent sessions, private reads/export, account erasure and
reconciliation of that erasure before an old-snapshot app restart.

The [selected HTTP format/count check](photo-formats-counts.md) now passes JPEG
orientation, PNG/static-WebP alpha and metadata handling, bounded rejection and
independent 20/50-entry limits with a verified 50-photo export. Consolidate the
local photo/Apple work into the combined candidate after host/native-notice
prerequisites; do not deploy each test increment. Physical Apple checks remain
separate until a device is available.

The [host/artifact preflight](photo-candidate-preflight.md) now supplies repeatable
read-only host facts and exact native/OS/framework notice evidence. Linux-x64
decoder publishing removes unused platform assets while retaining the identical
Linux library and notice archive; the disabled bundle and focused HTTP lifecycle
pass. Finish the remaining native terms/source mapping and reviewed host gates
before the combined CI/recovery/deployment checkpoint.

## 1. Saved plant shortlists

Save catalog plants the owner is considering, separately from plants already
owned. Start in the existing catalog/Garden navigation rather than adding a tab
for a small list. Support add/remove, reopening a comparison and later choosing
to add a real plant. Do not create care schedules simply by shortlisting.
Account-synced persistence was selected and implemented, with ownership isolation,
export, deletion and recovery tests. See [local acceptance](saved-shortlists.md).

## 2. Personal growth photo journal

Add dated photos and short notes to an owned plant, with a chronological view.
Design storage limits, image validation/resizing, private access, deletion and
backup behavior before enabling uploads on the small VPS. Strip unnecessary
photo metadata. Do not expose these photos through public comparison links.
Make the first version useful without a paid image service.

The [implementation design](photo-journal-design.md) selects bounded private SQL
image storage for the small beta, so images and metadata share transactional
deletion and existing database capture. It defines quotas, normalization, private
routes, account export and recovery gates. No uploads or new dependencies are
enabled by this design. Start with an isolated normalizer; do not expose upload
routes until decoding resource bounds, ownership, deletion/export and migrated
backup/recovery have been verified.

## 3. Healthy growth-stage reference library

Expand the existing reference-photo examples into useful seedling, established,
flowering and fruiting stages for common crops where appropriate. Use licensed,
identified photographs and clearly describe example conditions. These are
reference examples, not a promise of identical growth or a disease diagnosis.
Keep cultivar-photo gaps explicit; do not substitute a generic image as an exact
cultivar photograph.

## 4. Succession planting and crop batches

Extend the existing planting targets/calendar with separate sowing batches for
the same cultivar, repeat-sowing planning and actual progress dates. Keep target
dates distinct from completed actions and recorded harvests. Show each batch's
next action in Garden without duplicating existing care reminders. Use the
owner's growing context and local frost judgement, not universal German dates.

## Indoor and outdoor space planner

Garden includes a first planner for saved indoor and outdoor layouts. Add square,
rectangular or circular boundaries, obstacles and observed light zones. Place pots
on the map or enter grid coordinates. The planner can suggest the nearest free spot
where a pot fits within a marked zone and matches the catalog light for each plant.

Shared-pot checks compare catalog light and temperature ranges alongside the soil
moisture preferences entered by the user. A possible match still needs a check of
mature size, potting mix, drainage and root space. The catalog supplies care notes
and a light filter for choosing candidates. Layouts are saved per account in the
current browser. The existing photo journal release checks remain a separate track.

## Later candidates and release boundary

After these milestones, evaluate a structured plant-health observation log and
care-history insights using real user feedback. Avoid automatic treatment claims
or paid weather/AI dependencies in the initial version. Common-name-first labels,
care-action notification icons, seasonal preferences, harvest records/trends and
the current calendar already exist and are not new milestones.

Public release still requires the remaining gates in
[current milestones](current-milestones.md). The owner's deferred publication
decisions remain deferred. Advertising/monetization is a later independent step,
not bundled into the next feature batch.
