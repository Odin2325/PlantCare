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

## Deployment and verification (2026-10-14)

- Active staging runtime is now `d3d81f30cc2a6bbd06d92cb2781d7b5e40a3924d`.
  The first candidate `a6efe29` was rejected by the audit gate and never activated.
  Patched .NET 10.0.13 runtime images and Pkcs 9.0.21 were built on the PC;
  no build ran on the VPS. Existing SQL schema, secrets and source data were kept.
- All 125 backend tests pass (74 API, 50 domain, one application). New tests
  exercise stopped/stale/failed/restarted/corrupt/future heartbeat state, disk
  pressure, generic public response and independent API readiness. Current
  package audit reports no vulnerable Infrastructure packages.
- Exact candidate [CI run 38564281362](https://github.com/Odin2325/PlantCare/actions/runs/38564281362)
  passed Frontend, Backend and Production container gates. The candidate's
  actual exported Compose and image IDs also passed the fully internal encrypted
  snapshot application-recovery/activation-rollback drill before activation.
- `Activate-StagingBundle.sh` validates payload hashes/image IDs, retains the
  previous runtime and reapplies it on activation failure. Both public health
  endpoints and actual restricted API/worker SQL access passed after activation.
  Private backup configuration now references the new release, and a fresh
  encrypted VPS capture/repository data check passed.
- The owner confirmed a second UptimeRobot operations monitor showing UP with
  Down/Up email alerts. The controlled running-worker stall and delivery evidence
  are recorded separately below; account settings were owner-confirmed.

## Controlled running-worker stall

`Test-StagingWorkerFailure.ps1` starts only from both endpoints healthy, verifies
a separate twelve-minute server unpause timer, then freezes the running worker
for a maximum nine-minute test window. It leaves the API, SQL, keys and source
data intact. The script continuously requires API readiness 200, observes stale
operations health 503, unpauses in `finally`, waits for both checks to recover,
and cancels its timer only after recovery. If the PC/session disappears, the
server timer still attempts unpause. Care reminders may be delayed during this
deliberate synthetic-staging test; never run it as an unattended production probe.

On 2026-10-14 the worker was paused at 14:45:35 Berlin. Staleness was detected
at 14:48:38 with operations 503 and readiness 200. The real operations-monitor
DOWN message reached owner Gmail INBOX at 14:50:46. This demonstrates that a
running-but-stalled notification worker can trigger an independent email alert.
Gmail inspection briefly hit a connector rate limit, then succeeded on retry.

Worker processing and both endpoints recovered at 14:54:50 Berlin; the real
operations-monitor UP email reached INBOX at 14:55:50. Both mail bodies identify
the operations URL; the Down message records 503. The timer was cancelled after
recovery. Real Down/Up delivery therefore passed for this controlled stalled
worker incident, independently of the healthy readiness monitor.

The fresh post-activation encrypted snapshot also passed isolated SQL restore,
CHECKDB/all-table comparison and key/config hash verification after this test.
Full replacement-host/provider recovery and independent backup freshness alerts
remain separate release work.

## Bounded read sample results

Two samples each performed 200 trusted-HTTPS anonymous catalog reads at maximum
concurrency five, with a 250 ms pause in each lane and a 90-second overall request
cancellation deadline. Resource commands are read-only and retry one transient
SSH failure; failures never print raw diagnostics.

| Runtime | Errors | P95 response | Available host RAM afterward | Free disk afterward |
| --- | --- | --- | --- | --- |
| Previous `0acdb2d` | 0/200 | 121.3 ms | 2.14 GiB | 31.34 GiB |
| Patched `d3d81f3` | 0/200 | 49.9 ms | 2.20 GiB | 31.27 GiB |

Container restart counts stayed zero and OOM flags stayed false; all four services
remained running. Before/after memory/CPU snapshots are private local evidence,
not peak-resource measurements. The latency difference is not a proven speedup:
warm connections/caches and network variation affect these small samples.

The current catalog contains one synthetic species. These measurements support
only this small read workload; richer catalogs, authenticated care writes,
multiple busy users, push fanout, sustained load and concurrent backup-time
headroom are NOT established. No paid server upgrade is justified by this sample
alone. Reviewed real species content remains a separate release requirement.

## Capacity scope

Use bounded anonymous public catalog reads to measure initial HTTP/SQL headroom,
with a fixed request/concurrency cap and no account/email/push writes. Record
errors, latency, host available memory/free disk, container memory, OOM flags
and restarts before/after. This is a small read-workload sample, not proof of
authenticated scheduling/fanout capacity, sustained production scale or noisy
neighbor performance. Do not generate unbounded traffic or stress until OOM.
