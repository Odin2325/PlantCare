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

Deployment and verification results will be recorded after completion.
