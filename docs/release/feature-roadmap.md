# Next feature milestones

Proposed order as of 2026-10-31. These are feature goals, not fixed delivery dates.
Routine increments continue with targeted local checks and commits. Catalog
expansion and exact-cultivar photography continue alongside the feature work.

## Delivery checkpoint before the next persistent feature

The current local batch adds upcoming Garden work, related-variety browsing,
distinct cultivar photos, photo-status filtering, direct care comparisons,
growing/harvest comparisons and shareable public comparisons. Current local
totals are 257 reviewed entries and 115 cultivars; staging remains at the earlier
accepted release. The 77 unresolved cultivar photos remain visibly pending.

At the next accumulated-batch checkpoint, select the committed candidate, run one
full CI validation for the combined changes (including earlier test-policy edits),
exercise the real backend/catalog/browser integration, verify existing data and
service-worker upgrades, and complete the staging deployment/health/backup gates.
This checkpoint is deferred, not represented as already passed. Do not start
routine pipelines for each catalog batch.

## 1. Saved plant shortlists

Save catalog plants the owner is considering, separately from plants already
owned. Start in the existing catalog/Garden navigation rather than adding a tab
for a small list. Support add/remove, reopening a comparison and later choosing
to add a real plant. Do not create care schedules simply by shortlisting.
Decide device-local versus account-synced persistence before implementation;
account storage needs ownership isolation and deletion coverage.

## 2. Personal growth photo journal

Add dated photos and short notes to an owned plant, with a chronological view.
Design storage limits, image validation/resizing, private access, deletion and
backup behavior before enabling uploads on the small VPS. Strip unnecessary
photo metadata. Do not expose these photos through public comparison links.
Make the first version useful without a paid image service.

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
