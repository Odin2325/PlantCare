# PlantCare v1 acceptance checklist

Baseline: `969d8c074b8a399d05a32cffc33c2cb1d3eeff52`, assessed 2026-10-11.

Proposed v1 scope: accounts, plant catalog, personal collections and tags,
interval/weekday care schedules, care history, dashboard, calendar views,
in-app reminders, browser push, calendar subscriptions and sharing, and PWA
installation/updates. Google and Microsoft synchronization are optional launch
features: keep them disabled until their provider-specific checks pass.
Defer new features until these gates pass.

Unchecked means not yet accepted, even when supporting code or unit tests exist.
For each executed check, record the candidate SHA, environment, date, tester,
result, evidence link, and associated defect. Use two ordinary accounts A/B and
one administrator, synthetic plants, and dedicated test email addresses.
Never include passwords, reset links, OAuth tokens, or calendar-feed tokens in evidence.

## Gate 1: clean deployment and accounts

- [ ] From an empty staging database, all four Compose services start; API
  readiness succeeds; worker remains running; no migration or key-storage errors.
- [ ] HTTPS works and HTTP redirects to HTTPS; `/health/live` and
  `/health/ready` return success; an unknown `/api/` route returns 404.
- [ ] A registers, receives a real confirmation email, confirms once, signs in,
  reloads the page, signs out, and cannot access protected pages afterward.
- [ ] Production rejects login before email confirmation; invalid credentials
  and repeated attempts produce useful errors without exposing account data.
- [ ] Forgot-password sends a usable link; reset changes the password; the old
  password and reused/expired reset links fail. Unknown emails receive a neutral response.
- [ ] Missing/invalid antiforgery tokens reject unsafe cookie-authenticated
  requests; valid browser requests work; ordinary users cannot edit the catalog.
- [ ] Initial administrator provisioning and catalog population are documented
  and reproducible without enabling development mode on the public host.

## Gate 2: core care journey

- [ ] A browses the catalog, adds a plant with custom initial care dates, edits
  its nickname/location/tags, and finds it using the collection filters.
- [ ] A configures interval and weekday schedules; UI and API agree on next due
  time. Decide and verify whether today's future preferred time is eligible.
- [ ] The dashboard and month/week/agenda views agree on due, overdue, and
  completed care, including dates near midnight in the user's time zone.
- [ ] Completing care once creates one history entry and the correct next due
  date; repeated submissions/concurrent tabs have an explicitly tested outcome.
- [ ] Future and out-of-order completion dates are rejected without partial writes.
- [ ] Editing/deleting history recalculates the schedule correctly. Pausing,
  resuming, removing/restoring optional schedules preserve the intended history.
- [ ] Archiving hides a plant and stops new care/reminders; restoring it works.
- [ ] A cannot read or modify B's plants, schedules, history, notifications,
  preferences, integrations, or shares by changing IDs in requests.
- [ ] Weekday schedules pass same-day, spring clock-gap, autumn repeated-hour,
  month/year boundary, and time-zone-change scenarios on the Linux deployment.

## Gate 3: reminders, calendars, and PWA

- [ ] Worker generates a reminder within two configured polling intervals of
  its eligibility time, respects lead time/action/channel preferences, and
  does not duplicate reminders after a restart.
- [ ] Push works on each advertised browser/device. Denied permissions,
  expired subscriptions, multiple devices, and partial delivery failures behave
  predictably; retries do not indefinitely block other users' notifications.
- [ ] Archived/disabled/completed schedules do not produce misleading pending
  reminders; notification preferences and read state persist after restart.
- [ ] A calendar feed imports into a real calendar client; regeneration/revocation
  invalidates the old URL and does not reveal other users' entries.
- [ ] Sharing with B exposes only the intended entries; unshared accounts are
  denied, and revocation takes effect immediately.
- [ ] If enabled, each external provider passes connect/consent, manual sync,
  automatic sync, token refresh, revoke/disconnect, API outage/retry, and
  duplicate-prevention checks; unrelated external events remain untouched.
- [ ] PWA installs on supported devices, displays a useful offline state, and
  updates from the previous candidate without stale assets or broken navigation.

## Gate 4: release operations and usability

- [ ] SQL Server migrations pass on both an empty database and a copy of the
  previous staging database. Data survives API/worker/database restarts.
- [ ] Database and data-protection keys are backed up together and restored onto
  an isolated environment; restored users can sign in and use integrations.
- [ ] A deployment/rollback drill succeeds with recorded image/commit versions
  and a schema-compatible rollback or tested database recovery procedure.
- [ ] Monitoring detects API unavailability, worker failure/stalled processing,
  backup failure, and disk pressure; a named operator receives alerts.
- [ ] Phone/desktop layouts, keyboard navigation, focus, labels, validation,
  empty/loading/error states, and session expiry pass the core journey.
- [ ] Privacy/data deletion handling, support contact, installation instructions,
  license decision, and known limitations are documented for launch.
- [ ] Browser journey tests, backend tests, migration checks, frontend build,
  and container smoke tests pass for the exact candidate SHA.
- [ ] A small beta completes at least two normal care cycles; all blockers are
  resolved; remaining defects have an explicit launch decision.
- [ ] Versioned artifacts, release notes, and `v1.0.0` identify the tested candidate.

## Execution record

| Check | Candidate SHA | Environment/date | Tester | Result | Evidence/defect |
| --- | --- | --- | --- | --- | --- |
| Full staging acceptance | pending | No staging host as of 2026-10-11 | pending | NOT RUN | See backlog STAGE-01 |
| Local HTTPS and real-auth smoke | Baseline plus local release fixes | Docker Desktop / SQL Server, 2026-10-11 | Codex | PASS | scripts/Test-LocalDeployment.ps1; registration, confirmation email, login/account/logout |
| API integration suite | Baseline plus local release fixes | Windows / .NET 10, 2026-10-11 | Codex | PASS | 27/27, including migration discovery; SQLite remains a test substitute |
| Frontend unit test and build | Baseline plus local release fixes | Docker Node image, 2026-10-11 | Codex | PASS | 1/1 frontend test; production build succeeded |
