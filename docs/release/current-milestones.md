# Current milestone plan

Reviewed 2026-10-25. This is the current work order; dated execution records retain
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
plants, bringing the current reviewed total to 105.

## Completed staging milestones

| Milestone | Accepted evidence | Limits |
| --- | --- | --- |
| Core care and Android beta | [Public browser journey](public-browser-acceptance.md), [Android beta](android-beta.md), owner-confirmed reminders, multiple care cycles, Dashboard taps, installation, offline/reconnect and update with data intact | Other physical devices and assistive technologies need their own acceptance. |
| English catalog and images | [105 reviewed houseplants](care-history-discovery.md), licensed cover photos, 179 aliases across 90 entries, thirteen sourced cat-and-dog classifications, [catalog browsing](catalog-browsing.md), [24 growth references for twelve species](pet-assessment-expansion.md) | A curated selection; examples show different specimens, without a growth timetable or diagnosis. Personal photo uploads are future work. |
| Collection care filtering | [Needs care, Upcoming and No active due dates](collection-care-filter.md), combined search/location/tag filters, result count, clearing, minute clock and rollover/disposal tests; real care-completion and existing-session update checks | Uses the device calendar day, matching collection badges. Filters are local view controls and do not change reminders or saved care. |
| Care history discovery | [Action, completion-day and notes filters](care-history-discovery.md), newest/oldest order, clearing and edit-draft controls; read-only live acceptance and preserved history | Filters apply to the latest 100 loaded entries, with an explicit limit. Older records remain saved. Dates use the device time zone. |
| Plant comparison | [Compare up to three catalog plants](plant-comparison.md), preserved URL selection/filters, credited photos/care sources and explicit unknown pet assessment; 27 public catalog/comparison checks | Public species comparison, without suitability scores or personal-plant data sharing. Room conditions and actual moisture still need the owner's judgment. |
| Reminder snoozing | [Server-persisted 1-hour, 24-hour and 3-day follow-ups](reminder-snoozing.md), unchanged care due/history, SQL concurrency/cancellation tests and live 24-hour form/reload checks | Installed-phone delivery of a delayed follow-up remains owner-unconfirmed. Provider-queued messages cannot reliably be recalled. Compatible forward recovery is required after the migration. |
| Care-action reminder pictures | [Five cartoon-style action icons](care-notification-icons.md), public image hashes and offline coverage | Installed Android rendering of these new drawings is still owner-unconfirmed; older plant-photo confirmation does not cover them. |
| Sharing emails and signup invitations | [Calendar-sharing acceptance](calendar-sharing-email.md): real Gmail inbox delivery, read-only access/revocation, isolated new-account signup and confirmation tests | Seven-day invitations; SMTP acceptance is not an inbox guarantee. No Google Calendar synchronization is enabled. |
| Account controls and operations | [Account export/deletion](account-data.md), [retention and erasure-safe restore](retention-and-erasure-recovery.md), [VPS recovery](vps-application-recovery.md), [monitoring](worker-capacity.md) | Backups require the owner's PC/drive/access. Weekly email checks are not independent continuous monitoring. Replacement-host recovery remains open. |

Latest deployed application: `cddefa4b1cddd5e21a47fca3f969dfb68615581c`, with
[successful exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40211806128).
Its staging checks accepted the frontend update and collection-care filters,
preserved personal data and catalog images, and finished with healthy local/VPS
backup checks. Documentation commits after that SHA do not imply another
deployment or new health check.

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

[Garden crops and universal cultivars](garden-catalog-expansion.md) are prepared
in the workspace: 152 entries, a Garden tab, category navigation/entry-type filters and growing/harvest
guides. Focused native checks passed after approval review resumed. Browser checks,
full exact-candidate CI for the testing policy and deployment are pending. The accepted staging catalog remains at 105; do not treat this preparation
as a deployed milestone. The same record describes the focused-test path.

Feature work may continue during the deferred publication decisions, as the owner
requested. Keep additions small and preserve the catalog/source/image validation,
account isolation and update checks. The next content increment can expand curated
growth references for existing species through the established attribution process.
Additional species follow the same reviewed import path. A personal photo journal
requires a separate upload/storage/privacy/deletion design before implementation.
Common-name-first labels and action-icon reminders are already implemented; do not
re-add them as future milestones. The application and new content remain English.
