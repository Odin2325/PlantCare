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

Final CI, deployment and backup results will be recorded after verification.

Local verification passed: production API/worker builds, all 42 selected real SQL
Server push/account-data/erasure checks, and the restricted worker permission
check (including denied catalog writes). The initial full backend run passed
147 tests; the subsequently added common-name fallback also passed on real SQL.
All 22 unchanged catalog image files still match their attribution manifest.
