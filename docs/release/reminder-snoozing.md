# Reminder snoozing: next feature increment

Planned 2026-10-24 following the owner's request to expand the catalog and build
the recommended features. This document defines scope; snoozing is not implemented
or deployed by the pet-aware catalog change.

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
