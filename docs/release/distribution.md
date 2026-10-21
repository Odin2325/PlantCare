# v1 distribution preparation

Reviewed 2026-10-21. No v1 tag, GitHub release or production launch is created by
this preparation. The staging runtime is `677b91a0083cdfec6e5f2656a82c9594e49d07b0`;
subsequent documentation commits are not new application deployments.

## Documentation prepared

- [User guide](../user-guide.md): accounts, first plant, daily care, nickname
  calendar links, notifications, Android install/update, offline and sharing.
- [Support](../../SUPPORT.md): GitHub Issues for general support, safe report
  contents and the owner-authorized Gmail address for private requests.
- [Known limitations](../known-limitations.md): actual beta/device/content and
  operations scope, separate from planned features.
- [Privacy review](privacy-review.md): implementation facts and decisions/work
  required before publishing a complete notice, with a concrete
  [notice draft](../privacy-notice.draft.md),
  [private-request procedure](privacy-requests.md) and
  [provider checklist](provider-privacy-checklist.md). Operator name/city/country
  are owner-approved; service address and provider settings remain open.
- [Operator notice draft](../legal-notice.draft.md): approved identity/contact
  separated from missing postal address and service-specific legal review.
- [Privacy-request tabletop](privacy-request-tabletop.md): eight fictional
  decision scenarios reviewed without sending messages or modifying accounts;
  owner-led operational acceptance remains open.
- [LICENSE](../../LICENSE): owner chose rights reserved on 2026-10-21; no
  open-source grant. Third-party license/notice review remains part of packaging.
- [Third-party notices](../../THIRD_PARTY_NOTICES.md) and
  [dependency review](dependency-notices.md): corrected frontend-notice packaging
  verified in a rebuilt image, plus a 29-package shipped NuGet inventory. Backend,
  container and full distribution review remain open; this correction is not yet live.

## Proposed release notes (draft)

PlantCare brings together a personal plant collection, tags, care schedules,
Dashboard completion, care history, calendar views and reminders. Calendar events
use plant nicknames and care actions and link to individual plant details. An
Android-installed app can update without reinstallation and shows an offline
explanation when disconnected. Account confirmation and password recovery use
transactional email.

The current beta also includes private calendar feeds and read-only account
sharing. Their remaining acceptance checks must pass before advertising them as
accepted launch capabilities. Google/Microsoft synchronization stays disabled.
Real plant content and images must not be implied by screenshots or release notes
while the catalog is synthetic. See the known-limitations document.

## Before a release tag

1. Complete operator identity/privacy disclosures and remaining log/provider/
   support/marker retention criteria. Guarded encrypted-backup expiry and managed
   erasure reconciliation are verified in [retention-and-erasure-recovery.md](retention-and-erasure-recovery.md).
   Independent current-authority replication and replacement-host acceptance are
   still open; missing authority must not be bypassed during recovery.
2. Replace demo catalog content with reviewed, attributed real species through
   the controlled import procedure. Thumbnails/growth images remain later work.
3. Reconcile each acceptance gate with its recorded evidence, execute remaining
   required checks (including real calendar client and wider usability scope),
   and decide explicitly whether to disable any unaccepted optional capability.
4. Finish third-party distribution/license notices for the actual frontend,
   backend and container artifacts. Do not assume the project's rights-reserved
   notice covers bundled dependencies or future botanical images/content.
5. Select a final committed application candidate. Pass CI for that exact SHA,
   backup, deploy to staging, verify the real frontend update and run the required
   public acceptance. Keep evidence tied to that SHA; retain its pinned bundle.
6. Finalize version/release notes, upgrade/rollback instructions, known issues
   and artifact hashes. Tag the accepted commit as v1.0.0 only after the remaining
   gates and launch decisions are closed. Then prepare the production cutover.

Owner confirmation of working reminders and multiple normal care cycles on
2026-10-21 is accepted as owner-reported; it does not need to be repeated simply
because individual timestamps were not supplied. Fresh local/VPS backups and
their health checks passed that day after reconnecting the external drive.
