# Personal growth photo journal: implementation design

Decision record, 2026-10-31. Design complete; uploads are **not implemented or
enabled**. This document defines the first implementation slice and its gates.
No new provider, paid service, public photo sharing or deployment is introduced.
The [2026-11-03 normalizer prototype](photo-normalization.md) implements the first
decoder slice; hard process isolation and all upload-enablement gates remain open.

## Experience and boundaries

Use a Photo journal section on an owned plant's detail page, without another main
tab. Show newest entries first, with an optional oldest-first view, a user-entered
photo date, a short note and an accessible thumbnail. Open a larger image on
request. Display the plant nickname first, following existing personal details.
One image per entry; no automated growth score, diagnosis or care completion.
An entry does not change planting dates, care history, notifications or harvests.

The upload form previews the selected file locally, offers a date (today by
default) and optional note, then requires explicit Save. Only server confirmation
means saved. Failed saves retain the draft for a deliberate retry. Warn before
discarding a draft on navigation/update; do not persist image drafts to browser
storage. Revoke preview object URLs on replacement, completion and account change.
Offline mode explains that photos need a connection; no background upload queue.

Archived plants retain a readable journal and permit deleting entries; adding
requires an active plant. Existing plant deletion behavior must be checked before
adding any new delete control. Account deletion removes the whole private journal.
Use a confirmation before entry deletion; editing replaces date/note only in v1.
Changing a picture means explicitly deleting its entry and adding another.

## Storage decision

Store normalized image bytes and thumbnails in SQL, in a separate media table
from journal metadata. The current VPS capture backs up SQL, data-protection keys
and configuration (`scripts/Capture-VpsBackup.sh`); it does not capture an upload
directory. Keeping the first bounded journal transactional avoids introducing a
filesystem/database consistency and erasure protocol at the same time.

This trades database/backup size for simpler recovery. It is suitable only for a
small beta with enforced limits. Do not enable uploads until actual database
capacity, disk headroom and backup/restore time pass the checkpoint below. Moving
to object storage is a later migration requiring its own access/deletion/capture
design, not a silent fallback when this quota is reached.

Proposed hard defaults for the implementation (binary units):

| Limit | First version |
| --- | --- |
| Incoming file | 8 MiB; whole multipart request at most 9 MiB |
| Decoded image | At most 20 million pixels and 10,000 pixels on either axis |
| Saved image | JPEG, longest edge at most 1,600 pixels, at most 512 KiB |
| Thumbnail | JPEG, longest edge at most 320 pixels, at most 64 KiB |
| Entries | At most 20 per plant and 50 per account |
| Saved bytes | At most 20 MiB per account and 512 MiB service-wide, including thumbnails |
| Note | At most 500 characters; rendered as text |
| Processing | One decoder operation at a time per API process; no waiting upload queue |

Reject an image that cannot fit the saved limit after bounded normalization;
never retain the original as a fallback. JPEG transparency uses a white background.
These are engineering defaults, not measured VPS capacity claims. Limits are
validated configuration and may be lowered before beta; no automatic increases.
Do not disclose other accounts' usage when the service-wide quota is reached.

## Upload validation and resource control

Accept JPEG, PNG and static WebP by decoded content, with matching supported MIME
types; reject SVG, GIF, animated images, HEIC, remote URLs and corrupt/truncated
files. Explain supported formats before selection. Client resizing can improve
convenience later but is never the server's validation boundary.

Choose a maintained decoder only after dependency, license and vulnerability
review. No decoder package is selected by this record. Enforce byte, dimension,
pixel and frame limits before allocating full rasters; check that the selected
decoder actually supports bounded decoding. Bound decode time and memory, with
one admitted operation and prompt busy responses. Cancellation alone is not a
hard stop for synchronous decoding: if the library cannot enforce the bounds,
use a killable restricted process before uploads are enabled.

Apply orientation to pixels, create fresh output images and re-encode without
EXIF, GPS, IPTC, XMP, comments or source filenames. Verify output metadata in
tests. The stored date comes from the form, never EXIF. Source bytes remain only
for request processing; do not log them or write them to a permanent temp folder.
Re-encoding removes metadata, not identifying content visible in the photograph.
The UI warns that rooms, faces, labels and documents may be visible.

Authenticate and resolve an active, non-erased owner before decoding; cookie
mutations use the existing antiforgery protection. Apply authenticated upload
rate limiting plus the single-processing slot before reading the multipart body.
Reject oversized requests at both proxy and application boundaries. Do not let
ASP.NET multipart buffering spill originals to an unmanaged temporary directory;
use a bounded streaming reader. Busy/invalid/quota failures create no rows.

## Model and transaction contract

Proposed entities:

- `PlantPhotoEntry`: generated ID, owned-plant ID, `PhotographedOn` date-only,
  note, UTC creation timestamp and a client-generated upload request ID.
- `PlantPhotoMedia`: entry ID as its primary/foreign key, normalized JPEG bytes,
  thumbnail bytes, dimensions, byte lengths and integrity hashes. No public URL,
  original filename, location metadata or original image.

Derive ownership through the owned plant. Add a unique index on plant/request ID
for retry safety, and plant/date/ID for stable paginated ordering. No duplicate
account FK with a second cascading path. Cascade from plant to entry to media;
the existing account-to-plant cascade then removes both. Metadata and binary reads
must independently enforce ownership and account-erasure filtering. Grant the API
only necessary journal permissions; grant the reminder worker none.

