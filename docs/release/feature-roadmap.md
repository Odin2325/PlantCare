# Next feature milestones

Proposed order as of 2026-10-31. These are feature goals, not fixed delivery dates.
Routine increments continue with targeted local checks and commits. Catalog
expansion and exact-cultivar photography continue alongside the feature work.

## Delivery checkpoint completed

The [2026-10-31 combined staging checkpoint](catalog-batch-checkpoint.md) passed
full CI, real public/catalog/image checks, existing-worker update, pinned
activation and fresh pre/post backups. Current staging totals are 257 reviewed
entries and 115 cultivars, with 38 distinct cultivar photos and 77 pending.
No schema changed. The preparation outage and interrupted-backup recovery
follow-up are recorded in that acceptance document.

[Saved shortlists](saved-shortlists.md) are now implemented locally, awaiting their
full private-data/migration checkpoint. The [photo-journal design](photo-journal-design.md)
is complete; the [normalizer prototype](photo-normalization.md) and dependency
review are implemented locally. The [standalone process boundary](photo-process-isolation.md)
has passed focused Linux acceptance. The [private transport](photo-worker-connection.md)
has passed client/gateway and end-to-end acceptance, with uploads disabled. Next
is private journal storage and lifecycle handling. Broader pipelines/deployment wait for the next meaningful combined
checkpoint; routine increments retain targeted local checks and commits.

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
