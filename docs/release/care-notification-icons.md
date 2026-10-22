# Care action notification icons

Owner revision 2026-10-22: new reminders use small cartoon-style care illustrations
instead of species photographs. Watering uses a blue drop, fertilizing a plant-food
bottle, misting a spray bottle, pruning shears, and repotting a pot with soil.
Unknown legacy actions retain the PlantCare app-icon fallback. PNGs are static,
transparent 192px originals with editable SVG sources in `public/icons`.

Plant nickname/common-name text, reminder title/tag, preferences, encrypted
transport, expiry/retries and Dashboard tap behavior remain. Payloads no longer
include species-photo credits or photo metadata. Catalog/detail photographs and
their credits are unchanged. No migration, new service or runtime grant is needed.
The worker still reads species for the common-name fallback.

Existing delivered notifications retain their old image. Newly delivered ones
use the action icon. Device support controls rendering and the separate system
status-bar badge; this does not add animation to the notification. No repeated
permission request or reinstall is required. Accept Update now if offered to
cache the added application assets.

Regression coverage checks all five actions, unknown-action fallback, nickname,
Dashboard action, omitted photo metadata and bounded payload size, including
delivery through encrypted transport with catalog photo metadata present.
PWA browser coverage checks the five 192px PNGs remain available offline from
the installed service worker. Physical-device rendering requires owner acceptance.

Local verification passed: 152 backend tests and 45 selected real SQL Server
push/account-data/erasure checks, including encrypted transport and all action
mappings. The original SVG/PNG drawings were visually reviewed together; the
PNG files are 6,956–11,216 bytes. Production-image inspection confirms all five
are included in the prefetched service-worker application cache. Existing
`main-3OR3FFE7.js` is unchanged; the asset manifest changes for the added icons.

Full [exact-commit CI](https://github.com/Odin2325/PlantCare/actions/runs/39836761174)
passed for candidate `1657c5576c3d464eb5f09c2335c23f4dfd434c59`, including frontend,
backend, production API/worker builds, SQL Server isolation/session security,
encrypted push retry/failure checks, browser journeys, accessibility and PWA
updates. All five action PNGs passed offline-cache coverage.

## Staging acceptance

Candidate `1657c5576c3d464eb5f09c2335c23f4dfd434c59` is live on staging, following
`dd3bc5e5432c8a397a9fdd39cdc597c198926dd2`. Bundle checksums, actual API/worker
image identities, restricted runtime access, readiness and operations passed
during activation. All five public 192px action PNGs match the committed bytes
over trusted HTTPS. All 21 shared species identities, care fields, aliases and
growth-reference metadata remain; all 24 catalog photo hashes still match.

A service-worker-controlled approved test session held across deployment received
the real Update now prompt and reloaded to the new asset version. All five icons
loaded while offline afterward. Authentication, personal plant IDs and completed
calendar IDs were preserved, the catalog remained usable, and logout passed.
The JavaScript bundle remains `main-3OR3FFE7.js`. No additional phone pushes or
emails were sent; the next naturally delivered reminder will use the action icon.
Owner-reported physical acceptance of the new drawings remains pending.

Fresh encrypted off-server VPS backups passed before and after activation. The
post-deployment backup ran after the live session and public asset checks ended.
Both local and VPS backup health checks were healthy with no problem codes at
19:22 UTC on 2026-10-22; final public readiness and operations checks passed.
The backup candidate pointer identifies this deployed release.