Decode before the short database transaction. In that transaction recheck owner,
active plant, tombstone, entry counts and byte quotas, then insert metadata and
media atomically. Serialize all journal writes using a dedicated database quota
lock row (including deletions), and recalculate quota usage from authoritative
rows under that lock. Every writer takes the same lock order; no process-local
quota counter. Do not hold database locks while decoding or streaming responses.
Test concurrent uploads and account deletion on SQL Server, not just SQLite.

Retrying the same request ID and identical payload returns the original entry;
different normalized bytes/date/note with that ID returns conflict. Recheck this
inside the transaction after any race. The client retains the request ID for an
uncertain submission and must reconcile it before accepting a changed draft.
Idempotency checks do not bypass authorization. DELETE is owner-scoped and
idempotent; date/note updates require a concurrency token to prevent lost edits.

## Private API and browser access

Proposed routes, all authenticated and under `/api`:

| Operation | Route / behavior |
| --- | --- |
| List | `GET /api/my-plants/{plantId}/photos`; bounded cursor page, metadata only |
| Upload | `POST /api/my-plants/{plantId}/photos`; streaming multipart form |
| Read image | `GET /api/my-plants/{plantId}/photos/{id}/image` |
| Read thumbnail | `GET /api/my-plants/{plantId}/photos/{id}/thumbnail` |
| Edit date/note | `PATCH /api/my-plants/{plantId}/photos/{id}` |
| Delete | `DELETE /api/my-plants/{plantId}/photos/{id}` |

All reads verify both route IDs belong together and to the current owner. Return
the same missing response for unknown and other-owner records. Binary routes
return fixed `image/jpeg`, `nosniff` and `Cache-Control: no-store, private`; no
public static-file route or bearer image link. Apply no-store to metadata too.
Do not enable CORS, range requests or CDN caching for photos in v1. Exclude these
routes from service-worker caches; the current Angular config already excludes
API navigation and has no API data cache. Logout/account changes clear gallery
state, cancel requests and release object URLs, including already loaded images.

Calendar share recipients, private calendar feeds, notifications, catalog pages,
public comparisons and comparison share links get no journal/photo access.
Do not reuse journal images as notification icons or public catalog photographs.

## Export, deletion and recovery

Keep the existing password-verified JSON export compatible. Extend it with photo
metadata and an explicit statement that normalized images have a separate ZIP
download. Add a password-verified, rate-limited, antiforgery-protected POST export
for a ZIP containing a metadata manifest and every owned normalized image, named
by generated IDs. No originals exist to export; thumbnails are reproducible and
need not duplicate the image download. Stream a bounded export without exposing
an unauthenticated download URL or retaining the password. Serialize journal
mutations briefly to obtain a consistent bounded export snapshot, then release
the lock before sending it. Do not buffer unbounded ZIPs or leave plaintext files.
Test account deletion racing both exports; no subsequent request may regain access.

Entry deletion removes active SQL metadata and media together. Deleted entries
may remain in existing encrypted backups under the enabled guarded 30-day cleanup
target, which can be delayed by unavailable drives or safety failures (see
[retention and erasure recovery](retention-and-erasure-recovery.md)); do not claim
immediate secure physical erasure or a guaranteed 30-day maximum. Account tombstones must
hide restored metadata **and binary routes** before reconciliation, and existing
account erasure reconciliation must cascade their rows before recovered services
start. Test restored deleted-owner bytes as well as metadata. Individual entry
deletion has no independent restore-suppression ledger in v1: disaster recovery
can bring those entries back for an existing account from an older snapshot.
Disclose that limitation; a per-entry ledger would be a separate protocol change.

SQL capture includes new tables, but that alone is not recovery acceptance.
Snapshot hashing must include binary contents, not only metadata/byte counts.
Verify capture, encrypted transfer to the existing external drive, restore with
byte/hash equality and deleted-account reconciliation. Do not claim a new storage
volume is protected or run a backup while implementing this design. First close
the existing interrupted-backup writer-resumption follow-up at the checkpoint.

## Implementation order and completion gates

1. Review the image dependency and build an isolated normalizer with malicious,
   metadata, orientation, truncation, animation and memory-bound fixtures. Add
   limits/configuration without enabling routes. If resource bounds cannot be
   demonstrated, stop at this layer and isolate decoding.
2. Add schema, owner-scoped service, transaction quota enforcement and private
   routes. Focused SQL tests cover cross-owner access, CSRF, races/idempotency,
   tombstones, quota failure, no partial writes and least-privilege runtime grants.
3. Add the owned-plant journal UI, account switching, accessible phone controls,
   uncertain-write reconciliation and explicit confirmation. Test offline/retry,
   keyboard navigation, 320-pixel reflow and absence of public/calendar leakage.
4. Complete JSON/ZIP export, deletion and recovery tests. Update the privacy draft
   with exact implemented behavior and limitations before accepting uploads.
5. At the next meaningful batch checkpoint, run full committed-candidate CI,
   migrate a disposable SQL database, measure image processing on the VPS budget,
   capacity/backup headroom and restore duration, and run migrated capture/recovery
   acceptance. An upload feature flag stays off until all gates pass. Changing the
   flag/configuration belongs to the reviewed activation, not a frontend toggle.

Routine increments remain local commits and targeted checks. No full pipeline or
staging rollout is required for this design-only record. Catalog photo sourcing
continues separately: personal uploads never fill exact-cultivar photo gaps.
