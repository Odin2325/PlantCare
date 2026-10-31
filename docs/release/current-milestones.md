# Current milestone plan

Reviewed 2026-10-30. This is the current work order; dated execution records retain
their original candidate and scope. No v1 release or production cutover is created
by this plan. The public contact-address decision remains deferred by the owner.

Owner direction on 2026-10-24 prioritizes catalog expansion and the recommended
features. [Pet-aware catalog browsing](pet-aware-catalog.md),
[reminder snoozing](reminder-snoozing.md) and the
[20-plant expansion](houseplant-expansion.md) are deployed to staging. Release
work below remains tracked alongside it.

[Plant comparison and the 15-plant batch](plant-comparison.md) are also deployed,
bringing the reviewed catalog to 58 entries while retaining all existing records.
The following [growth-reference and ten-plant expansion](growth-reference-expansion.md)
brought that batch to 68 plants and sixteen references across eight species.
The [growth-photo discovery increment](growth-photo-discovery.md) adds a combined
gallery filter and ten more plants, bringing that reviewed total to 78. The following
[flowering-reference increment](flowering-reference-expansion.md) adds five plants
and two galleries, bringing that reviewed total to 83. The subsequent
[pet-assessment increment](pet-assessment-expansion.md) adds an explicit unknown
filter, eight species and two galleries, bringing that total to 91. The next
[collection-care increment](collection-care-filter.md) adds active collection care
filters and nine species, bringing that reviewed total to 100. The following
[care-history increment](care-history-discovery.md) adds history discovery and five
plants, bringing that reviewed total to 105. The [Garden and cultivar increment](garden-catalog-expansion.md) adds 47 entries, bringing that total to 152. The [planting-plan increment](planting-plan.md) adds twelve entries, bringing that total to 164. The [planting-calendar increment](planting-calendar.md) adds eight entries, bringing that total to 172. The [harvest-record increment](harvest-records.md) adds eight entries, bringing that total to 180. The [harvest-overview increment](harvest-overview.md) adds twelve cultivars, bringing that total to 192. The [harvest-trends increment](harvest-trends.md) adds eighteen cultivars, bringing that total to 210. The [seasonal-care increment](seasonal-care-preferences.md) adds twelve vegetable cultivars, bringing that total to 222. The [care-schedule preview increment](care-schedule-preview.md) adds eight fruit cultivars, bringing the current total to 230.

## Completed staging milestones

