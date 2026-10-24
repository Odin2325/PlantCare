# Reminder snoozing: next feature increment

Implemented for validation on 2026-10-24 following the owner's request to expand
the catalog and build the recommended features. Staging deployment/acceptance is
recorded below once complete; the earlier pet-aware catalog release did not include it.

## User behavior

- Offer Remind me in 1 hour, 24 hours or 3 days on a current care reminder.
  Use explicit elapsed durations; do not label 24 hours as tomorrow when a clock
  change could make that misleading.
- Snoozing delays a reminder, without recording completion, changing the care
  interval or moving the actual due date. Dashboard and calendar may still show
  care as overdue; display the snoozed-until time beside it.
- Save the decision on the server so it survives app/device/worker restarts.
  Offline or failed requests must not look saved.
- Completing care, disabling/archiving the schedule or archiving the plant before
  the delay expires must prevent another reminder for that old occurrence.
- Respect channel/action preferences. Snoozing does not grant push permission.
  Previously delivered or provider-queued pushes cannot reliably be recalled.
  Keep the existing care-action icon and Dashboard destination.

## Implementation boundaries

The existing unique notification key is schedule plus actual due time, and device
delivery outcomes belong to a notification ID. Preserve accepted device outcomes
rather than resetting them to pending. A follow-up reminder needs its own identity
and durable generation/eligibility state while retaining the original care due time.
Its delivery-expiry window must start at reminder eligibility, rather than expiring
a three-day snooze one day after the snooze was requested.

The owner-only API must apply normal authentication, antiforgery and throttling.
Serialize competing snoozes and completion for the same occurrence: repeated or
stale commands must not schedule multiple follow-ups or revive completed care.
An additive migration must preserve existing notification IDs, delivery ledgers,
account export/deletion and erasure-safe recovery. Shared read-only calendars must
not gain care or reminder-write permissions.

## Required acceptance

Use controlled time in domain and real SQL tests to verify delay boundaries,
repeated/concurrent requests, completion and archive/disable during the delay,
preference changes, another account's denial, worker restarts and partial-device
retries. Verify the UI's loading/error/saved states, displayed local time and
unchanged care history/due time. Keep existing baseline reminder behavior covered.
Then pass exact-candidate CI, normal staging backup/update checks and one explicitly
authorized real-device follow-up before claiming installed Android delivery accepted.

## Implementation

Dashboard and Notifications offer the three durations on a current generated
reminder. The saved server time is shown locally beside care that remains due.
Visible pages refresh reminder state on focus/reconnection and once per minute;
the existing API returns at most 100 notifications, prioritizing current care.
Historical or disabled occurrences are not offered snoozing. Read status is separate
from snoozing. Failed requests do not display a successful save.

`POST /api/notifications/{id}/snooze` validates the elapsed duration, ownership,
current care and preferences. A serializable transaction conditionally claims the
original notification before reading its schedule. Competing requests for that ID
produce one follow-up; stale or already delayed requests return conflict. A new ID,
sequence and `NotBeforeUtc` retain the actual due time, while `SupersededAtUtc`
hides the old notification from the active list. No history entry is created.

The worker waits for eligibility, checks current care/preferences again and uses
eligibility plus one day for delivery expiry. It closes pending attempts on the
superseded reminder without resetting accepted device outcomes. Existing care icons,
Dashboard taps and bounded push retry behavior remain. In-flight/provider-queued
messages cannot reliably be recalled; delivery is not claimed exactly once.

The additive migration preserves old IDs and delivery rows with sequence zero.
Account export includes reminder state; deletion and erasure reconciliation retain
their notification cascade. The API gains notification INSERT permission to create
follow-ups, with no new delivery-ledger write permission. The worker owns cancellation
of pending device attempts.

Exported committed source declares reminder protocol 1. Bundle creation reads that
commit's capability marker, so exporting an older commit cannot relabel its worker.
Activation blocks protocol downgrades; failed activation stops writers if the old
runtime cannot safely handle snoozes. Backups record the required protocol and app
recovery rejects incompatible bundles. Schema downgrade is explicitly rejected:
an old worker could otherwise deliver delayed reminders immediately. Use compatible
forward recovery, preserving the erasure protections.

## Local verification

The backend suite passed 55 domain, one application and 115 API tests. All 62
selected real SQL Server push/account/sharing checks passed, including elapsed
eligibility, three-day expiry handling, concurrent snoozes creating one follow-up,
stale/foreign requests, secure antiforgery, completion/disable/archive/preference
cancellation, preserved accepted deliveries and account export/deletion of follow-ups.
All 35 frontend tests passed, including pending/error and duplicate-submit behavior.
The real browser care journey now snoozes first, verifies the saved delay after
reload and completes care with its original due time/history preserved.
