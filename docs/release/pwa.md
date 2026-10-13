# PWA offline and update acceptance

## Behavior

PlantCare caches its application shell and static icons/scripts/styles. It does
not cache account, plant or other API responses, and does not queue care changes
offline. A previously installed shell can reopen offline with an explicit
connection-required message. Offline initialization does not wait through
authentication retries or infer a cached identity. Account data needs a working
connection; a first-ever visit still needs the network to install the shell.

API and health URLs are excluded from service-worker navigation fallback, so an
offline API navigation cannot masquerade as successful cached app HTML.
App navigation uses Angular's cached-version strategy. Authentication and all
account data remain network-only API calls; app navigation needs no server-side
authentication redirect. A live network index cannot bypass the installed
version's verified shell.

Ready updates are offered explicitly. Save unfinished input before choosing
Update now: a full reload switches the document/version together. There is no
automatic reload and no pre-reload activation that could mix old documents and
new lazy chunks. Update checks run when reconnecting or returning to a visible
tab, as well as Angular worker navigation checks. Foreground return reconciles
network state after missed background events. Checks are coalesced; failed
background checks leave the current document usable and retry on a later event.
An unrecoverable cached version offers Reload PlantCare. Reload controls are
disabled offline. See [Angular update communication guidance](https://angular.dev/ecosystem/service-workers/communications).

The manifest has a stable app ID, standalone display and 192/512 PNG icons;
an Apple touch icon is also supplied. PNGs are deterministic raster exports of
the existing SVG. Regenerate them with the browser-test container, mounting
`plant-care-web/public` at `/icons`, and running `/tests/generate-icons.mjs`.
This is asset preparation, not evidence of a native install on a physical device.

## Automated acceptance

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalBrowserJourney.ps1
```

The runner now includes core HTTP/SQL/SMTP journeys plus `pwa.spec.ts` on desktop
Chromium, mobile-sized Chromium and Firefox. The separate loopback server loads
the real production assets, verifies their SHA-1 hashes against the generated
ngsw manifest and serves the real Angular worker. It creates controlled A/B
index/manifest variants and a candidate with an intentionally incorrect index
hash. JavaScript bundles are the actual current build, not separately compiled
historical release bundles.

The fixture binds only inside the proxy network namespace at port 4173; no host
port is published. Loopback is a browser secure-context exception. Core journeys
still exercise HTTPS through Caddy. Fixture API responses are anonymous synthetic
responses; it never proxies API requests or cookies to the real database. Its
control routes are present only in this test process and never in product images.

Tests wait for a document served by the installed worker, not registration
alone. Offline emulation also refuses fixture connections so worker-owned fetches
cannot bypass the simulated outage in a different browser engine.
After reconnect the fixture explicitly dispatches the foreground lifecycle event:
Firefox's offline emulation can change navigator.onLine without emitting online
after an offline cold reload. This exercises the app's foreground reconciliation;
physical-device network event delivery remains a manual beta check.

Checks cover:

- Manifest/icon payload sizes and valid PNG dimensions.
- Cold reload of an installed shell offline, prompt offline feedback, denied API
  access, no API entries in Cache Storage and recovery of the network indicator.
- Real VERSION_READY delivery, unchanged document/input before consent, disabled
  offline update control and explicit reload to B, preserved after another reload.
- Real hash-mismatch installation failure, no ready-update offer, and the prior
  working document remaining available; correcting the candidate restores update
  readiness and a verified offline cold launch after accepting the corrected version.

Traces, videos and screenshots remain disabled so auth captures are not exported.
The PWA fixture uses no real accounts, password resets or private user data.
Frontend unit tests additionally cover ready/unrecoverable signals, failed-check
retry/coalescing, foreground state reconciliation, listener/subscription teardown
and offline authentication startup.

## Manual beta checks still required

Do not mark the complete PWA/device gate accepted from headless tests alone.
Record the actual OS/browser/device versions and candidate SHA for:

1. Desktop Chromium install, standalone launch, icon/name, start URL and uninstall.
2. Physical Android install, standalone navigation, offline/reconnect and update.
3. iPhone/iPad Safari Add to Home Screen, standalone launch and updates if iOS
   support is advertised. Desktop Firefox testing covers web/worker behavior,
   not a native app-install claim.
4. Upgrade from a separately built previous release on public HTTPS, preserving
   cookies, collection/history and navigation with changed lazy-chunk filenames.
5. Cache eviction, failed/interrupted deployment and recovery after correcting
   server assets; avoid deploying an index whose referenced chunks are unavailable.
6. Keyboard/focus/screen-reader announcements, physical screen layouts and two
   normal care cycles. Real push permission/delivery is a separate acceptance gate.

Keep old versioned assets available during deployments. A corrupt deployment can
prevent new installs or cold launches even when an already open old tab survives.
Angular enters EXISTING_CLIENTS_ONLY after the tested hash-mismatch failure;
correct the server assets and validate installation before requiring new launches.
This test fixture is not a public-host deployment/rollback exercise.
