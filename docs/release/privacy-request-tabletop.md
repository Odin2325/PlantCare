# Privacy-request tabletop review

Reviewed 2026-10-21 against [the private-request procedure](privacy-requests.md),
the current Account controls and guarded recovery behavior. All scenarios below
are fictional. This is an agent-led walkthrough of decisions, not a live support
exercise or proof that the owner has operated the mailbox. No email was sent,
account data accessed/deleted or provider setting changed by this review.

| Fictional request | Walkthrough result | Remaining action |
| --- | --- | --- |
| A public issue asks for deletion of `alice@example.invalid`. The reporter knows the address but cannot sign in. | Do not disclose whether the account exists or delete it. Move handling to the private channel without copying private contents publicly. Offer normal recovery; address knowledge is not ownership proof. Never request a reset link/password. | Owner must handle identity uncertainty and any necessary proportionate additional verification. |
| A signed-in person requests all information about them, including support emails and provider records. | Password-verified Account download covers its documented application fields. It is not the complete access response. Review private support/provider/technical records and processing information separately, protecting other people and secrets. | Actual provider access/deletion mechanisms and support-record retention must be documented and exercised by the owner. |
| A verified person deletes their account while the external backup drive is offline. | Active account data is removed transactionally and a minimal marker remains. Do not promise immediate removal from backups or a strict 30-day maximum. Guarded repository expiry resumes only with storage and safety requirements satisfied. | Resolve separate provider/external copies and marker-retention criteria; keep the minimal completion record private. |
| An older backup contains that person, but the current source authority is unavailable during recovery. | Do not start the recovered application for users or treat the old snapshot as fresh authority. The managed workflow fails closed. Do not delete the journal to make recovery work. | Independent current-authority replication and replacement-host acceptance remain open recovery work. |
| A person corrects a plant nickname that appeared in a shared calendar. | Use the supported plant-edit controls. New application/feed results reflect current data; previously received calendar-client copies may persist. Do not promise remote deletion from a recipient's device. | Explain client refresh/copy limits and review other requested corrections privately. |
| A person objects to optional tracking or asks to restrict processing. | Browser push, calendar sharing and account sign-in are separate choices. Do not infer email-tracking consent from registration or promise an unimplemented account-wide restriction switch. Escalate the scope and decision to the operator. | Confirm actual Brevo tracking controls and define the manual restriction/objection response. |
| A complex access request arrives close to a month boundary. | Record the receipt date and applicable calendar-month deadline; do not substitute 30 days. Review whether an extension applies and notify within the initial period with reasons. Do not silently suspend handling because verification is unclear. | Owner review of the specific deadline/extension and final response is required. |
| Advertising is added after someone registered for plant care. | Registration is not consent to future ad tracking. Keep unreviewed advertising disabled; review the actual provider, purposes, recipients, device storage and choices before enabling it. | Complete a separate advertising/privacy review at that time. |

## Outcome and scope

The procedure yields a safe next action in these eight scenarios without treating
a public address as identity proof, the Account download as an exhaustive access
response or backup expiry as immediate erasure. Technical deletion/reconciliation
coverage remains the evidence in [account-data.md](account-data.md) and
[retention-and-erasure-recovery.md](retention-and-erasure-recovery.md); no technical
tests were rerun for this document-only exercise.

This does not close the human-workflow acceptance gate. Before wider signup, the
owner should walk through a synthetic private request, demonstrate the mailbox
and minimal private tracking process, and resolve provider records, lawful bases,
support/log/marker retention criteria and the completed public notices. Do not
commit real request correspondence, identity evidence or exports to this document.
