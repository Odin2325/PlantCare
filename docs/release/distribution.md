# v1 distribution preparation

Reviewed 2026-10-24. No v1 tag, GitHub release or production launch is created by
this preparation. The staging runtime is `7ce5a11df523ce38c094fbe48e6395f05fa38f27`;
subsequent documentation commits are not new application deployments.
See the [current milestone plan](current-milestones.md) for the remaining work order.

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
  container and full distribution review remain open. The frontend packaging
  correction is included in the deployed English catalog candidate.

## Proposed release notes (draft)

PlantCare brings together a personal plant collection, tags, care schedules,
Dashboard completion, care history, calendar views and reminders. Calendar events
use plant nicknames and care actions and link to individual plant details. An
Android-installed app can update without reinstallation and shows an offline
explanation when disconnected. Account confirmation and password recovery use
transactional email.

The current beta also includes private calendar feeds and read-only account
sharing. Sharing now sends transactional emails and seven-day signup invitations;
matching confirmed accounts can accept, and owners can resend or revoke. Real Gmail
inbox delivery and live read-only access/revocation are verified in
[calendar-sharing acceptance](calendar-sharing-email.md). Private feeds still need
real calendar-client acceptance. Google/Microsoft synchronization stays disabled.
The beta includes 68 reviewed English houseplant entries and licensed local
reference photos. Eight species also have sixteen curated growth-feature examples
of different specimens, without timelines or diagnosis; personal photo journals
remain future work. See [growth references](growth-references.md),
[the catalog record](houseplant-starter-catalog.md) and the
known-limitations document. The five [flowering additions](flowering-houseplant-batch.md)
include seasonal care guidance and individual photo credits.
Catalog browsing now includes compact photo cards, light filtering, name sorting
and search state retained through details and reload; see
[catalog browsing acceptance](catalog-browsing.md).
The [pet-aware batch](pet-aware-catalog.md) adds parlor palm, cast iron plant and
baby rubber plant, with a sourced non-toxic cat-and-dog filter covering five
reviewed species in that earlier batch. The [larger expansion](houseplant-expansion.md)
adds 20 further plants, bringing search to 62 aliases across 31 entries and the
sourced cat-and-dog filter to nine entries. Missing warnings alone do not qualify
a plant for that filter.

The next [15-plant batch and comparison feature](plant-comparison.md) brings the
dataset to 58 entries, 92 aliases across 46 entries and twelve sourced cat-and-dog
classifications. Users can compare up to three species with their care guidance,
credited photos, warnings and sources. Selection remains through filters, details
and reload; comparison URLs contain public species choices. Existing catalog IDs,
personal care data and reference photos remain intact.

The [next growth-reference batch](growth-reference-expansion.md) adds ten further
plants, twenty aliases and one sourced cat-and-dog classification. The current
totals are 68 plants, 112 aliases, thirteen classifications and 77 original photos,
with sixteen growth references across eight species.

[Reminder snoozing](reminder-snoozing.md) now saves 1-hour, 24-hour or 3-day delays
on current Dashboard/Notifications reminders without changing care due times or
history. The additive migration and reminder-protocol guard require compatible
forward recovery; do not downgrade to the older worker. Installed-phone delivery
of a delayed follow-up remains a separate acceptance check.

## Before a release tag

1. Complete operator identity/privacy disclosures and remaining log/provider/
   support/marker retention criteria. Guarded encrypted-backup expiry and managed
   erasure reconciliation are verified in [retention-and-erasure-recovery.md](retention-and-erasure-recovery.md).
   Independent current-authority replication and replacement-host acceptance are
   still open; missing authority must not be bypassed during recovery.
2. Retain and review the attributed starter catalog through the controlled
   import/update procedure, expanding it as needed. Keep the preserved synthetic
   demo distinct from reviewed botanical content. Expand curated growth references
   through the existing source/license review; the initial examples are implemented.
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
