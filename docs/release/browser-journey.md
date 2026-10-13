# Local browser release checks

The core journey in the dedicated `tests/browser` package runs Playwright against the production
Angular assets and API through local Caddy HTTPS, with SQL Server and Mailpit.
It does not mock application requests, seed authentication cookies, or disable
antiforgery. All tested users register through the UI and remain ordinary users.
The same runner includes a separate anonymous loopback fixture for real-worker
offline/update checks; see [pwa.md](pwa.md) for its isolation and acceptance limits.
It also includes strict axe scans and keyboard/reflow checks; see
[accessibility-beta.md](accessibility-beta.md). This does not replace a manual
screen-reader/device evaluation or the two normal beta care cycles.

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalBrowserJourney.ps1
```

PowerShell 7 and Docker running Linux containers are required. The browser runner
has its own locked dependencies and matching Playwright 1.64.0 image; host Node
and Angular tooling are not required. The runner shares the local proxy's network
namespace, so captured links and browser requests retain `https://localhost:8443`.
Only that fixed origin is accepted for email links. Mailpit is reached privately
at `http://mailpit:8025`. Certificate validation is bypassed for this local CA only.
See [Playwright's Docker guidance](https://playwright.dev/docs/docker).

The script transactionally imports `deploy/catalog.example.json` before running.
This is synthetic data, not reviewed botanical guidance; existing matching
species are retained. It never promotes browser users or accesses Windows SQL.
Use `-SkipBuild` only when the runner image already matches the current test files.
Build the application again when testing changes to production code.

Each of desktop Chromium, Pixel 7-sized mobile Chromium, and desktop Firefox
executes a fresh journey with a separate generated account:

- Protected-route redirect, registration, rejection before confirmation, actual
  captured confirmation link, sign-in return URL and persistence after reload.
- Catalog/detail navigation, plant creation with location/tags and a past initial
  watering date, persisted tag changes and interval scheduling.
- Overdue dashboard, care completion, history record/edit/delete, and completed
  calendar agenda entries.
- Archive/restore, persistence after reload, password-reset email and forms,
  mismatched confirmation feedback, old-password rejection, new-password sign-in,
  retained collection, logout/protected-route rejection and incomplete reset link.
- Weekday configuration and displayed time zone, then a second context in
  America/New_York signing in normally and pausing a Europe/Berlin schedule
  without changing its zone; original-browser reload and resume.
- No uncaught browser exceptions during the journey.

Production authentication limits remain enabled. Only rejected (429) form
submissions are retried, with bounded 20-second waits; tests run sequentially
with no whole-test retries. A run can therefore take several minutes. CI runs
the same script after the HTTP account-isolation and reset-security checks and
removes its disposable Compose volumes afterward.

Successful local runs leave synthetic accounts/messages and archived plants in
the isolated Docker database. Failed runs can leave active fixtures. Browser
contexts and the runner container are discarded. Traces, videos, screenshots,
storage-state files and report uploads are disabled because authentication
captures may contain passwords, reset links or cookies. Diagnostics should name
the failed step without publishing sensitive payloads.

This is functional browser regression coverage. Mobile emulation does not prove
real Android/iOS behavior. Safari/WebKit, accessibility and visual review, native
PWA install/update/offline behavior, real push and calendar providers, weekday/DST
boundaries on physical devices, concurrent submissions and two normal care cycles remain separate
acceptance work. Public staging and the broader v1 checklist remain open.
