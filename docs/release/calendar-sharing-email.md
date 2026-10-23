# Calendar sharing email delivery

Owner report 2026-10-23: entering a Gmail recipient did not produce an email.
The previous share operation only persisted in-app read access for registered
users; it did not call an email sender or invite unregistered recipients. This
was independent of the recipient's email provider.

New shares now send a transactional email through the existing SMTP configuration,
using a configured-origin calendar link. Registered recipients retain the existing
direct-sharing behavior. Unregistered recipients get a pending invitation without
creating a placeholder account or granting access. They create an account with
the invited address, confirm their email and accept the link. Signup/login preserve
the return URL; after acceptance the token is replaced with a normal share URL.
Only the matching confirmed recipient can accept; links alone cannot read calendars.
Shared entries remain read-only and cannot open private owner plant details.

Creation reports email acceptance separately from the persisted access grant.
An SMTP failure leaves access usable and the UI says the email was not sent.
Owners can use Send email on existing shares or pending invitations, including
shares created before this fix. Invitation links expire after seven days and
grant access only once. Resending replaces earlier tokens and renews expiry.
Resends do not duplicate access and are denied to recipients/observers or after
revocation. Create/accept/resend requests share a 20-per-hour source-IP limit.
No automatic retries are claimed; SMTP acceptance does not guarantee inbox placement.

Whitespace/case differences are accepted; Gmail dots and plus aliases are not
rewritten or merged. This does not enable Google OAuth or automatically add events
to Google Calendar. Pending invitations and granted shares are separately labelled.

Email content contains the owner's email and an authenticated calendar link,
without plant details, credentials or reset tokens. Text/link values are encoded;
SMTP credentials/provider errors are not logged. Only SHA256 token hashes are
stored. The additive CalendarInvitations table has an owner foreign key and active
invitation/token indexes; API access is provisioned with other private tables,
while the worker gets no invitation access. Acceptance and revocation use serializable
transactions to prevent a pending-card revoke racing acceptance from leaving access.
Direct sharing and revocation also invalidate earlier invitations to that recipient,
so an older unaccepted link cannot restore a grant after the owner revokes it.
Account export excludes tokens/hashes. Account deletion and erasure reconciliation
remove outgoing invitations and invitations addressed to the erased account.
Expired records remain owner-managed until revocation/account deletion; expiry
invalidates the link, rather than claiming automatic record cleanup. No new mail
provider or paid service is needed. Catalog content and notification icons remain.

Local verification: 32 frontend tests and all three sharing browser journeys
(desktop Chromium, mobile Chromium and Firefox) passed. The browser flow captures
real SMTP mail in Mailpit for a Gmail-format address, registers the new recipient,
confirms email, follows the invitation, resends a sharing email and revokes access.
The shared calendar passes sampled accessibility, including empty mobile views.
The migration matches the EF model and adds only the invitation table/indexes.

All 53 selected real SQL Server push/account/erasure/sharing checks passed,
including applying the new migration, invitation expiry/replay, wrong/unconfirmed
recipients, failed-email resends, stale-link revocation and erased invitation data.
Staging acceptance will be recorded after completion.
