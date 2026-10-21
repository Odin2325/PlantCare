# Account export and deletion

Implementation prepared 2026-10-21. Deployment status: candidate under local
verification; public staging remains `82de1584291f7238e9e2fd5d6a677d0fa6808257`
until a separately recorded successful activation. No normal owner account has
been deleted to test this feature; checks use generated local accounts only.

## User behavior and security

The protected Account page uses POST `/api/account/export` and
`/api/account/delete`. Both require the signed-in user's current password,
normal authentication throttling and cookie antiforgery protection. Failed
password attempts participate in Identity lockout. The server derives identity
from authentication, never a user ID supplied in the request. Deletion also
requires the exact confirmation DELETE.

Export downloads a no-store JSON attachment, schema version 1. It includes the
account profile, active/archived plants, tags, schedules/history, notification
preferences/history, device metadata, calendar subscription/share metadata,
external-calendar metadata, roles and claims. Explicit field projections exclude
password hashes/security stamps, Identity tokens, push endpoints/keys, calendar
feed secrets/hashes and protected provider access/refresh tokens. It excludes
other users' private plants and operational backups/provider-held copies.
Treat the file as private; it is not an automated database restore/import file.

Deletion uses one serializable transaction. It removes incoming and outgoing
share rows, then the Identity account. Existing foreign keys cascade personal
plants/tags/schedules/history, notifications/delivery records, device subscriptions,
preferences, private calendar subscriptions and Identity children. Another account
and the common catalog remain intact. External-calendar connections block the
transaction until the user disconnects on Calendar, so remote cleanup is not
silently bypassed. Copies previously downloaded by external clients may remain.

Successful deletion signs out the browser and clears local view/device metadata;
browser push unsubscribe is best-effort. Other cookies fail security-stamp
validation on their next request. Real Identity bearer tickets are now also
revalidated before authorization; deleted-user or changed-stamp tickets cannot
continue to authorize protected requests. A push already in delivery can still
arrive. UI failure does not silently clear the signed-in identity.

## Operator handling of private requests

The owner authorized publication of nico.arevalo2325@gmail.com for confidential
requests. GitHub Issues is for general support only. Prefer the signed-in,
password-verified Account flow. If a person cannot sign in, use the normal
password-recovery flow first; never ask for a password/reset token or accept a
public issue/email address alone as proof of account ownership. Escalate uncertain
identity cases to the owner rather than deleting data based on an unverified email.
Use the private channel for access/correction/provider-copy questions not covered
by the download. Record only minimal request/completion metadata in private
operator records. Do not commit requests, account exports or recipient details.

## Retention proposal and remaining launch work

The owner selected account data until deletion, with encrypted backups for up to
30 days, as the proposed policy. Active-service deletion is implemented, but
snapshot expiry is not enabled and no existing backup was removed. Do not
promise that all earlier copies disappear within 30 days yet.

Before broader launch, prepare a read-only expiry plan for both local/VPS backup
sets and review the exact snapshots before enabling cleanup. Define how a private
minimal erasure record survives restoring an older database and is reapplied
before exposing the recovered service. Test that suppression without changing
normal owner data, then implement/verify expiry. Until that exists, a restore must
remain isolated and be reconciled against verified deletion requests before use.
No production-ready automated erasure/suppression process is claimed here.
Set separate log, support-request and provider retention; finish operator/legal
basis/recipient disclosures in privacy-review.md. The current notice stays a draft.

## Deployment and verification

No schema migration is needed. Runtime provisioning gains API SELECT on
PushDeliveries for the owner-scoped export. Re-run the existing controlled
`--provision-runtime-users` operator job from the new image before activating it;
the ordinary API/worker must retain their restricted credentials. Verify runtime
access afterward. The extra permission is read-only; no new DDL grant is needed.
Activate-StagingBundle.sh now performs this controlled provisioning before runtime
activation. Application rollback leaves that read-only grant in place; if reverting
the operator policy too, explicitly review/revoke this grant before running the
old provisioner, which refuses unmanaged permissions. No automatic permission
removal or password rotation is performed here.

Checks include real-auth generated-account export/secrecy, anonymous denial,
wrong password/confirmation, cookie antiforgery, owner isolation, atomic provider
block, cascaded removal, cookie/bearer/refresh rejection and old-password login
denial. AccountDataTests also runs on generated SQL Server databases through
Test-LocalPushDelivery.ps1, which preserves the existing push regression checks.
Browser journeys exercise the actual download, archived data/history, disabled
confirmation control, deletion and protected-route rejection, with axe sampling.
No traces, screenshots or external recipient messages are required for this check.
