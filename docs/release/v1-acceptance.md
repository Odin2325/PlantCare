# PlantCare v1 acceptance checklist

Baseline: `969d8c074b8a399d05a32cffc33c2cb1d3eeff52`, assessed 2026-10-11.

Current reconciliation: [milestone plan, 2026-10-24](current-milestones.md).
It identifies completed staging work, exact-candidate evidence and the remaining
work order. Historical unchecked compound gates below remain open when only part
has been accepted; they do not mean every supporting check must be repeated.

Owner direction on 2026-10-21: continue English catalog content, photos and
feature work while deferring the public contact-address decision. This overrides
the original feature-freeze planning below; it does not close unchecked public
release gates. See [the catalog record](houseplant-starter-catalog.md) and
[catalog browsing](catalog-browsing.md) for the current feature evidence.

Proposed v1 scope: accounts, plant catalog, personal collections and tags,
interval/weekday care schedules, care history, dashboard, calendar views,
in-app reminders, browser push, calendar subscriptions and sharing, and PWA
installation/updates. Google and Microsoft synchronization are optional launch
features: keep them disabled until their provider-specific checks pass.
The owner's later direction permits feature work while publication decisions are
deferred; the unchecked gates still govern a public v1 launch.

Continue small owner-directed features while retaining the automated release
checks. Current priorities and their evidence are maintained in the milestone
plan above. A required v1 defect fix remains appropriate throughout this work.

Unchecked means not yet accepted, even when supporting code or unit tests exist.
The [public browser acceptance record](public-browser-acceptance.md) contains
the three-engine authenticated staging sample and real dedicated-account reset
checks completed on 2026-10-14. Keep broader bullets open where only a sample
was executed. Installed Android dashboard taps, offline cold launch and reconnect
are now owner-reported passed. The owner also confirmed the real
`cfbc073` → `dbf65b8` installed Android frontend upgrade, calendar links and
preserved data. On 2026-10-21 the owner confirmed working reminders and multiple
completed care cycles; the normal-cycle check is accepted as owner-reported.
Per-cycle timestamps and running build were not supplied. Broader unchecked gates
still require their own evidence; this report does not accept the whole release.
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

