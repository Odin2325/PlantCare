# Gated photo uploads and journal UI

Implemented locally on 2026-11-03 after [journal storage](photo-journal-storage.md).
Not pushed, deployed or activated. The previous staging runtime remains unchanged.
Both `PhotoJournal:UploadsEnabled` and `PhotoProcessing:Enabled` default to false.
The application refuses an upload-enabled configuration without the isolated
transport enabled. Local tests explicitly replace the processor with a synthetic
test double or mock APIs; they do not enable uploads for real users.

## Upload boundary

`POST /api/my-plants/{plantId}/photos` accepts a streamed multipart body with
exactly the supported fields: `requestId`, `photographedOn`, optional `notes`, and
one `file`. It derives the owner from authentication. Disabled uploads, missing
ownership, archived plants and occupied admission reject before reading the body.
There is one admitted upload per API process, without queuing, plus a fixed
twenty-per-hour source-IP limiter. The gateway still separately admits one job.

Cookie uploads require the antiforgery header; the filter cannot fall back to
reading form tokens. Existing authenticated bearer policy is retained. Form/file
value providers are disabled, so MVC does not spool a form/file before admission.
The reader uses bounded in-memory chunks, no `IFormFile`/`Request.Form`, no disk
queue and no original filename. It accepts JPEG/PNG/static WebP MIME types; actual
content/frame/dimension validation remains in the isolated decoder.

The file limit is 8 MiB, total HTTP body limit 9 MiB, with at most four named
sections, bounded headers/boundary and small UTF-8 metadata fields. Both known
content length and chunked reads are bounded. Remaining multipart epilogue bytes
are counted before decoding or saving, so trailing chunked data cannot bypass
the total cap. Intake has a twenty-second deadline. Kestrel and the upload-only
Caddy matcher also enforce the total cap. Invalid/duplicate/unknown fields and
unsupported or oversized files never reach the processor or create rows.

After metadata validation, the API calls the private `IPhotoProcessor` transport,
then the existing transactional storage service rechecks active ownership,
tombstones and quotas before insertion. Erasure/archive changes during decoding
cannot be bypassed. Processing failures cross HTTP only as allowlisted codes;
unknown implementation messages are suppressed. Admission is released on every
exit. No native codec or Docker access is introduced into the API.

Owner-only `GET .../photos/capabilities` reports feature state/active-plant ability,
and `GET .../photos/requests/{requestId}` resolves an uncertain upload without
starting another decode. Both remain private/no-store. This increment changes no
schema; it uses the existing unique plant/request index and storage migration.

## User experience

Photo journal is a section on owned-plant details, without another main tab. It
loads bounded metadata/thumbnails, offers newest/oldest order, shows a full photo
on request, edits date/notes with the expected version, and asks for confirmation
before deletion. The viewer receives keyboard focus and returns it to its opener
when closed. Literal user-entered dates are preserved; default/max date follows
the current local day when selecting a new unedited draft.

When uploads are enabled, the form offers a local preview, photo date and optional
500-character note. It explains supported formats, the 8-MiB limit, metadata
removal and visible-content privacy. Source filenames are replaced with `photo`
in multipart transmission. Saving does not complete care, alter reminders, acquire
plants or record harvests. Archived/disabled states allow existing photos to remain
viewable/removable without presenting a new-upload action.

Only confirmed server success clears the draft. Failed uploads retain the same
file, date, notes and request ID, locking changes until status is checked or the
user explicitly discards it. A found request clears the draft and reloads the
journal. Not-found status does not prove an earlier request cannot still finish;
the UI says so. Transient failures can retry the unchanged payload. Rejected
format/size/metadata/conflict cases explain the problem and require a deliberate
draft change instead of repeatedly submitting an unchanged rejected photo.
Deletion errors require authoritative refresh; failed/stale edits retain their
draft and require refresh/cancel/reopen before applying another edit.

Drafts/images stay in memory, not localStorage, IndexedDB or an offline queue.
Offline controls explain the connection requirement. Account/plant changes cancel
requests, synchronously mask the prior context, clear drafts and revoke object
URLs; late responses cannot merge into the new context. Same-plant updates from
other controls preserve drafts. Refresh cancels stale loads. Viewer replacements,
close and destruction revoke URLs. The CSP adds `blob:` only to `img-src` for these
previews/private image blobs; script and other resource policies remain unchanged.

Navigation prompts before discarding a draft. An explicit PWA update also checks
the draft before reloading; ordinary background update checks do not reload it.
The browser-close handler is best effort: an OS kill or browser termination can
still lose an in-memory draft. Nothing is queued for a later automatic upload.

Account now offers Download my plant photos using the existing password-verified
ZIP endpoint, separately from the JSON export. Both export types clear the entered
password and suppress a response download if the account changed in the meantime.
Deletion/export retention and restored-account suppression retain the rules in
[storage/lifecycle acceptance](photo-journal-storage.md).

## Focused verification

- Nine affected backend checks passed on generated SQL databases, including
  default-off registration and the existing storage boundary; the additional
  chunked-epilogue case and normal-upload regression also passed afterward.
  Instrumentation proves missing CSRF headers, foreign ownership, archive and
  busy admission read no body. Tests cover duplicate request IDs/status lookup,
  strict formats/fields/size limits, erased owners after decoding, sanitized
  processor failure and released admission, without real IPC or public messages.
- Fifteen focused frontend tests passed for journal state, account/plant changes,
  URL disposal, uncertain/rejected saves, frozen original retry, edit/deletion
  recovery, offline/disabled state, draft guards, PWA update cancellation and
  account export identity changes. The production build passed with only the
  existing unrelated My plants/Calendar CSS budget warnings.
- Six browser cases passed across desktop Chromium, mobile Chromium and Firefox.
  Synthetic APIs exercise save-confirmation loss, owner status reconciliation,
  no duplicate upload, filename masking/CSRF, keyboard viewer focus, stale edits,
  reload persistence, explicit deletion, authoritative recovery, offline/disabled
  states, accessibility and 320-pixel reflow. A synthetic phone screenshot was
  inspected. Chromium's initial offline container environment was corrected for
  these browser-only tests; decoder network restrictions remain intact.
- Caddy validated the upload-only request limit. No ordinary route's body limit
  changed. Existing conservative scope selection still requires full validation.
  New SQL and browser cases are wired into the full runners for the next checkpoint.

No full application suite, remote CI, push, deployment, real upload, email, push
notification or backup/restore was run. Generated test databases were removed by
their guarded cleanup. Browser mocks do not replace real installed-phone or VPS
acceptance, and the new API tests deliberately use a fake normalized result.

## Next combined checkpoint

The next step is one combined committed-candidate validation and staging
preparation checkpoint, rather than another routine rollout. Package/verify the
separate decoder image and trusted gateway, keep the API free of Docker privileges,
and verify the root-owned socket directory, read-only API mount and real UID/GID
mapping. The host-service example remains uninstalled; base Compose has not been
given a daemon or socket mount automatically.

Run full exact-candidate CI, restricted-runtime access checks, migrated encrypted
capture/restore with image hashes and current erasure authority, and measure actual
VPS memory/disk/backup capacity. Close interrupted-backup writer-resumption work.
Then accept the full HTTP -> real gateway/decoder -> SQL path and existing-session
PWA update, followed by an installed Android photo upload/view/delete check.
Local tests do not certify that activation. Keep both flags off until those gates
pass; no public-release/legal decisions or paid services are bundled into this work.
