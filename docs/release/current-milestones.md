# Current milestone plan

Reviewed 2026-10-24. This is the current work order; dated execution records retain
their original candidate and scope. No v1 release or production cutover is created
by this plan. The public contact-address decision remains deferred by the owner.

## Completed staging milestones

| Milestone | Accepted evidence | Limits |
| --- | --- | --- |
| Core care and Android beta | [Public browser journey](public-browser-acceptance.md), [Android beta](android-beta.md), owner-confirmed reminders, multiple care cycles, Dashboard taps, installation, offline/reconnect and update with data intact | Other physical devices and assistive technologies need their own acceptance. |
| English catalog and images | [20 reviewed houseplants](flowering-houseplant-batch.md), licensed cover photos, [catalog browsing](catalog-browsing.md), [16 familiar-name aliases](common-name-aliases.md), [eight growth references for four species](growth-references.md) | A starter catalog; examples show different specimens, without a growth timetable or diagnosis. Personal photo uploads are future work. |
| Care-action reminder pictures | [Five cartoon-style action icons](care-notification-icons.md), public image hashes and offline coverage | Installed Android rendering of these new drawings is still owner-unconfirmed; older plant-photo confirmation does not cover them. |
| Sharing emails and signup invitations | [Calendar-sharing acceptance](calendar-sharing-email.md): real Gmail inbox delivery, read-only access/revocation, isolated new-account signup and confirmation tests | Seven-day invitations; SMTP acceptance is not an inbox guarantee. No Google Calendar synchronization is enabled. |
| Account controls and operations | [Account export/deletion](account-data.md), [retention and erasure-safe restore](retention-and-erasure-recovery.md), [VPS recovery](vps-application-recovery.md), [monitoring](worker-capacity.md) | Backups require the owner's PC/drive/access. Weekly email checks are not independent continuous monitoring. Replacement-host recovery remains open. |

Latest deployed application: `6d6d4d52fa6231d335b9b22705f4984ca14c5c76`, with
[successful exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/40032318279).
Its staging checks preserved personal data and catalog images, exercised the real
frontend update, and finished with healthy local/VPS backup checks. Documentation
commits after that SHA do not imply another deployment or new health check.

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

Feature work may continue during the deferred publication decisions, as the owner
requested. Keep additions small and preserve the catalog/source/image validation,
account isolation and update checks. The next content increment can expand curated
growth references for existing species through the established attribution process.
Additional species follow the same reviewed import path. A personal photo journal
requires a separate upload/storage/privacy/deletion design before implementation.
Common-name-first labels and action-icon reminders are already implemented; do not
re-add them as future milestones. The application and new content remain English.
