# Worker progress monitoring and capacity

CI caught an existing WebPush transitive dependency, Pkcs 9.0.4, affected by
[CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx) and
[CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).
Infrastructure now explicitly selects the patched same-major Pkcs 9.0.21;
the high/critical audit gate remains enabled. Rebuild/redeploy images to apply
the package fix; changing source alone does not update a running container.

`/health/ready` checks API/database availability. The separate public
`/health/operations` endpoint returns only Healthy (200) or Unhealthy (503),
without exposing paths, credentials, timestamps or workload counts.

The notification worker writes a small atomic heartbeat after **both** reminder
generation and pending push processing complete. Startup invalidates an older
run's heartbeat; an exception marks the pass failed. Missing/corrupt/future/stale
state is unhealthy. A running but stalled container therefore expires its health
signal. Heartbeat write failure is logged without stopping reminder processing;
the missing/stale signal remains unhealthy.

Compose mounts a dedicated `worker-health` volume writable by the non-root
worker and read-only by the API. The volume contains no account data or secrets
and is ephemeral operational state, not a recovery backup requirement. API
readiness remains independent, so worker startup/recovery has no circular
Compose health dependency.

Defaults are a maximum 180 seconds since a successful notification pass and
5 GiB free on the API filesystem. The current VPS stores Docker/SQL on the same
host disk; this check does not establish free space on arbitrary separate mounts.
Configure `WORKER_HEALTH_MAX_AGE_SECONDS` and `OPERATIONS_MIN_FREE_BYTES`
deliberately for a different polling/workload/storage setup. Slow push batches
can legitimately exceed the time threshold; measure before tuning it.

Add a separate free UptimeRobot HTTP(S) monitor for
`https://staging.plantacare.org/health/operations` with expected 200 and both
Down/Up owner email alerts. Keep the existing readiness monitor. At a five-minute
monitor interval, stale-worker detection plus sampling/retries can take roughly
eight minutes or longer; this is not instant paging. Monitor setup and actual
delivery must be verified separately from endpoint behavior.

This signal covers notification processing, not the external-calendar loop or
per-device push success. Optional calendar providers remain disabled on staging.
It also does not monitor backup freshness or detect a complete host failure by
itself; the independent public monitors and backup checks cover separate cases.

## Capacity scope

Use bounded anonymous public catalog reads to measure initial HTTP/SQL headroom,
with a fixed request/concurrency cap and no account/email/push writes. Record
errors, latency, host available memory/free disk, container memory, OOM flags
and restarts before/after. This is a small read-workload sample, not proof of
authenticated scheduling/fanout capacity, sustained production scale or noisy
neighbor performance. Do not generate unbounded traffic or stress until OOM.
