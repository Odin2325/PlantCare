# Bounded container logging

Applied to staging on 2026-10-21 from configuration commit `fcd2927`. The
application remains pinned to `677b91a0083cdfec6e5f2656a82c9594e49d07b0`.
Policy: Docker `json-file`, `max-size: "10m"`,
`max-file: "3"` on database, API, worker, proxy and operations containers.
This bounds each container's stdout/stderr log rotation to approximately 30 MB
plus implementation/record overhead. It is not a fixed maximum retention age,
nor a bound on database internal logs, host journals, backups or copied artifacts.
Low-volume logs can remain older than any proposed day-based retention period.

`compose.yaml` defines the default for future bundles. For the existing pinned
staging bundle, [the override](../../deploy/compose.budget.logging.yaml) preserves
the 2048 MiB SQL setting and adds only logging. It replaces the operational
`compose.budget.yaml`, which is copied into backups and forward to later releases.
The application image candidate remains unchanged.

## Guarded application

[Apply-StagingLogRotation.sh](../../scripts/Apply-StagingLogRotation.sh) is limited
to the existing staging project. It defaults to validation only. It requires the
exact candidate, reviewed previous-override SHA256 and new-override SHA256. It
checks the pinned bundle, current image tags/IDs, health and disk headroom. Resolved
Compose configuration is compared in memory, rejecting any difference beyond the
declared logging policy; it never prints or saves resolved credentials.

Before `apply`, take a fresh verified encrypted VPS backup. Transfer the LF-encoded
script and override to the fixed staging base; check shell syntax and run
validation. The script preserves the prior override and refuses to overwrite an
earlier maintenance record. Explicit application stops writers, recreates the
database, then API/worker and proxy using existing local images with no pull/build.
All named data/key/proxy volumes stay attached. Brief downtime is expected.

Container recreation removes those containers' previous stdout/stderr history;
this is not a selective redaction or general log-erasure procedure. Never truncate
Docker-managed files directly. The failure trap reapplies the old override and
attempts to resume the existing services. Runtime log options, readiness,
operations and restricted DB access are checked after application. No application
schema, email settings, backup schedules or thresholds are changed.

## Evidence

- Initial read-only inspection: all four live containers used `json-file` without
  rotation options. Raw logs and environment contents were not read.
- Shell syntax and real resolved-configuration validation passed: only the shared
  logging extension and service logging differ; current image IDs/health passed.
- Fresh pre-maintenance encrypted VPS SQL/key/config backup and repository check
  passed. Guarded application succeeded; all four live containers report exactly
  the intended driver and 10m/3 options. Readiness, operations and restricted API/
  worker database-access checks passed. Public DNS, HTTP-to-HTTPS, trusted TLS,
  readiness/liveness, SPA deep link and API 404 checks passed afterward.
- The existing operational override is included by the normal VPS capture path
  and copied forward by activation. No new application bundle or schema migration
  was deployed. No time-based retention guarantee is claimed.
- A fresh post-maintenance encrypted VPS capture and full repository check passed;
  default VPS backup health reported healthy with no problems. The committed main
  Compose source also passed real Docker configuration parsing for all six
  services, including operations profiles.
- Exact-configuration commit `fcd2927f937a6e55c9c3fe61e85f8753a5992d57` passed
  [CI run 39602840172](https://github.com/Odin2325/PlantCare/actions/runs/39602840172):
  backend/frontend, production images, SQL isolation/authentication/push checks
  and browser/accessibility/PWA journeys all succeeded.

Official Docker references:
[JSON-file driver](https://docs.docker.com/engine/logging/drivers/json-file/),
[Changing logging options](https://docs.docker.com/engine/logging/configure/).
Existing containers require recreation to adopt these settings. A complete
time-based retention/privacy policy remains separate launch work.
