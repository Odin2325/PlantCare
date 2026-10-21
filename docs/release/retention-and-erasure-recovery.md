# Backup expiry and erasure-safe managed recovery

Deployed on 2026-10-21 as `677b91a0083cdfec6e5f2656a82c9594e49d07b0` after
exact-candidate [CI run 39584072953](https://github.com/Odin2325/PlantCare/actions/runs/39584072953)
passed all gates. The additive AccountErasures migration and restricted grants
were applied on staging. The guarded 30-day policy is enabled. It does not finish
the public privacy notice or certify a production launch.

## Thirty-day expiry

Set-BackupRetention.ps1 previews the complete repository and saves a private plan.
Enabling requires its exact repository-state hash. The policy applies only to the
two known local/VPS host/tag groups. Both must have fresh verified successful
backups, and their recorded latest snapshots must exist. Unknown groups, future
dates, missing/latest stale backups, changed repository state or a modified expiry
set stop cleanup. Snapshot IDs older than the reviewed UTC cutoff are expired;
the latest verified copies are protected. Age exactly 30 days is retained until
it passes the cutoff.

After enabling, existing backup scripts run retention after successful capture
and verification, under the existing operation lock. No schedule or health
threshold is changed. Restic forget removes only the exact planned IDs, then
prune runs with max-unused 0 and a full read-data check. A durable pending flag
retries interrupted pruning even when no newly expired snapshots remain. Retention
failure is recorded separately and appears in backup health checks; it does not
discard a successfully captured backup.

This is a 30-day cleanup target, not a hard wall-clock deletion guarantee. The
PC/drive must be available, and safety failures defer cleanup rather than erase
the last usable copy. Existing weekly monitoring reports retention failures.
Do not promise a strict 30-day maximum while those dependencies exist. Provider
copies, exported files, delivered mail and logs have separate retention decisions.

## Deletion markers and current authority

Account deletion records a minimal user ID and UTC erasure/reference time in
AccountErasures in the same transaction as active-data removal. It has no account
foreign key and survives user deletion. API access is SELECT/INSERT only; worker
access is SELECT only. Runtime probes verify that DELETE is denied. No marker
pruning is enabled, and the migration refuses a downgrade that would drop them.
Identity, plants/care, calendar feeds/shares and integrations have matching query
filters to hide marked identities while reconciliation is pending.

The offline export command produces a private, complete current-authority file:
marker IDs/times plus the current non-erased account IDs, environment identity and
capture time. The full live-ID set also protects deletions made before marker
tracking began. It contains no email addresses, password hashes or tokens. A
serializable read keeps the two sets consistent. It is not a public endpoint.

Managed recovery fetches this authority through the trusted live local/VPS source,
keeps it outside the restored snapshot, and refreshes it immediately before
reconciliation. The operator job checks version/source, freshness, duplicates,
limits and required membership, merges markers without replacing newer target
markers, and removes restored accounts absent from current membership or marked
erased. Related private data and shares are removed through the normal cascades.
Other current accounts and the species catalog remain intact. Reconciliation is
transactional/idempotent and verifies physical user removal before commit.

Recovered API/worker startup follows reconciliation, never precedes it. Missing,
stale, malformed, wrong-source or unavailable authority stops managed recovery.
SQL-only restore checks do not constitute permission to expose a recovered app.
For a real cutover, freeze source mutations from the final authority capture until
the replacement starts; isolated drills exercise a point-in-time copy and leave
the live source untouched.

If the live source/current authority has been lost, do not fall back automatically
to a journal inside an older backup. Independent durable authority replication
and a replacement-host drill remain open. Verified request reconciliation is
required before any manual reopening. This limitation is intentional: availability
does not take precedence over resurrecting deleted accounts.

The current guarded paths are Invoke-LocalRecoveryDrill.ps1 and
Test-VpsApplicationRecovery.ps1. The latter requires an erasure-aware pinned
bundle (manifest protocol 1), applies additive migrations only to the isolated
target, and disables external integrations. Current authority files are removed
with their restricted generated scratch after the drill. Do not publish them.

## Deployment and rollback

New successful local drills remove their generated SQL/key payloads and source
temporary SQL backup, retaining only private evidence. Older retained drill
artifacts and failed-drill payloads require separate operator review; repository
expiry is scoped to the encrypted snapshot groups and must not be claimed to
erase every historical copy on the computer.

Back up before migration. Activate-StagingBundle.sh validates the protocol,
migrates and provisions controlled grants before starting the pinned candidate.
Automatic application fallback is permitted only to another marker-aware bundle.
If the previous image predates the protocol and activation fails, API/worker stay
stopped for a forward repair; do not silently resume deletion without markers.
Schema downgrade and journal deletion are not recovery procedures.

## Verification record

- Read-only real-repository preview: 13 snapshots, zero older than 30 days before
  the additional local pre-migration backup. No real snapshot expired in that preview.
- Pure policy tests passed cutoff/latest protection, fresh groups, unknown groups,
  duplicate IDs and future timestamps.
- Disposable encrypted-repository test removed only two expired fixtures, rejected
  a modified plan, pruned/checked data, resumed pending pruning and restored both
  retained backups. The real repository was not used for destructive testing.
- Generated-account tests cover transactional markers, replayed older rows,
  pre-marker deletion via full current membership, other-account preservation,
  stale/missing/duplicate/wrong-source/disabled reconciliation and filtered access.
- All 38 isolated SQL push/account/erasure checks passed. Full backend regression
  and final targeted recovery cases passed; exact-candidate CI passed backend,
  frontend, retention safety, SQL security/push/recovery and browser/PWA gates.
  The initial parallel restart drill interrupted SQL test connections; the full
  SQL suite was rerun separately and passed, without weakening assertions.
- The final local full SQL/key/application drill passed fresh complete authority,
  reconciliation before startup, restored session/history, worker/restart behavior
  and generated SQL/key-payload cleanup.
- Fresh encrypted pre-migration VPS backup and transferred bundle hashes passed.
  Staging migration, marker-aware activation, runtime positive/negative access
  probes and trusted public DNS/TLS/health passed. Real desktop frontend update
  from e5d8131 preserved session/collection/calendar. Public Account export/axe/
  safeguards passed; no existing public account was deleted for testing.
- The pre-migration e5d8131 VPS snapshot restored in an isolated new-runtime stack.
  Raw SQL/key checks, target migration, freshly exported current authority and
  before-start reconciliation passed, followed by copied-account recovery/login,
  history/worker/restart deduplication and failed-activation recovery/logout.
- Enabled only after reviewing a 15-snapshot, zero-expiry plan. Existing local
  and VPS scheduled tasks each subsequently returned 0 with configured retention;
  the VPS task captured the new schema. Final preview: enabled, 17 snapshots,
  zero expired. Both backup health reports and public service checks were clear.
  No existing real backup was removed; destructive expiry/prune proof used only
  the disposable encrypted fixture. Schedules and thresholds were unchanged.
