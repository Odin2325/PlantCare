# Account isolation on SQL Server

Run with Docker Desktop (Linux containers) and PowerShell 7:

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalAccountIsolation.ps1
```

The script targets only `https://localhost:8443`, Mailpit on `localhost:8025`,
and the `plantcare-local` Docker API. It does not connect to Windows SQL Server.
HTTPS certificate validation is skipped only for that fixed local origin.
Do not run it concurrently with another acceptance or recovery drill.

It registers and confirms three synthetic ordinary accounts through real email
links and cookie authentication. A and B each own a plant, completed care event,
and worker-generated notification. C is an unrelated observer for calendar shares.
No account is promoted; all three must receive 403 when attempting catalog writes.
The demo catalog is populated through the existing explicit operator importer.
The script waits and retries authentication requests with a bounded delay when
its three-account setup/logout reaches the production IP rate limit; the server
policy is unchanged.

The checks cover:

- Anonymous denial and separate authenticated identities. Client-supplied user
  IDs in payloads/query strings cannot select the owner of account-scoped data.
- A/B isolation in collection, dashboard, calendar, and notification lists.
- Both directions of plant read/update/archive/restore denial, schedule creation,
  editing/removal, care completion, history reads/edits/deletes, and notification
  read acknowledgement. Requests use valid payloads and attacker antiforgery
  tokens so a validation failure does not masquerade as an ownership check.
- Mixed IDs: an attacker-owned plant plus an owner-owned care event must still
  fail. Owner plant/tags, schedule, history, and notification state remain intact.
- Preference changes affect only the signed-in account. Push endpoints cannot
  be taken over or deleted by another user. Synthetic push endpoints require
  local push delivery to remain disabled; no push service is contacted.
- Calendar shares admit only the intended recipient, expose only the owner's
  calendar, and do not grant access to private plant endpoints. An observer
  cannot list/read/revoke the share; the recipient cannot revoke it. Owner
  revocation immediately removes recipient access.
- Calendar feeds contain only the account associated with their opaque bearer
  token. Possession of that URL intentionally grants anonymous read access;
  rotation and revocation invalidate it without revoking another user's feed.
- Archived collections remain isolated and the sessions are logged out.

Synthetic accounts and archived plants remain in the local database. Successful
runs revoke created feeds/shares and delete the synthetic push subscription.
A failed run can leave active fixtures; retain them for diagnosis or use a fresh
isolated project for another acceptance environment. The script never prints
passwords, confirmation links, or feed tokens, and redacts feed paths in failures.

## CI and limits

The production-container CI job runs the same script against an empty SQL Server
container, real API/worker images, HTTPS proxy, and local Mailpit. It uses
`Start-LocalDeployment.ps1 -SkipBuild` after tagging the already-built CI images.
CI removes only its disposable local project/volumes afterward. This adds real
cookie/antiforgery and SQL authorization coverage alongside the SQLite API suite.

This does not validate Google/Microsoft OAuth credential isolation, real push
delivery, browser interactions, or concurrency. Those remain separate acceptance
checks. A calendar-feed regression test also covers anonymous download, owner-only
entries, no-store caching, rotation/revocation, and another account's unaffected
feed. Feeds cover 30 days of history and 335 days ahead, staying within the calendar
service's 366-day maximum; the old 395-day window made valid feeds return 400.
