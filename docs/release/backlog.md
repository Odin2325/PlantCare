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
| BOOT-01 | P1 | Define production administrator/catalog bootstrap | Fresh staging can obtain an administrator and initial species through a documented controlled procedure; no public development-mode bootstrap. |
| CARE-01 | P1 | Decide same-day weekday scheduling behavior | A selected weekday with a preferred time later today has a documented expected result and a regression test. Current loop starts at tomorrow. |
| PUSH-01 | P1 | Validate and harden partial push failures | Multiple subscriptions, expired endpoints, repeated failures, and restarts do not cause uncontrolled resends or block the pending queue indefinitely. |
| DATA-01 | P1 | Validate migrations and recovery on SQL Server | Empty-database and upgrade migration checks pass; database plus key-ring restore drill succeeds. |
| CI-01 | P1 | Add executable release gates | Browser core journey, real-auth coverage, SQL Server migrations, API/worker startup smoke tests, and required merge checks are in place. |
| OPS-01 | P1 | Prepare production access and operations | Restricted runtime DB credentials, controlled migrations, alerts, backup schedule, and rollback procedure are documented and tested. |
| UX-01 | P1 | Complete beta acceptance | Supported-device matrix, accessibility/core journey, PWA update, and two normal care cycles pass with no open blockers. |
| CAL-01 | P1 if enabled | Validate external calendar providers | Both real-provider success and failure paths pass; otherwise leave the unvalidated provider disabled at launch. |
| RELEASE-01 | P1 | Prepare v1 distribution and user documentation | Version/tag/artifacts, release notes, support/privacy information, license decision, and known limitations are complete. |

## Local checks performed

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
