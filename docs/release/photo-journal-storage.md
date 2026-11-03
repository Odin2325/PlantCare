# Private photo-journal storage and lifecycle

Implemented locally on 2026-11-03 after the
[private worker connection](photo-worker-connection.md). Not pushed or deployed.
**No upload route exists and the transport remains disabled.** Only synthetic
test data was created. No normal application database, real photo, catalog asset,
backup, schedule or existing runtime login changed.

## Persistence and transaction boundaries

The additive `20261103163152_AddPrivatePhotoJournal` migration creates:

- `PlantPhotoEntries`: owned-plant ID, upload request ID/fingerprint, photo date,
  optional note, creation UTC timestamp and edit concurrency version;
- `PlantPhotoMedia`: one-to-one normalized JPEG/thumbnail bytes, dimensions and
  SHA256 integrity values, without original image, filename or source metadata;
- `PhotoJournalQuota`: a seeded singleton lock row with no account information
  or cached usage total.

Metadata/media insert atomically. A unique plant/request index prevents duplicate
uploads. Retrying the original normalized payload/date/note returns the existing
entry without overwriting later edits; a changed payload under the same request
ID conflicts. Entry deletion also removes that request record: there is no
forever-lived idempotency ledger for deleted entries.

Hard defaults: 20 entries per plant, 50 per account, 20 MiB normalized media per
account and 512 MiB service-wide, including thumbnails. Only lower limits are
accepted. Counts and actual binary lengths are checked inside the insertion's
serializable transaction. The service-wide total includes physically stored rows
hidden pending erasure reconciliation. All journal writes, ZIP snapshot reads,
account deletion and erasure reconciliation acquire the database lock row before
account/plant reads. Concurrent API processes cannot each spend the last slot.
Decoding and response transfer happen outside the lock. Deletion reclaims space.

The storage boundary caps JPEGs at 512 KiB, thumbnails at 64 KiB and dimensions
at 1,600 pixels per axis. It checks JPEG markers without running native decoding
in the API; the isolated worker remains responsible for validating/re-encoding
originals. Dates cannot be empty or later than tomorrow UTC, allowing the user's
time zone. Notes are trimmed and capped at 500 characters. Invalid inputs/quota
failures leave no partial rows. Failures expose no other account's usage. Saving
does not create plants, care, reminders, notifications, planting targets or harvests.

## Prepared private API

| Method | Route suffix under `/api/my-plants/{plantId}/photos` | Behavior |
| --- | --- | --- |
| GET | root | At most twenty metadata entries, newest first; `oldestFirst=true` reverses order. No binary list payload. |
| GET | `/{id}/image` or `/{id}/thumbnail` | Owner-only JPEG, verified against its stored SHA256 before return. |
| PATCH | `/{id}` | Date/note edit with expected version; stale versions conflict. |
| DELETE | `/{id}` | Owner-scoped idempotent metadata/media removal. |

There is deliberately no POST/upload action. Owners derive from authentication,
with active-account and plant checks. Plant/photo IDs must belong together;
missing and other-owner records share a missing response. Calendar sharing grants
no photo access. Archived plants retain photos and allow removal; new saves
require an active plant. PATCH cannot replace the picture. Cookie writes use the
existing antiforgery filter. No public image URL or bearer token is introduced.

Photo responses retain `private, no-store, no-cache` after Identity's headers;
binary content type is fixed JPEG with `nosniff`. No ranges, CORS change, CDN or
service-worker media cache is enabled. The existing CSP remains intact. Corrupt
stored bytes return a safe conflict instead of an image. Header enforcement opts
in only photo endpoints/ZIP export, leaving other routes' cache behavior intact.

## Account export, deletion and recovery

The existing password-verified JSON export now adds owner-only journal metadata
and describes the separate image download, preserving the JSON format.
`POST /api/account/export/photos` requires authentication, current password,
cookie antiforgery and the existing authentication rate limiter. It returns a ZIP
of normalized JPEGs named by generated IDs and a JSON manifest containing plant
IDs, dates/notes, timestamps, filenames and hashes. It excludes originals,
duplicate thumbnails, credentials and other owners' data. There is no public
download link, plaintext export file or background queue.

ZIP collection takes a consistent owner snapshot under the journal lock, checking
the 50-entry/20-MiB bound before loading bytes. The lock is released before ZIP
creation/transfer. The archive is separately capped at 21 MiB. One export per API
process is admitted without queuing, held through response completion to bound
memory. This is a bounded in-memory download, not unlimited database streaming;
actual VPS peak memory/transfer behavior remains an activation check.

Account deletion atomically cascades plant -> entry -> media with its erasure
marker. Both photo tables have tombstone filters. Replayed deleted-account data
stays hidden before current-ledger reconciliation removes it. Reconciliation takes
the same journal lock. Ledger protocol/authority rules remain unchanged. Existing
recovery scripts already migrate before applying the current ledger, ensuring
the lock table exists before these commands run.

Individual deleted photos can return from an older disaster-recovery snapshot
for an active account; no per-photo suppression ledger exists. Deleted-account
suppression is different and tested. Downloaded/in-flight copies cannot be
recalled. Encrypted backups retain the existing guarded 30-day cleanup target and
availability limits, without a promise of immediate physical erasure or a strict
maximum retention time.

The shared SQL snapshot query still discovers all tables. It now additionally
fingerprints actual image/thumbnail bytes with SHA256 in stable ID order when
the photo table exists. It does not rely on stored hashes or row checksums to
compare blob contents. Older schemas without that table retain their existing
format. No backup was performed; capture/transfer/restore acceptance remains open.

API runtime policy grants entry SELECT/INSERT/UPDATE/DELETE, media
SELECT/INSERT/DELETE and quota-row SELECT/UPDATE. Media UPDATE and quota
INSERT/DELETE are excluded. The reminder worker receives no permissions on any
new table. Actual deployed grants still require checkpoint verification.

## Focused verification and next step

Twelve affected SQL cases passed across the focused runs: eight journal cases,
two extended erasure-recovery cases and two existing deletion rollback/provider
guard cases. They cover idempotency, quotas/reclaimed space, the last-global-slot
race, malformed input with no partial rows, cookie CSRF, stale edits, archive
retention, owner-only reads and ZIP/JSON export, calendar-share exclusion,
disabled uploads, private headers, corrupt bytes, account cascade and replayed
erased media while another owner's bytes survive. Actual byte changes affect SQL
snapshot hashes. API/worker grants were verified with database-local users
without logins, leaving real server logins/passwords untouched. Early SQLite
checks supported iteration; SQL-only concurrency/permission cases ran on SQL Server.

The model matches its generated migration. Cases join the isolated SQL runner at
the next full CI checkpoint. Existing conservative rules still select full scope
for this private-data/schema/erasure/snapshot change. No full application suite,
remote CI, public email, push, backup/restore or deployment was run. Generated
test databases were removed by guarded cleanup; ignored reports remain private.

Next: bounded multipart admission, an owner/CSRF-protected upload controller,
request-ID reconciliation after uncertain saves, and the owned-plant journal UI.
Put admission before reading the body: the byte-array transport gate alone does
not bound concurrent HTTP buffering. Include accessible previews, account-change
cleanup, offline/retry states and explicit delete confirmation. Keep uploads off
until full candidate CI, actual migrated capture/recovery, VPS resource/grant
checks and the existing interrupted-backup-resumption follow-up pass.
