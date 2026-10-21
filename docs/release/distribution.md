# v1 distribution preparation

Reviewed 2026-10-21. No v1 tag, GitHub release or production launch is created by
this preparation. The staging runtime remains `82de1584291f7238e9e2fd5d6a677d0fa6808257`;
subsequent documentation commits are not new application deployments.

## Documentation prepared

- [User guide](../user-guide.md): accounts, first plant, daily care, nickname
  calendar links, notifications, Android install/update, offline and sharing.
- [Support](../../SUPPORT.md): GitHub Issues for general support, safe report
  contents and the unresolved confidential contact.
- [Known limitations](../known-limitations.md): actual beta/device/content and
  operations scope, separate from planned features.
- [Privacy review](privacy-review.md): implementation facts and decisions/work
  required before publishing a complete notice.
- [LICENSE](../../LICENSE): owner chose rights reserved on 2026-10-21; no
  open-source grant. Third-party license/notice review remains part of packaging.

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

1. Resolve the confidential contact/operator identity, complete the privacy
   notice, choose/implement retention and test account-data request handling.
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
