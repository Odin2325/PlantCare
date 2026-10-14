# Release backlog and baseline verification

Assessed 2026-10-11 at `969d8c074b8a399d05a32cffc33c2cb1d3eeff52`.
These IDs are local backlog references. TEST-01 is also tracked in
[GitHub issue #39](https://github.com/Odin2325/PlantCare/issues/39).
Work in the order below. P0 blocks the next validation stage; P1 blocks public
release unless the related optional feature is disabled.

| ID | Priority | Work item | Acceptance condition |
| --- | --- | --- | --- |
| TEST-01 | P0 | Repair API integration test provider replacement | All 26 existing API tests execute and pass. Test setup removes the production provider configuration before registering SQLite; SQL Server remains the production provider. |
| ENV-01 | P0 | Restore a usable frontend toolchain | Installed Node satisfies the locked Angular CLI requirement; pinned pnpm installs with a frozen lockfile; frontend tests and production build pass. |
| STAGE-01 | P0 | Establish an isolated staging environment | Dedicated Linux host/domain, Docker Compose, staging-only SMTP/VAPID credentials, and empty isolated volumes are available; follow staging.md and attach results. |
| DEP-01 | P1, resolved locally 2026-10-11 | Fix SQLite test dependency advisory | SQLitePCLRaw bundle/native library updated from 2.1.11 to 2.1.13; audit reports no known vulnerable test dependencies; native SQLite security baseline and all backend tests pass. See dependency follow-up below. |
| AUTH-01 | P1, resolved locally 2026-10-11 | Validate reset/session security and fix bearer antiforgery exemption | Real SQL/SMTP reset checks pass; invalid bearer headers cannot bypass antiforgery; reset invalidates browser sessions on their next request; real-Identity regression tests pass. |
| BOOT-01 | P1 | Define production administrator/catalog bootstrap | Fresh staging can obtain an administrator and initial species through a documented controlled procedure; no public development-mode bootstrap. |
| CARE-01 | P1, resolved locally 2026-10-13 | Define same-day and clock-change scheduling behavior | Today's future time is eligible without prior care; completion satisfies its local date; gap/repeated-time rules, saved-zone preservation and calendar agreement have regression coverage. See weekday-scheduling.md. |
| PUSH-01 | P1, failure handling verified locally 2026-10-13 | Validate and harden partial push failures | Durable per-device outcomes, expiry/removal, bounded retries/fanout and interrupted-pass persistence pass SQLite/SQL fault-injection tests. Real browser/provider delivery remains open; see push-reliability.md. |
| DATA-01 | P1 | Validate migrations and recovery on SQL Server | Empty-database and upgrade migration checks pass; database plus key-ring restore drill succeeds. |
| CI-01 | P1 | Add executable release gates | Browser core journey, real-auth coverage, SQL Server migrations, API/worker startup smoke tests, and required merge checks are in place. |
| OPS-01 | P1, database access verified locally 2026-10-13 | Prepare production access and operations | Restricted runtime DB credentials, controlled migrations, alerts, backup schedule, and rollback procedure are documented and tested. See database-access.md; scheduled backups/alerts and public-host drills remain open. |
| UX-01 | P1 | Complete beta acceptance | Supported-device matrix, accessibility/core journey, PWA update, and two normal care cycles pass with no open blockers. |
| CAL-01 | P1 if enabled | Validate external calendar providers | Both real-provider success and failure paths pass; otherwise leave the unvalidated provider disabled at launch. |
| RELEASE-01 | P1 | Prepare v1 distribution and user documentation | Version/tag/artifacts, release notes, support/privacy information, license decision, and known limitations are complete. |

## Local checks performed

### First public staging deployment (2026-10-14)

- Deployed the prebuilt `0acdb2d` bundle on the prepared OVHcloud VPS, with
  fresh private credentials, Brevo STARTTLS/SMTP authentication, staging VAPID
  keys and a 2048 MiB SQL memory setting. All migrations/runtime provisioning
  passed. Actual restricted API/worker permission checks and trusted public
  HTTPS/DNS/SPA/health routing passed. See [public-staging.md](public-staging.md).
- Owner authorized both app test emails to their address. Confirmation and reset
  arrived in Gmail INBOX; confirmation/login, password change, old password/reused
  reset rejection, old-session revocation and logout passed. No secrets/links
  are published. Provider tracking redirects are a privacy-configuration follow-up.
- Synthetic demo species and care journey passed: catalog write denied to the
  ordinary test account, plant/dashboard, worker reminder, completion/next due,
  history/calendar, archive. Fixed read permissions only on the non-secret deploy
  mount so the non-root catalog operator can read it.
- Existing public-page keyboard/reflow/axe browser checks passed 3/3 on actual
  HTTPS staging with certificate verification. SQL/API/worker restart preserved
  reminders and the existing login session. Roughly 2.5 GiB host RAM available
  and 32 GiB disk free after light tests; no observed OOM/automatic restarts.
- Public staging exists; full acceptance, real devices/push, sustained capacity,
  VPS backups/recovery/outage monitoring and release packaging remain open.

### Delivered VPS preparation (2026-10-14)

- Configured the purchased OVHcloud Ubuntu 26.04 x86-64 host: Ubuntu/kernel
  updates and verified reboot, official Docker Engine 29.9.0/Compose v5.6.0,
  hello-world container, automatic security updates, owner-only staging directory.
- Verified key-based administrative SSH before disabling password/root login.
  Enabled UFW with SSH restricted to the current administrator source and web
  ports allowed; cancelled a timed rollback only after fresh login succeeded.
  Rebooted and rechecked effective SSH settings, firewall and Docker boot startup.
- Uploaded the original `0acdb2d` runtime bundle; all seven SHA-256 checks and
  both remote image IDs match the original manifest. Staging DNS points to the
  server. No SQL/API/worker/Caddy app containers are running yet.
- [staging-host.md](staging-host.md) records evidence and access maintenance.
  SMTP/domain authentication, public HTTPS/app acceptance, capacity under load,
  provider firewall and independent host backups/monitoring remain open. Owner
  requested a free SMTP option; Brevo is recommended, not yet configured.

### Budget VPS and prebuilt-image handoff (2026-10-14)

- Owner selected OVHcloud VPS-1 2027 in the UK with Ubuntu 26.04, 4 GB RAM,
  40 GB NVMe and one-month checkout EUR 5.34 including VAT. Total monthly budget
  is EUR 15. Delivery/validation is pending; remote access and tests are NOT RUN.
- Added [budget-staging.md](budget-staging.md) and `Export-StagingBundle.ps1`:
  committed-source-only Linux amd64 API/worker images, allowlisted runtime files,
  image IDs and Linux-compatible SHA-256 checksums. No local data/secrets are
  copied; no source-build fallback in exported Compose. Existing bundles are
  preserved. Staging continues to require SMTP, public DNS and remote operations.
- Exported candidate `0acdb2db70a343d1aae977bf4bb19150deaaaf7a` locally: 110.6 MiB
  image archive. Both builds, all seven Linux payload checksums, archive reload,
  image ID/runtime checks, eight-file allowlist, build-free Compose validation
  (including operator profile) and existing-bundle refusal passed. Temporary
  exporter scratch was removed. Local application services were not restarted.
- Capacity on 4 GB/40 GB, public journeys and VPS backups/recovery remain open;
  local Windows backups do not cover the new host. STAGE-01/OPS-01 remain open.

### Domain and public-staging handoff (2026-10-13)

- Owner purchased `plantacare.org` at Porkbun. Public DNS still points the apex
  and staging name at Porkbun parking. No public app host is provisioned.
- Prepared [domain-hosting.md](domain-hosting.md) with a compatible x86-64 server
  recommendation, staging-only A record, firewall, SMTP and access requirements.
- Added a read-only `Test-StagingEndpoint.ps1` check that requires the actual
  server IPv4 before checking DNS, trusted HTTPS, redirects, readiness and SPA/API
  routing. PowerShell parsing, invalid-IP/URL rejection and refusal of the current
  parking DNS passed. Public success-path execution is NOT RUN until a server
  exists. STAGE-01 stays open.
- Prior candidate `2c00bb2` completed Frontend, Backend and Production container
  CI successfully in run `38468089723`; this does not establish public acceptance.

### Keyboard/accessibility and beta follow-up (2026-10-13)

- Added a keyboard-visible skip link, route main landmarks, post-render heading
  focus (main fallback while loading), active navigation announcements, strong
  focus indicators and reduced-motion styling. Auth fields expose required/invalid
  states and associated instructions/errors; feedback uses alert/status roles.
- Fixed Register-link, helper/schedule and calendar-date/completed-entry contrast,
  and labelled the add-care-action selector. Browser checks cover 320-pixel reflow,
  keyboard navigation/Enter, inline error descriptions and visible focus.
- Found Angular's generated print-media stylesheet activation handler being
  blocked by the script CSP. Disabled critical-CSS inlining while retaining
  script/style minification and the existing CSP; no script permission was relaxed.
  Public-page tests assert no policy violations.
- Added pinned axe 4.13.0 scans to real-auth collection, notifications, dashboard,
  history, all calendar views and reset forms, plus public pages and PWA prompts.
  Selected WCAG A/AA rules fail on any violation, with no ignored rules/regions.
  Only rule IDs/impact/counts are logged; private page/field data is withheld.
- Visiting notifications exposed Firefox rejecting an unnecessary push-status
  query while server push was disabled. Disabled push now skips the query;
  enabled lookup errors are handled and lookups cancelled on page destruction.
  Three regression tests cover those cases; frontend suite passes 9/9.
- [accessibility-beta.md](accessibility-beta.md) records scope, manual device/
  screen-reader/zoom checks and two actual care-cycle evidence slots. Headless
  audits do not establish complete WCAG conformance or native/mobile acceptance.
  UX-01, physical-device/provider tests and public staging remain open.

### PWA offline/update follow-up (2026-10-13)

- Added 192/512 PNG manifest icons and an Apple touch icon rendered from the
  existing SVG; stable app ID and explicit offline/cache policy are documented.
- Offline cold startup renders without authentication retry delay or cached
  identity. No API data is cached or care writes queued. API/health navigations
  are excluded from cached HTML fallback; app navigation stays with the verified
  cached version rather than a live network index.
- Updates use a consented full reload, with unfinished-input guidance and
  disabled offline controls. Reconnect/foreground checks coalesce and retry
  failures; foreground reconciles missed network events. Unrecoverable state
  offers reload. Destroyed services remove browser listeners/subscriptions.
- Nine real-worker checks passed across desktop Chromium, mobile-sized Chromium
  and Firefox using verified production assets with controlled A/B index variants:
  offline cold reload, PNG sizes, API non-caching, version-ready/consent, input
  preservation before reload, corrupt candidate rejection and corrected-deployment
  recovery. Six frontend tests and production image builds pass.
- Initial fixtures were corrected to wait for a worker-served document and to
  refuse connections for worker-owned requests. Firefox offline emulation omits
  online events after a cold reload; the fixture explicitly exercises foreground
  reconciliation, not physical-device event delivery. A corrupt Angular manifest
  enters EXISTING_CLIENTS_ONLY: old open tabs survive, while cold launches may
  fail until assets are corrected. This limit is documented, not treated as an
  unconditional offline recovery guarantee.
- The combined core/PWA runner is part of CI. [pwa.md](pwa.md) provides manual
  beta checks for native installation, physical Android/iOS, actual separately
  built release upgrades, public HTTPS, cache eviction and accessibility.
  UX-01 and the complete PWA acceptance gate remain open. No Windows SQL,
  user clock, production authentication limits or external providers were changed.

### Encrypted scheduled-backup follow-up (2026-10-13)

- Initialized a restic-encrypted repository on the owner's selected external USB
  drive. A pinned official Windows binary is SHA-256 checked. The random password
  is DPAPI-protected in an ACL-restricted, ignored local state directory.
- Captured SQL COPY_ONLY/CHECKSUM/VERIFYONLY backup, matching key ring and private
  configuration in one encrypted snapshot. Application writers stop only during
  capture and restart before encryption; per-run plaintext is cleaned up.
- Registered a daily 03:00 Berlin Windows task under the logged-in owner.
  Its first real scheduled run completed with result 0. Decrypted that snapshot,
  verified all file SHA-256 hashes, restored SQL in a fresh isolated container,
  passed CHECKDB and compared every user table's counts/checksums.
- Failure probes passed for stale backups, missing destinations, disk pressure,
  failed jobs and wrong passwords; a healthy-state probe also passed with only
  the known source-capacity warning threshold temporarily overridden for testing.
  The default threshold remains active. No Windows SQL or source restore target
  was modified. Private plaintext scratch and disposable SQL resources were removed.
- Found actual source-disk free space below 5 GiB. The owner must free reviewed
  files/caches; no unrelated files were deleted. An hourly local Codex heartbeat
  checks health and sends deduplicated failure/recovery emails through the
  connected Gmail account. A minimal warning/test email was sent successfully.
- [local-backups.md](local-backups.md) records requirements and limitations.
  The owner still needs a separately preserved portable recovery password and
  separate-machine recovery test. Off-site/immutable copies, independent outage
  alerting, accepted recovery targets and public-host drills remain open.

### Restricted database access follow-up (2026-10-13)

- Replaced runtime `sa` connections with separate API/worker logins and explicit
  table grants. Neither can change schema, migration history or Identity roles;
  the worker cannot read Identity accounts or update the species catalog.
- Migrations/role seeding, credential provisioning and administrator/catalog
  bootstrap run through an explicit operator-only one-shot service. Normal
  Compose API startup migrations are disabled. Local startup builds first,
  stops writers, runs operator steps and recreates runtime containers only
  after success. Repeat provisioning succeeds; unexpected grants/roles fail closed.
- Real SQL checks validate both principals, required grants, denied operations,
  invalid passwords and rejection/cleanup of a temporary unmanaged grant.
  CI runs the same permission checks before real-cookie account isolation.
- The full core-care and isolated recovery journey passed with restricted
  accounts: CHECKDB, table counts/checksums, key hashes, existing session/history,
  worker processing and restart deduplication. Restore recreates server logins
  and remaps database users before application startup. Private evidence remains
  in ignored `recovery-artifacts/d343d58ebb44/`. Windows SQL was untouched.
- [database-access.md](database-access.md) records policies, staging sequence and
  recovery requirements. The owner reconfirmed no host/domain and requested local
  preparation. Scheduled encrypted off-host backups, alerts, secret storage,
  physical-device/provider acceptance and public staging remain open.
- The first candidate CI passed fresh SQL deployment/permissions, isolation,
  authentication and push checks, then exposed a mobile browser-harness race:
  it selected an event while an outstanding calendar view load could clear the
  selection. The journey now waits for initial/view loading to finish before
  selecting the event, without increasing timeouts or enabling retries.

### Push failure/retry follow-up (2026-10-13)

- Found notification-level retries resending to already successful devices and
  repeatedly failing oldest notifications retaining eligible batch slots.
  Added per-notification/device outcome persistence, deferred queue eligibility,
  five-attempt backoff, Retry-After support, 15-second HTTP timeout, fanout/pass
  budgets, stale/preference suppression and one-hour provider retention TTL.
- Provider acceptance, expired subscriptions, permanent failure and skipped
  devices are terminal. Caller cancellation propagates. Each result is committed
  before sending to another device; an interrupted pass preserves earlier successes.
- 24 fault-injection cases pass on SQLite and generated disposable Docker SQL
  databases, using real WebPush encryption/HTTP mapping with a local handler.
  Coverage includes mixed success, 404/410, 4xx/redirects, repeated 5xx, batch-one
  fairness/large fanout, network/timeout failures, both Retry-After formats,
  new/removed devices, disabled push, stale/preference state and cancellation.
- Backend checks: API 70/70, domain 50/50, application 1/1 (121 total).
  SQL tests apply all migrations and remove only their generated databases.
  CI runs both provider variants plus the existing security/browser suites.
- Upgraded the preserved local database with additive migration
  `20261013110834_AddPushDeliveryRetries`. Before/after counts matched: 38 users,
  29 plants, 27 care events, 18 notifications. API/worker production images built
  and restarted successfully. Windows SQL was untouched.
- Post-upgrade production smoke passed real SMTP/cookie sign-in, ordinary/Admin
  bootstrap boundaries, core care/history/calendar, worker reminder generation
  and protected logout (`Test-LocalDeployment.ps1 -IncludeCareJourney`).
- [push-reliability.md](push-reliability.md) records migration/rollback, safe
  monitoring queries and limits. Use one worker; there is no cross-process lease
  or exactly-once guarantee across provider acceptance and database commit.
  Real provider credentials/browser permissions and physical-device delivery,
  production alerts and retention remain open. The whole reminder gate is not accepted.
- First pushed CI run passed backend/frontend and SQL isolation, then exposed a
  fresh PowerShell-session dependency in the existing reset-security harness:
  WebRequestSession was used before its module was loaded. The HTTP harnesses
  now explicitly import Microsoft.PowerShell.Utility before constructing it.
  No application authentication policy was relaxed.

### Weekday/time-zone follow-up (2026-10-13)

- Defined same-day behavior: new schedules can use today's selected future time;
  exact/past instants move forward. Completion/history recalculation satisfies
  the entire completed local date, so early care does not create a second
  reminder that day. Prior care remains the anchor for overdue schedules.
- Spring gaps advance to the first valid local minute; repeated autumn times
  use one later UTC occurrence. Calendar recurrence must always advance strictly
  in UTC. Historical date-line changes remain outside this acceptance matrix.
- Fixed saving/pausing weekday schedules silently replacing the saved zone with
  the current browser's zone. The UI now preserves and displays the saved zone
  and explains same-day/clock-change behavior.
- Added 19 domain cases, including Berlin/New York, Lord Howe half-hour changes,
  local/UTC midnight, month/year changes, completion/history, explicit zone change,
  and interval conversion. Added seven API cases for stored
  same-day instants/calendar agreement, completion, invalid-zone non-write and
  recurring spring/fall calendar dates. See [weekday-scheduling.md](weekday-scheduling.md).
- Added the previously omitted API integration project to `PlantCare.slnx` so
  the normal backend CI command executes those checks on Linux too.
- Local backend checks pass: domain 50/50, API integration 46/46, application
  1/1. Desktop Chromium, mobile Chromium and Firefox pass the extended SQL/SMTP
  journey (3/3, about 3.4 minutes), including a second real sign-in from a New
  York context and saved Berlin-zone pause/resume. Production images build.
  No host clock or Windows SQL changes were made. Public staging, physical
  device clock changes and real push timing remain unverified.
- Feature implementation should resume after stable beta acceptance, keeping v1
  scope fixed until its tag. Remaining priorities: reminder/push failure handling,
  public staging and operations, supported-device/PWA checks and two care cycles,
  then release packaging. Collect and prioritize feature ideas during this work.

### Browser core-journey follow-up (2026-10-13)

- `Test-LocalBrowserJourney.ps1` passes all three projects against `203a0f1`
  production application images plus browser tooling: desktop Chromium,
  Pixel 7-sized mobile Chromium and desktop Firefox. Final local run: 3/3,
  approximately 1.3 minutes, real local HTTPS/SQL Server/Mailpit.
- Ordinary users register and follow captured confirmation links through the
  browser; login before confirmation is rejected. Coverage includes guarded
  routes, sign-in/reload, catalog/details, plant creation with past care date,
  persisted tags/interval, overdue dashboard completion, history record/edit/
  delete, completed agenda entry, archive/restore/reload, forgot/reset forms,
  mismatched confirmation, old/new passwords and logout.
- Added a separate locked Playwright 1.64.0 package and matching Docker runner,
  independent of the incompatible host Angular toolchain. CI runs the same
  journey after HTTP security checks, with existing disposable-volume cleanup.
- Initial failures identified harness timing/locator problems: fields shared
  between routes could be filled before navigation finished; reload could abort
  restore; the calendar's wrapping label required a prefix match. Explicit
  destination/saved-state checks fixed these; no production code changed.
- Authentication rate limits remain enabled; only rejected form submissions
  have bounded waits. No uncaught page exceptions occurred in the final run.
  No authentication captures are exported. Synthetic accounts/messages and
  archived plants remain locally; Windows SQL is untouched.
- [browser-journey.md](browser-journey.md) records execution and scope. CI-01
  now has core browser coverage; required merge checks remain open. UX-01 still
  needs physical-device/accessibility/visual review, PWA behavior, date boundaries
  and two normal care cycles. Public staging and real providers remain open.

### Password-reset and session-security follow-up (2026-10-11)

- `Test-LocalAuthenticationSecurity.ps1` passes on `7bb2422` plus auth changes:
  real SQL Server, HTTPS, captured SMTP links, confirmed/unconfirmed login,
  two browser cookie jars, Secure/HttpOnly/SameSite flags, antiforgery failures,
  neutral forgot-password responses, reset-page routing, invalid/wrong-account/
  weak/reused-code rejection, old/new password behavior, and protected logout.
- Confirmed two failures before fixing them: a bogus Bearer header bypassed
  antiforgery on anonymous Identity endpoints; old cookies remained authenticated
  after reset during the default security-stamp validation interval.
- Antiforgery exemption now requires successful Identity bearer authentication.
  Cookie security stamps are checked each request so reset/stamp changes revoke
  old browser sessions immediately. This adds an Identity DB validation cost per
  authenticated cookie request; production sizing should account for it.
- API suite passes 39/39. Six added tests use real Identity rather than simulated
  users, covering invalid/valid bearer requests, reset cookie/refresh-token
  revocation and reuse, cross-confirmed-account codes, expired reset codes,
  and expired cookies. Expiry tests do not change host clocks or production TTLs.
- CI now runs the SQL/SMTP security script after account isolation. Existing IP
  rate limits are preserved; the scripts use bounded waits. Windows SQL is
  untouched. Synthetic accounts and messages remain locally.
- [authentication-security.md](authentication-security.md) records limits:
  normal logout clears the current browser cookie, while copied-ticket/per-device
  server-side revocation is not added; old bearer access tokens can last until
  expiry although their refresh tokens fail after reset. Browser form UX and
  real-provider tests remain open.

### SQL Server account-isolation follow-up (2026-10-11)

- `Test-LocalAccountIsolation.ps1` passes against `369187f` plus isolation/feed
  changes using three confirmed ordinary users, real cookies/antiforgery, HTTPS,
  Mailpit, and Docker SQL Server. Windows SQL Server remains untouched.
- Bidirectional checks cover account identity and scoped collection/dashboard/
  calendar/notification lists; plant/tags, schedule, completion, history,
  mixed plant/event IDs, archive/restore, notification read state, preference
  ownership, and push subscription takeover/deletion. Rejected requests leave
  owner state intact. All accounts are denied administrator catalog writes.
- Calendar shares admit the explicit recipient, deny an unrelated observer,
  preserve private plant isolation, and stop working after owner revocation.
  Bearer feed downloads contain only their owner's data; rotation/revocation
  invalidate old URLs without revoking another account's feed.
- Found/fixed valid calendar feeds returning 400: their -30/+365-day window
  exceeded CalendarService's 366-day limit. The window now spans 30 days of
  history and 335 days ahead. A regression test verifies download, content scope,
  no-store caching, rotation, revocation, and another feed's continued access.
- API integration tests pass 33/33. Added the same real-cookie SQL isolation
  script to production-container CI, with disposable SQL data and cleanup.
  CI-01 remains open for browser coverage and required merge checks.
- The harness respects the existing authentication rate limit with bounded
  waits; no production rate limit was relaxed. Synthetic accounts/archived plants
  remain locally; successful runs revoke shares/feeds and remove test push data.
- See [account-isolation.md](account-isolation.md). Real Google/Microsoft
  credential isolation, real browser push, concurrency, and browser acceptance
  remain unverified, so the broader acceptance gates remain unchecked.

### SQLite dependency follow-up (2026-10-11)

- DEP-01 resolved on `48c543a` plus dependency changes. The affected chain was
  `Microsoft.EntityFrameworkCore.Sqlite 10.0.10` ->
  `SQLitePCLRaw.bundle_e_sqlite3 2.1.11` -> native library 2.1.11.
  A direct test-project reference to bundle 2.1.13 upgrades the bundle, provider,
  core, and native library together without changing EF or production SQL Server.
- [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)
  affects the old native package and identifies SQLite 3.50.2 as the patched
  baseline. The new runtime test queries `sqlite_version()` through a real
  connection and rejects engines below that baseline.
- NuGet restore completes without NU1903. Explicit transitive auditing is enabled
  for the integration-test project; high/critical audit warnings NU1903/NU1904
  now fail restore, including the existing backend CI restore step. No advisory
  suppression was added.
- `dotnet list PlantCare.Api.IntegrationTests/PlantCare.Api.IntegrationTests.csproj
  package --vulnerable --include-transitive --no-restore` reports no vulnerable
  packages against current sources. `dotnet nuget why` confirms native 2.1.13.
- Release tests pass: API integration 32/32, domain 31/31, application 1/1.
  The existing CalendarShareService nullable warning CS8602 remains separate.
  This audit result is time-specific, not a claim that all dependencies are free
  of undiscovered vulnerabilities.

### Backup and recovery follow-up (2026-10-11)

- DATA-01 local recovery drill passes on `afed937` plus recovery tooling:
  `Test-LocalDeployment.ps1 -IncludeCareJourney -IncludeRecovery`.
- Source SQL/API/worker restart preserved an existing real login and care history.
  The worker created a new overdue reminder and did not duplicate it after restart.
- Took a SQL `COPY_ONLY` backup with checksums and matching data-protection keys;
  restored into a fresh isolated Docker project. `DBCC CHECKDB`, every user-table
  row count/aggregate checksum, and individual key SHA-256 hashes matched.
- Restored HTTP account/history access worked with the existing cookie without
  logging in again. The restored worker also generated a new reminder without
  duplication across restart. Full auth/core-care smoke completed successfully.
- Private evidence is retained in ignored `recovery-artifacts/a964abebc96a/`;
  temporary restore containers/volumes were removed. Windows SQL was untouched.
- Added image-availability preflight after encountering a running worker whose
  old image had been removed locally; recreated it from the current built image.
- [recovery.md](recovery.md) documents execution, cleanup, proposed retention,
  and rollback. Off-host encrypted storage, scheduled jobs/alerts, public staging,
  measured recovery targets, and real-provider credential recovery remain open;
  DATA-01/OPS-01 are not fully accepted.

### Bootstrap and core-care follow-up (2026-10-11)

- BOOT-01 implementation complete: explicit offline `--bootstrap-admin` promotes
  only a confirmed existing account; `--import-catalog` validates JSON and imports
  transactionally without overwriting existing species. Reviewed production
  catalog content still needs to be supplied; the included example is synthetic.
- API integration suite passes 31/31, including missing/unconfirmed-account
  rejection, repeatable promotion/import, and invalid-input non-write behavior.
- `Test-LocalDeployment.ps1 -IncludeCareJourney` passes against Docker SQL Server:
  ordinary-user catalog denial, Admin promotion and catalog update, repeated
  import, plant/tags, overdue dashboard, worker reminder/read acknowledgement,
  completion, next due date, history, calendar, and archive behavior.
- Existing Windows SQL Server at `localhost` was inspected with Windows
  authentication and already contains PlantCare. No changes were made there.
  The isolated Docker database is exposed only at `localhost,14330` for SSMS.
- Push/calendar provider checks, cross-user checks on real SQL Server, browser
  usability, restart/recovery drills, and the SQLite advisory remain open.

### Follow-up after Docker installation (2026-10-11)

- TEST-01 fixed locally: remove retained provider configuration, seed actual
  Identity users, store UTC date ticks in the SQLite test model, and refresh
  antiforgery tokens when switching users. The deliberate missing-token test
  remains intact. API suite now passes 27/27, including migration discovery.
- Frontend production build and 1/1 frontend test pass in the Docker Node image;
  the installed host Node remains incompatible (ENV-01 host setup remains open).
- Docker Desktop 4.94.0 / engine 29.8.2 runs the isolated `plantcare-local` stack.
- Fixed worker final image to include ASP.NET runtime; initialized key directories
  with non-root ownership; repaired the initially created local key volume without
  deleting data. CI now builds/checks the worker runtime too.
- Discovered and fixed five migrations missing discovery attributes. SQL Server
  startup now applies them and worker notification queries succeed. This validates
  initial deployment plus upgrade from the incomplete local schema; production
  backup/restore and other preexisting schemas remain untested.
- Fixed double HTML encoding of Identity confirmation/reset links. The local
  HTTPS smoke test passes registration, captured SMTP email, rejection before
  confirmation, confirmation, cookie login, current account, and protected logout.
- SQLite advisory DEP-01, production bootstrap BOOT-01, real push/calendar tests,
  full care journey, recovery drills, and public staging remain open. Original
  baseline results below are retained as historical evidence.

| Check | Result | Evidence |
| --- | --- | --- |
| Working tree at start | Clean | `git status --short` returned no changes. |
| Domain tests, Release | PASS | 31 passed, 0 failed, 0 skipped. |
| Application tests, Release | PASS | 1 passed, 0 failed, 0 skipped. Build also emitted CS8602 in CalendarShareService.cs at line 21; review nullable handling. |
| API integration tests, Release | FAIL | 26 failed, 0 passed; setup throws `Multiple relational database provider configurations found. A context can only be configured to use a single database provider.` at ResetDatabaseAsync. Individual test behaviors were not exercised. |
| Frontend tests | BLOCKED | Installed Node v24.13.0; installed Angular CLI 22.1.8 requires v22.22.3, v24.15.0, or v26.0.0 minimum in its respective supported major. CLI exited before running tests. |
| pnpm invocation | BLOCKED in sandbox | `pnpm --version` reported access denied while canonicalizing the repository directory. Direct Angular invocation independently confirmed the Node incompatibility. |
| Container/staging checks | NOT RUN | Docker was not on PATH or at its standard Docker Desktop location; owner confirmed no staging host exists. No public domain or staging credentials were configured. |
| NuGet restore | Completed after network-enabled retry | Initial sandbox DNS failure was environmental; retry restored dependencies and emitted NU1903 (DEP-01). |

Commands used for successful domain/application execution after compilation:

```powershell
dotnet test PlantCare.Domain.Tests/PlantCare.Domain.Tests.csproj --configuration Release --no-build --verbosity minimal
dotnet test PlantCare.Application.Tests/PlantCare.Application.Tests.csproj --configuration Release --no-build --verbosity minimal
```

API reproduction command:

```powershell
dotnet test PlantCare.Api.IntegrationTests/PlantCare.Api.IntegrationTests.csproj --configuration Release --verbosity minimal -p:UseSharedCompilation=false -m:1
```

The earlier successful GitHub CI run is historical evidence, not a substitute
for these local results or acceptance of a new candidate. Investigate dependency,
platform, and test registration differences before claiming parity.