Documentation review on 2026-10-21 prepared the user guide, GitHub Issues support,
known limitations, rights-reserved notice and draft release notes. See
[distribution.md](distribution.md). The documentation gate below stays open:
operator identity, privacy notice, backup retention/restore suppression
and third-party distribution notices remain unfinished. No release tag
or production cutover has been made.
Subsequent account-data work adds password-verified export/deletion and the
owner-authorized Gmail private contact; see [account-data.md](account-data.md).
Subsequent runtime 677b91a adds minimal markers and current-authority reconciliation
before managed restore startup. Guarded 30-day expiry is enabled and both scheduled
backup tasks/health passed. See [retention-and-erasure-recovery.md](retention-and-erasure-recovery.md)
for offline delays, independent-authority gaps and exact verification scope.

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
| Growth-photo discovery and ten houseplants | 259dddd60c18c2b2a63efc5d3c22a32a6dacf8b6 | Public staging / SQL Server / three browser projects, 2026-10-25 Berlin | Codex | PASS for recorded scope | growth-photo-discovery.md: exact CI (48 browser cases), 30 live catalog/discovery/comparison cases, all 69 previous public records unchanged, 78 profiles/87 image hashes, real frontend update preserving session/history, isolated recovery and fresh healthy backups. No external sends or new physical-device acceptance. |
| Growth-reference expansion and ten houseplants | 7ce5a11df523ce38c094fbe48e6395f05fa38f27 | Public staging / SQL Server / three browser projects, 2026-10-24 Berlin | Codex | PASS for recorded scope | growth-reference-expansion.md: exact CI (45 browser cases), 27 live catalog/comparison checks, 68 profiles/77 photo hashes, 59 previous identities/care preserved with four allowed gallery changes, sixteen references across eight species, existing-worker session/history intact, isolated recovery and fresh backups. No new external sends or physical-device acceptance. |
| Plant comparison and 15 additional houseplants | ff61f3003bcc2e83af5a2336a577ffdebb2b72fb | Public staging / SQL Server / three browser projects, 2026-10-24 Berlin | Codex | PASS for recorded scope | plant-comparison.md: exact CI (45 browser cases), all 27 live catalog/comparison checks, 58 reviewed profiles/62 photo hashes, all 44 previous catalog records unchanged, real frontend update preserving session/history and loading comparison, isolated recovery. No schema change or new external mail/push; earlier physical reminder checks remain separate. |
| Durable reminder snoozing and 20-plant expansion | 5e6225a7946b131c7216a4deb5edf37f89b05345 | Public staging / SQL Server / three browser projects, 2026-10-24 Berlin | Codex | PASS for recorded scope | reminder-snoozing.md and houseplant-expansion.md: exact CI (39 browser cases), 43 reviewed profiles/47 photo hashes, all 24 previous catalog records preserved, 21 public catalog checks, real frontend update with session/history intact, live 24-hour snooze with unchanged due/history and synthetic cleanup, isolated recovery and downgrade guards. Delayed physical-phone delivery remains separate. |
| Pet-aware catalog expansion and combined filters | b33c68d1eaf5e600a7cde158a333153bd501c128 | Public staging / SQL Server / three browser projects, 2026-10-24 Berlin | Codex | PASS for recorded scope | pet-aware-catalog.md: exact-candidate CI, all 18 public catalog checks, 23 reviewed entries, 27 photo hashes, 21 previous identities/care records preserved, real frontend update with test-account session/history intact, successful actual VPS backup-task retry and fresh pre/post backups. Physical Android rendering of new reminder drawings remains separate. |
| Calendar email sharing and signup invitations | 6d6d4d52fa6231d335b9b22705f4984ca14c5c76 | Public staging / SQL Server / Brevo / Gmail, 2026-10-24 Berlin | Codex | PASS for recorded scope | calendar-sharing-email.md: exact-candidate CI, actual frontend update with data preserved, one Gmail inbox message with DKIM passing, authenticated read-only access and immediate test-share revocation. New-recipient signup/confirmation/acceptance verified with isolated SMTP in three browser projects. Both backup health checks healthy afterward; real calendar-client feed acceptance remains separate. |
| Full staging acceptance | pending | Public OVHcloud HTTPS app and SMTP working, 2026-10-14 | pending | PARTIAL | public-staging.md; public auth/core/worker/restart checks pass, but full browser isolation/device/operations/recovery/beta gates remain open |
| Worker progress/disk operations health and bounded read capacity | d3d81f30cc2a6bbd06d92cb2781d7b5e40a3924d | OVHcloud 4 GB / UptimeRobot / Gmail, 2026-10-14 | Codex and owner monitor setup | PASS for recorded checks | worker-capacity.md: 125 backend tests, patched Pkcs audit, exact CI/isolated compatibility/activation; stale running-worker operations 503 with API readiness 200, recovery and real Down/Up inbox delivery. 200 reads at concurrency five, zero errors, P95 49.9 ms; no OOM/restarts. Fresh encrypted snapshot SQL/key/config restore passed. Sustained/authenticated/fanout/backup-time capacity NOT established. |
| Encrypted VPS snapshot full isolated app recovery and failed-activation rollback | 0acdb2db70a343d1aae977bf4bb19150deaaaf7a runtime; recovery tools added afterward | Fresh local internal Docker project / captured SMTP, 2026-10-14 | Codex | PASS for listed checks | vps-application-recovery.md: hashes/CHECKDB/table comparison, keys/login remapping, copied-account password recovery/login/reset reuse rejection, care/history, worker restart deduplication, exit-42 activation rejection and pinned-runtime rollback preserving cookie/history/keys, logout. Old password/live cookie, real integrations, replacement VPS and schema downgrade NOT accepted. |
| Independent public outage/recovery email alerts | 0acdb2db70a343d1aae977bf4bb19150deaaaf7a runtime | OVHcloud / UptimeRobot / Gmail, 2026-10-14 | Codex and owner monitor setup | PASS for API outage | uptime-monitoring.md: bounded six-minute API stop, independent server recovery timer, public 502 then 200, real readiness-monitor Down/Up emails in INBOX. Worker/backup failure detection is separate; extra unrelated URL monitors need owner cleanup. |
| VPS encrypted backup, scheduled execution and isolated SQL restore | 0acdb2db70a343d1aae977bf4bb19150deaaaf7a runtime; backup tooling added afterward | OVHcloud to external drive / isolated local Docker SQL, 2026-10-14 | Codex | PASS for listed checks | vps-backups.md: manual/scheduled captures, task result 0, encrypted repository check, decryption/all-file hashes, CHECKDB/table comparisons, failure probes and healthy final status. Key/config hashes verified; full restored app/integrations and independent monitoring remain open. Weekly email schedule preserved. |
| Public HTTPS, real email/auth, core care and restart | 0acdb2db70a343d1aae977bf4bb19150deaaaf7a | OVHcloud UK / SQL Express / Brevo / trusted HTTPS, 2026-10-14 | Codex | PASS for listed checks | public-staging.md: two owner-authorized inbox messages, confirmation/reset and session/token rejection, synthetic care/worker/calendar/archive, actual runtime grants, 3/3 public browser checks and restart persistence. Light workload; VPS restore/independent backups and physical devices NOT RUN. |
| VPS host and runtime-image handoff | 0acdb2db70a343d1aae977bf4bb19150deaaaf7a bundle | Ubuntu 26.04 / OVHcloud UK, 2026-10-14 | Codex | PASS for host only | Updated kernel/reboot, key-only SSH, UFW, Docker hello-world/startup, seven payload hashes and both image IDs; staging-host.md. Application acceptance NOT RUN. |
| Prebuilt Linux amd64 runtime packaging | 0acdb2db70a343d1aae977bf4bb19150deaaaf7a | Docker Desktop / Windows, 2026-10-14 | Codex | PASS locally | API/worker build, Linux SHA-256 verification, archive reload and image IDs/runtimes, allowlisted payload, build-free Compose/operator references and no public DB/API ports; budget-staging.md. VPS capacity and public deployment NOT RUN. |
| Keyboard, reflow and sampled axe checks | 5dbcba8 plus accessibility changes | Production HTTPS/SQL/SMTP; Chromium desktop/mobile and Firefox, 2026-10-13 | Codex | PASS locally | Public keyboard checks 3/3 and authenticated journeys/scans 3/3; frontend 9/9. CSP-compatible stylesheet load, focus/error/contrast fixes and disabled-push failure handling. accessibility-beta.md; physical devices, speech output and two normal care cycles remain open. |
| PWA offline shell, consented updates and corrupt-candidate recovery | 762724d plus PWA changes | Real Angular worker / isolated loopback / Docker browsers, 2026-10-13 | Codex | PASS locally | 9/9 PWA cases across desktop/mobile Chromium and Firefox; frontend 6/6; pwa.md. Controlled index variants use current production bundles; physical/native install and separately built historical-release upgrade remain open. |
| Encrypted scheduled backup and isolated SQL restore | 970a568 plus local backup tools | Windows Task Scheduler / external USB drive / Docker SQL, 2026-10-13 | Codex | PASS with capacity warning | Scheduled run result 0; restic data/decryption/all-file hashes, CHECKDB and all-table comparisons; failure probes and minimal Gmail test; local-backups.md. Source disk below 5 GiB warning. Portable key/separate-machine recovery and independent off-site monitoring remain open. |
| Restricted runtime database access and controlled migrations | 073f827 plus database operations changes | Docker SQL Server / HTTPS / SMTP, 2026-10-13 | Codex | PASS | Test-LocalDatabaseAccess.ps1; repeat provisioning, positive/negative ACL and unmanaged-grant checks; full care and isolated restore with login remapping, CHECKDB/table/key/session/worker verification; database-access.md; scheduled backups/alerts and public staging remain open |
| Push failure handling and additive SQL migration | 3214fe0 plus push changes | SQLite/backend and isolated Docker SQL Server, 2026-10-13 | Codex | PASS | 24 encrypted HTTP fault-injection cases on both providers; backend 121/121; existing SQL row counts preserved; push-reliability.md; real device/provider delivery NOT RUN |
| Weekday same-day/DST and saved-zone policy | 37714ea plus weekday changes | Windows backend tests; Docker SQL/SMTP browsers, 2026-10-13 | Codex | PASS | Domain 50/50, API 46/46, application 1/1; extended browsers 3/3; weekday-scheduling.md; public staging/physical devices NOT RUN |
| Browser core care and password-reset forms | 203a0f1 application images plus browser tooling | Docker HTTPS / SQL Server / Mailpit, 2026-10-13 | Codex | PASS | Test-LocalBrowserJourney.ps1; desktop Chromium, mobile Chromium, Firefox: 3/3; browser-journey.md; physical devices, accessibility, PWA and real providers NOT RUN |
| Password reset and session security | 7bb2422 plus auth changes | Docker SQL Server / Windows, 2026-10-11 | Codex | PASS | Test-LocalAuthenticationSecurity.ps1; real SMTP/cookies, reset revocation, antiforgery; API 39/39 includes expiry/real Identity; browser form UX NOT RUN |
| Real-cookie SQL Server account isolation and feed regression | 369187f plus isolation/feed changes | Docker SQL Server / Windows, 2026-10-11 | Codex | PASS | Test-LocalAccountIsolation.ps1; account-isolation.md; API 33/33; provider credential isolation NOT RUN |
| SQLite dependency remediation and backend tests | 48c543a plus dependency changes | Windows / .NET 10, 2026-10-11 | Codex | PASS | Native security baseline; no known vulnerable test dependencies; API 32/32, domain 31/31, application 1/1; see DEP-01 |
| Local database/key-ring recovery and restart | afed937 plus recovery tooling | Docker SQL Server, 2026-10-11 | Codex | PASS | recovery.md; isolated restore/CHECKDB, table/key comparisons, existing cookie/history, worker and restart deduplication; real providers NOT RUN |
| Bootstrap and core care via real HTTP | b099ddc plus bootstrap changes | Docker SQL Server, 2026-10-11 | Codex | PASS | scripts/Test-LocalDeployment.ps1 -IncludeCareJourney; see bootstrap.md |
| Bootstrap/API suite | b099ddc plus bootstrap changes | Windows / .NET 10, 2026-10-11 | Codex | PASS | 31/31 API integration tests |
| Local HTTPS and real-auth smoke | Baseline plus local release fixes | Docker Desktop / SQL Server, 2026-10-11 | Codex | PASS | scripts/Test-LocalDeployment.ps1; registration, confirmation email, login/account/logout |
| API integration suite | Baseline plus local release fixes | Windows / .NET 10, 2026-10-11 | Codex | PASS | 27/27, including migration discovery; SQLite remains a test substitute |
| Frontend unit test and build | Baseline plus local release fixes | Docker Node image, 2026-10-11 | Codex | PASS | 1/1 frontend test; production build succeeded |