| Milestone | Accepted evidence | Limits |
| --- | --- | --- |
| Core care and Android beta | [Public browser journey](public-browser-acceptance.md), [Android beta](android-beta.md), owner-confirmed reminders, multiple care cycles, Dashboard taps, installation, offline/reconnect and update with data intact | Other physical devices and assistive technologies need their own acceptance. |
| English catalog and images | [230 reviewed entries](care-schedule-preview.md), houseplants/crops/trees and 88 named cultivars, category tabs, 156 licensed originals, 262 aliases across 131 entries, thirteen sourced cat-and-dog classifications, [catalog browsing](catalog-browsing.md), [24 growth references for twelve species](pet-assessment-expansion.md) | A curated selection; examples show different specimens, without a growth timetable or diagnosis. Personal photo uploads are future work. |
| Garden and cultivar browsing | [Garden tab and category navigation](garden-catalog-expansion.md), saved-location groups, nickname-first cards, next care, harvest guidance and personal growing-guide links; live future-only fixture and data preservation | Read-only garden overview; bed planning and harvest summaries remain later features. Generic cultivar photos are explicitly labelled. |
| Planting calendar | [Monthly target-date agenda](planting-calendar.md), date-only month/year navigation, nickname/action links, shared Garden filters and phone reflow | Saved targets only; viewing the calendar does not complete care, record a harvest or send reminders. |
| Actual harvest records | [Private date/quantity/notes log](harvest-records.md), create/read/confirmed removal, archived retention, export/erasure and migrated recovery | Latest 100 displayed; older records remain in export. Units are not summed and harvesting does not complete care or change target dates. |
| Care-schedule preview | [Non-persisting server calculation and explicit application](care-schedule-preview.md), interval/weekday/pause controls, DST and travelling-zone checks, cancel/retry and live future-care fixture | Estimated due date recalculates against latest history on apply; provider-queued messages cannot be recalled. |
| Seasonal care preferences | [Saved environment and manually selected season](seasonal-care-preferences.md), owner/CSRF protection, retry/clear controls, private export, archived retention and migrated recovery | General review guidance; preferences do not infer weather or change schedules automatically. |
| Personal planting plans | [Optional sowing, planting-out and expected-harvest dates](planting-plan.md), owner/antiforgery protection, private export, Garden filters and migrated recovery | User-entered targets; no automatic maturity forecast, shared-calendar event, reminder or completed harvest is created. |
| Collection care filtering | [Needs care, Upcoming and No active due dates](collection-care-filter.md), combined search/location/tag filters, result count, clearing, minute clock and rollover/disposal tests; real care-completion and existing-session update checks | Uses the device calendar day, matching collection badges. Filters are local view controls and do not change reminders or saved care. |
| Care history discovery | [Action, completion-day and notes filters](care-history-discovery.md), newest/oldest order, clearing and edit-draft controls; read-only live acceptance and preserved history | Filters apply to the latest 100 loaded entries, with an explicit limit. Older records remain saved. Dates use the device time zone. |
| Plant comparison | [Compare up to three catalog plants](plant-comparison.md), preserved URL selection/filters, credited photos/care sources and explicit unknown pet assessment; 27 public catalog/comparison checks | Public species comparison, without suitability scores or personal-plant data sharing. Room conditions and actual moisture still need the owner's judgment. |
| Reminder snoozing | [Server-persisted 1-hour, 24-hour and 3-day follow-ups](reminder-snoozing.md), unchanged care due/history, SQL concurrency/cancellation tests and live 24-hour form/reload checks | Installed-phone delivery of a delayed follow-up remains owner-unconfirmed. Provider-queued messages cannot reliably be recalled. Compatible forward recovery is required after the migration. |
| Care-action reminder pictures | [Five cartoon-style action icons](care-notification-icons.md), public image hashes and offline coverage | Installed Android rendering of these new drawings is still owner-unconfirmed; older plant-photo confirmation does not cover them. |
| Sharing emails and signup invitations | [Calendar-sharing acceptance](calendar-sharing-email.md): real Gmail inbox delivery, read-only access/revocation, isolated new-account signup and confirmation tests | Seven-day invitations; SMTP acceptance is not an inbox guarantee. No Google Calendar synchronization is enabled. |
| Account controls and operations | [Account export/deletion](account-data.md), [retention and erasure-safe restore](retention-and-erasure-recovery.md), [VPS recovery](vps-application-recovery.md), [monitoring](worker-capacity.md) | Backups require the owner's PC/drive/access. Weekly email checks are not independent continuous monitoring. Replacement-host recovery remains open. |

