# Bounded container logging

Prepared 2026-10-21. Policy: Docker `json-file`, `max-size: "10m"`,
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
- Application results are recorded after maintenance completes.

Official Docker references:
[JSON-file driver](https://docs.docker.com/engine/logging/drivers/json-file/),
[Changing logging options](https://docs.docker.com/engine/logging/configure/).
Existing containers require recreation to adopt these settings. A complete
time-based retention/privacy policy remains separate launch work.
