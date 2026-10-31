# Account-synced plant shortlists

Implemented locally on 2026-10-31; not pushed or deployed. Staging remains at
`6c84b8e1547e569e189f960af196233508172ee2` with 257 reviewed catalog entries.
No catalog or photograph data changed in this increment.

## User behavior

Signed-in users can save/remove catalog entries from catalog cards and detail
pages. The catalog's My shortlist only control combines with existing search,
category and care filters, persists in the view URL and clears with the other
filters. Clearing filters does not remove saved entries or comparison selections.
View full shortlist resets the other view filters while retaining comparison
selections; normal detail/back links retain the current view. Existing comparison
tools work on saved plants.

Shortlists are private and saved to the account for use across devices. Refresh
shortlist retrieves changes made elsewhere; there is no live cross-device push
or offline editing queue. No private shortlist is stored in localStorage or
included automatically in public comparison links. Those links continue to
contain only public catalog IDs explicitly selected for comparison.

Saving does not create an owned plant, care schedule, notification or harvest.
To acquire a plant, use its existing View details / Add to My plants form. Adding
an owned plant does not silently remove it from the shortlist. No new main tab
is introduced. Anonymous visitors get a sign-in link and do not request private
shortlist data.

The UI waits for server confirmation and serializes changes. If a save/removal
cannot be confirmed, controls require a refresh to reconcile server state before
retrying; it does not report an unconfirmed write as saved. Account changes and
logout cancel requests and discard the previous account's in-memory list.

## Storage, security and data lifecycle

`ShortlistedPlants` stores the owner, public catalog ID and saved UTC timestamp.
The composite primary key prevents duplicates. PUT/DELETE are idempotent; PUT
preserves the first save timestamp. The authorized `/api/shortlist` endpoints
derive the owner from authentication, accept no owner override and require an
active, non-tombstoned account. Cookie mutations use the existing antiforgery
filter. Missing catalog entries cannot be saved; private responses are no-store.

Foreign keys remove shortlist rows when their account or catalog entry is deleted.
The erasure query filter hides unreconciled restored data, and account erasure
reconciliation removes it through the owner cascade. Account export includes
only the current owner's catalog IDs and saved timestamps. Existing database
backup/checksum code discovers every user table, so no backup script edit is
needed; actual recovery acceptance for the new schema remains a checkpoint gate.

The additive `20261031160822_AddPlantShortlists` migration creates this table and
its indexes. The API runtime policy grants SELECT/INSERT/DELETE on it; the worker
gets no shortlist permissions. No erasure/snooze protocol downgrade is introduced.
Staging migration, runtime-grant checks and recovery have not been performed.

## Focused local verification

- Five affected API tests passed on SQLite, then the same five passed on generated
  local Docker SQL Server databases with real migrations. They cover duplicate
  saves, reload, owner isolation, absence of owned plants/reminders, missing catalog
  records, anonymous/tombstoned identities, real-cookie antiforgery, private export
  and owner-only account deletion. The generated databases were removed by their
  guarded test cleanup; personal/local/staging databases were not migrated.
- Two affected erasure-recovery cases passed on generated SQL databases, including
  replayed deleted-owner shortlists, preservation of another owner and tombstone
  filtering before reconciliation.
- Eleven focused frontend tests passed for account changes, canceled requests,
  unconfirmed writes, duplicate clicks, refresh recovery and combined catalog views.
- Six focused browser cases passed across desktop Chromium, mobile Chromium and
  Firefox using synthetic APIs. They cover guest behavior, load/save/remove failure,
  authoritative refresh, reload, detail navigation, search, comparison, no owned-
  plant writes, accessibility and 320-pixel reflow. The phone screenshot was inspected.
- The production frontend build passed with existing unrelated CSS-budget warnings.
  EF reports no pending model changes after the generated migration.

The existing conservative selector classifies the private API/service, migration
and new browser path as full scope. The new browser cases are wired into the full
runner, and shortlist API cases join the isolated SQL runner. No selector rule is
relaxed. No pipeline, full local suite, runtime deployment, email or push was run.

Before deployment, run full committed-candidate CI, actual runtime-grant checks,
migrated backup/recovery acceptance and existing-session/account-switch checks.
The earlier staging backup-interruption follow-up remains open. Next feature work
is the personal photo journal's upload/storage/privacy/deletion design.