Latest deployed application: `07848557db0ac3cc43f3d0c839a2af64de690138`, with [successful full CI](https://github.com/Odin2325/PlantCare/actions/runs/41040578725). [Care-schedule preview acceptance](care-schedule-preview.md) covers non-persisting server previews, explicit interval/weekday/pause confirmation, preserved care/history, the catalog increment, existing-worker update and refreshed backups. Documentation commits after that SHA do not imply another runtime deployment.

## Remaining work, in order

1. **Finish artifact notice packaging.** Continue the
   [dependency review](dependency-notices.md) against the actual current API/worker
   manifests, carry the required backend notices, and account for container/vendor
   notices separately. Verify the resulting exported artifacts. Frontend notice
   packaging is implemented; a package inventory alone does not close this work.
2. **Accept the private calendar feed in a real client.** Choose a calendar client
   the owner uses, import only a dedicated synthetic account's feed, and verify
   updates plus rotation/revocation. Keep feed URLs private. HTTP/parser tests and
   email sharing do not substitute for client acceptance; provider synchronization
   stays disabled. Collect the care-icon result from the next natural Android
   reminder, without sending unnecessary extra pushes.
3. **Close the remaining operations scope.** Decide the supported recovery promise
   and test a replacement host if that promise requires it. Document independent
   account-erasure authority, backup availability and capacity limits before
   claiming them solved. Add paid infrastructure only with the owner's agreement.
4. **Finalize publication decisions when the owner resumes them.** Complete the
   privacy/provider/retention review and legal notice. Do not infer or publish an
   address. This remains a public-launch gate, not a reason to stop authorized
   staging or feature work. Advertising requires its own later review.
5. **Select and verify a release candidate.** Reconcile the full
   [acceptance checklist](v1-acceptance.md), make explicit decisions on unsupported
   optional capabilities, run exact-candidate CI and staging acceptance, then
   finalize artifacts, upgrade/recovery instructions and release notes. Create a
   v1 tag and production cutover only after the remaining launch gates close.

## Feature lane

[Garden and cultivar browsing](garden-catalog-expansion.md), [personal planting plans](planting-plan.md) and the [monthly planting calendar](planting-calendar.md) are accepted on staging. The catalog now has 230 entries and 88 cultivars. [Care-schedule previews](care-schedule-preview.md) show recurrence and estimated due-date impact before explicit application. [Seasonal care preferences](seasonal-care-preferences.md) save the owner-selected environment and season with general review prompts, preserving current care schedules. [Harvest trends](harvest-trends.md) compare recorded quantities by month and plant, including empty months and the existing Garden filters. The [Garden harvest overview](harvest-overview.md) adds date and collection filters with exact, separate quantity totals. Actual harvest records are also accepted on personal plant details, separately from target dates and care. Plan dates are editable on personal details and visible/filterable in Garden and its calendar. The migration/private write received full CI; catalog-only changes retain focused validation.

Feature work may continue during the deferred publication decisions, as the owner
requested. Keep additions small and preserve the catalog/source/image validation,
account isolation and update checks. The [upcoming-work view](garden-upcoming-work.md) and eight more cultivars are implemented locally for the next batch checkpoint. [Related-cultivar discovery and source-specific photos](related-cultivar-photos.md) are now also implemented locally. Continue the pending photo queue before the next accumulated-batch checkpoint.
Additional species follow the same reviewed import path. A personal photo journal
requires a separate upload/storage/privacy/deletion design before implementation.
Common-name-first labels and action-icon reminders are already implemented; do not
re-add them as future milestones. The application and new content remain English.

## Delivery cadence (owner direction, 2026-10-30)

Routine feature increments use local commits and only the targeted tests necessary
for their changes. Accumulate a meaningful batch before pushing, running the
broader pipelines and deploying to staging, or do so when the owner requests a
checkpoint. Do not push or redeploy merely to publish acceptance notes. Existing
CI scope rules continue to apply when pipelines run; sensitive changes and release
checkpoints still receive full validation. Finish post-deployment verification and
backups for any deployment already in progress before returning to local work.

## Local batch awaiting the next checkpoint

[Garden upcoming work](garden-upcoming-work.md) combines saved planting targets
and next scheduled care, with date/type/past-item controls, shared Garden filters
and nickname-first detail links. Eight cultivars bring the local reviewed dataset
to 238 entries and 96 cultivars. The deployed totals remain 230 and 88 until the
batch is integrated. Targeted local validation accompanies the local commit;
full CI, public acceptance, update checks and deployment wait for the larger
checkpoint. The latest deployed application above remains unchanged.

The local [related-cultivar/photo increment](related-cultivar-photos.md) adds
context-preserving profile links and 30 licensed assets, bringing the working
image manifest to 186 files. Of the 96 cultivars, 31 have distinct source-identified
photos and 65 use the approved named pending placeholder. Generic images are no
longer repeated on cultivar entries. This increment and its CI-path updates are
local-only; the next checkpoint must run full validation for the accumulated
policy/workflow changes before deployment.

The local [cultivar-photo filter](cultivar-photo-filter.md) adds URL-preserved
photo/pending browsing with the existing filters and comparison selection. One
further reviewed Sweet Dumpling photo brings the working totals to 187 assets,
32 cultivars with distinct photos and 64 pending. Reviewed entry/cultivar totals
remain 238/96. This increment also awaits the batch checkpoint.
