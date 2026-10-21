# Privacy information: draft and launch decisions

Prepared 2026-10-21 from the current implementation. This is an operator review
document, not a finished privacy notice. Do not present it as an approved policy
or enable wider signups on its basis. On 2026-10-21 the owner authorized publishing
[nico.arevalo2325@gmail.com](mailto:nico.arevalo2325@gmail.com) for private requests.
GitHub Issues remains the general-support channel.

Follow-up: [retention-and-erasure-recovery.md](retention-and-erasure-recovery.md)
records the new guarded expiry and reconciliation implementation/enabling status.
Deletion retains a minimal ID/time marker to prevent restoration; transient private
recovery authority also includes current account IDs, never emails/passwords.
Backup cleanup targets 30 days but can be delayed by offline storage or failed
verification. Independent recovery authority replication, provider/log/support
retention and the complete operator notice remain open. The proposal/history below
must not be read as a hard 30-day guarantee or as a finished privacy notice.

The owner accepted the proposed policy of account data kept until account
deletion and backups retained up to 30 days. This is a proposal, not an enforced
backup retention guarantee: no existing snapshots were deleted. Account deletion
removes active data; backup expiry, post-restore suppression, log/provider/support
retention and full operator disclosures remain unfinished. See
[account-data.md](account-data.md) for the implementation and acceptance record.

## Verified implementation facts

| Information | Purpose / behavior |
| --- | --- |
| Account email, user identifier, Identity password hash/security state | Sign-in, email confirmation, password recovery and access control. Passwords are handled by ASP.NET Core Identity; email confirmation/reset links are sensitive. |
| Personal plants, nickname, location, notes, tags, care schedules/time zones and history | Collection display, scheduling and completion records. Location and notes are user-entered, not a verified physical address or device geolocation. |
| Reminder preferences, notification records and per-device push subscriptions | In-app reminders and optional browser push. Stored push endpoint/encryption material supports delivery. |
| Calendar subscription hashes and shares between accounts | Optional private feeds and recipient access. The full generated feed URL is shown once; a hash is retained. Recipients can see calendar entries; external clients may retain copies. |
| Authentication and antiforgery cookies | Essential account session and request protection. Authentication has a configured seven-day sliding expiry; do not describe it as guaranteed deletion after seven days. |
| Calendar view and push subscription identifier in browser local storage; service worker/cache | Remember view/device settings and deliver the installed-app shell/update/offline behavior. Browser storage is separate from server account data. |
| Encrypted backup copies | Recovery of application data and keys. Archiving a plant does not erase it; backups are not currently subject to automated snapshot deletion. |

The source reviewed does not add an analytics SDK or advertising tracker. This
is not a claim about all hosting, email or browser-provider telemetry. Review
actual deployment logs, provider settings and retention before writing the notice.

The current hosting deployment is OVHcloud in the UK, with Brevo used for
transactional mail and browser push services used for optional notifications.
GitHub receives public support issues. Provider contracts, processing locations,
subprocessors, email tracking settings and any transfer safeguards need operator
verification; do not claim that all data remains in Germany or the EU.
Uptime checks and owner-only backup alert emails are operational services and
must be reviewed separately from application transactional email.

## Required owner decisions and launch work

- Identify the service operator/controller for the final notice. The owner selected
  the published Gmail address for confidential requests and GitHub Issues for
  general support; monitor both through the actual operator process.
- Define each processing purpose and its applicable legal basis. Review provider
  arrangements, recipients and international transfers for the deployed service.
- Choose actual retention periods/criteria for accounts, history, notifications,
  support requests, logs and backups; implement and verify them before promising
  deletion dates. No retention deletion is authorized by this document.
- Establish and test identity verification, access/export, correction and erasure
  handling. Include sessions, plants/history, notifications, shares/feed access,
  push subscriptions and any external integrations; inspect the actual schema
  before implementation. Disabling sign-in or archiving is not account erasure.
- Define how backup expiry and restored-data suppression handle an accepted
  erasure request. Do not promise selective removal from immutable snapshots
  without a tested procedure.
- Describe relevant rights, how to request them, and the complaint route; verify
  which authority and contact details apply to the operator.
- Put the completed notice and contact where users can find them before signup,
  and review any other service/operator disclosures required for launch.

The official EU guidance explains the information people should receive and how
access/erasure and other requests are handled:
[Your Europe: data protection](https://europa.eu/youreurope/business/governance-and-sustainability/digital-and-data-compliance/data-protection-gdpr/index_en.htm),
[European Commission: requests from individuals](https://commission.europa.eu/law/law-topic/data-protection/information-business-and-organisations/dealing-requests-individuals_en).
These sources guide the review; this draft does not certify compliance.
