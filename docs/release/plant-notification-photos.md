# Familiar plant names and pictured reminders

Owner follow-up implemented 2026-10-22; deployment/physical acceptance is tracked
below. Catalog cards/details already lead with English common names, with
scientific names secondary and both searchable. Adding a plant prefills its name
with the common name. Personal plant cards, details, care history, Dashboard,
calendar and reminders lead with the owner's nickname. The domain requires a
nonblank nickname on normal create/update operations. The push path additionally
falls back to the common name for legacy blank nicknames. Scientific-name import
matching and all species/plant identities remain unchanged.

## Notification images

Superseded by the owner's 2026-10-22 revision to
[care action icons](care-notification-icons.md). The following describes the
earlier photo implementation and its historical acceptance.

The push delivery query loads each reminder's own species. A validated local
catalog cover photo becomes its notification `icon`; missing or invalid metadata
uses the packaged `/icons/plantcare-192.png` app icon. Validation rejects external
image URLs, traversal paths and unsupported/mismatched licensing without blocking
the care reminder. Photo reminders include the photographer and license in their
body, with original title, photographer, source and license links in payload
metadata. Complete visible credits remain on the species/personal plant pages.
No new image assets, personal photo uploads or external image hosts are introduced.

Angular receives its existing flat notification options and unchanged default
`openWindow` action to `/dashboard`. Title, per-reminder tag, preference checks,
queue durability, device expiry and bounded retries remain. The worker gains only
SELECT on the public `PlantSpecies` table, with catalog writes still denied.
Apply the current controlled runtime provisioner before starting this worker.
Older provisioners may reject this additional read grant as unmanaged; use the
current reviewed provisioner for forward recovery rather than bypassing its checks.
Rolling back the application runtime does not require reverting the read grant.

Browser/OS support controls photo appearance. `icon` is distinct from the small,
often monochrome status-bar badge; no replacement of that system badge is promised.
If an image cannot load or the platform does not render it, the textual care
reminder and tap destination remain. Automatic replacement of a failed network
image with the app picture is not implemented; the app-icon fallback covers absent
or invalid catalog metadata. Actual closed-app rendering and photo-load failure
behavior require a physical Android/Chrome check.

References: [Angular push payload/click handling](https://angular.dev/ecosystem/service-workers/push-notifications)
and [notification icon/badge options](https://developer.mozilla.org/en-US/docs/Web/API/ServiceWorkerRegistration/showNotification).

## Verification

Integration checks cover separate plants/photos across fresh worker scopes,
nickname/common-name display, author/license attribution, Dashboard tap metadata,
legacy/no-photo fallback, unsafe persisted metadata and bounded payload size.
Existing real encrypted transport and retry tests remain enabled. The runtime
permission check verifies the new read grant while actually attempting and
rejecting catalog writes. A local headless Chromium probe could not establish
notification display permission for the worker; it is not counted as a pass or
as physical-device acceptance.

Final CI, deployment verification and owner-reported Android photo/tap acceptance
are recorded below.

Local verification passed: production API/worker builds, all 42 selected real SQL
Server push/account-data/erasure checks, and the restricted worker permission
check (including denied catalog writes). The initial full backend run passed
147 tests; the subsequently added common-name fallback also passed on real SQL.
All 22 unchanged catalog image files still match their attribution manifest.

## Staging acceptance

Candidate `22539196d11db01e5769f36db292e59e11f94813` is live on staging, following
`c23787c071074105a637e5f20dee06a0ea6f4cf9`.
[Full CI](https://github.com/Odin2325/PlantCare/actions/runs/39779446480) passed
for this exact commit, including backend/frontend, real SQL Server isolation,
password-reset/session security, encrypted push retry/failure checks, browser
journeys, accessibility and PWA behavior. The deployment bundle hashes, actual
API/worker image IDs, worker SELECT/catalog-write-denial checks, readiness and
operations health passed during activation.

All 12 catalog browser checks passed against public trusted HTTPS in desktop
Chromium, mobile Chromium and Firefox. All 21 shared catalog IDs/names, 20 reviewed
profiles and 22 served image hashes are unchanged. The fallback app PNG is also
available over trusted HTTPS. A conservative payload check of all 20 real photos,
using an intentionally long non-ASCII plant name, remained below 2,500 bytes per
message (largest 1,775); this is a check of the current dataset, not a guarantee
for arbitrary future metadata.

This server/worker release keeps `main-AQVOAFGY.js` unchanged. A session controlled
by the previous frontend's service worker stayed authenticated across activation,
retained its approved test account's plant IDs and completed calendar IDs, loaded
the catalog/photos and logged out successfully. No frontend update prompt,
reinstallation or renewed push permission is required for this payload change.
No extra phone pushes or emails were sent for acceptance; existing normal
reminders will use the new payload when next delivered. Previously delivered
notifications retain their original appearance.

The owner confirmed that a newly received real-catalog reminder in the installed
Android app showed the correct plant photo and tapping opened Dashboard. This is
owner-reported physical acceptance for photo selection/rendering and the tap
destination, separate from automated payload/transport checks. The report did not
separately recheck closed-app state, multiple species, failed image loading or
current Android/Chrome version numbers; those are not inferred from this result.

Fresh encrypted off-server VPS backups passed before and after activation. The
post-deployment capture ran after live browser/session checks completed. Local
and VPS backup health both reported healthy with no problems, and final public
readiness and operations checks passed. The private backup candidate pointer now
identifies this deployed release.
