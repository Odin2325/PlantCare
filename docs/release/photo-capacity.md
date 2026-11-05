# Local photo capacity measurements and proposed budget

Measured 2026-11-04. This is local preparation, not photo activation or VPS load
certification. Staging uploads, live container settings, backup schedules and
database recovery settings were not changed. No remote CI or deployment ran.

## Decoder samples

`scripts/photo_capacity.py --bundle <accepted-bundle-directory>` uses generated
PNG images and the exact bundle image ID. Each job is a fresh container with no
network, read-only root, no capabilities/new privileges, 64 PIDs, one CPU and
384 MiB memory with no extra swap. A small in-cgroup shell reads memory.peak,
cpu.stat and memory.events after the real supervisor/native child exit, avoiding
the undercount from polling short-lived Docker stats. Kernel accounting must be
available; missing peaks, OOM kills and unknown errors fail the measurement.
The wrapper's overhead is included. Docker wall time includes container startup.
Only the UUID-named owned job is removed; uncertain cleanup is an error.

| Generated input | Input bytes | Peak MiB | CPU seconds | Docker wall seconds | Result |
| --- | ---: | ---: | ---: | ---: | --- |
| Detailed 1024x768 | 2,360,852 | 57.1 | 0.433 | 0.868 | Accepted |
| Pattern 4000x3000 (12MP) | 2,511,871 | 87.6 | 0.945 | 1.368 | Accepted |
| Pattern 5000x4000 (20MP) | 3,426,320 | 120.4 | 1.119 | 1.520 | Accepted |
| Pattern 5000x4200 (21MP) | 3,597,672 | 39.4 | 0.325 | 0.692 | photo-dimensions-exceeded |

These are single sequential samples, not averages, percentiles, real-camera
coverage or adversarial worst-case guarantees. The hard 384 MiB cap remains;
the 120 MiB observation is not a proposed lower limit. JPEG/static WebP and other
validation remain covered by existing functional tests, not this PNG measurement.

Six focused accounting cases also pass: missing/over-limit peak, OOM events,
unknown native errors, expected dimension rejection and uncertain cleanup. They
join the existing Linux gateway/bundle target; all 25 focused cases passed.
The test selector and full-checkpoint policy were not relaxed.

## Real HTTP budget rehearsal

The existing isolated [HTTP journal acceptance](photo-http-acceptance.md) adds:

```powershell
python scripts/Test-PhotoHttp.py --capacity --budget `
  --api plantcare-api:<accepted-local-api-commit> `
  --bundle .operations-local/photo-processing-bundles/<decoder-source-commit>
```

--capacity uses the detailed 1024x768 input, records actual input/normalized/export
sizes and local API/SQL/gateway kernel accounting, and performs the real journal
lifecycle. --budget applies the public preparation example only to disposable
containers; it requires --capacity. All confirmation mail remains in internal
Mailpit. The normal local app/VPS are untouched. No full suite or rebuild is needed
when the selected runtime/decoder source has not changed.

`deploy/compose.photo-budget.json.example` proposes API 384 MiB, SQL 2 GiB, worker
192 MiB and proxy 128 MiB, without extra swap. SQL's internal target is 1024 MiB,
separate from its 2-GiB container ceiling. The local trusted gateway is also capped
at 128 MiB for the rehearsal; the isolated decoder retains its 384-MiB bound.
The distinction between SQL's internal setting and the container ceiling follows
[Microsoft's memory guidance](https://learn.microsoft.com/en-us/sql/linux/configure/performance-best-practices-sql-server-memory?view=sql-server-ver17).
SQL's [minimum startup memory](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-setup?view=sql-server-ver17)
is not a claim that a busy workload will fit this proposed configuration.

The real upload/view/edit/reload/export/owner-isolation/CSRF/erasure lifecycle
passed with those local limits. Measured cgroup peaks were API 133.7 MiB, SQL
669.4 MiB and gateway 31.8 MiB. These include startup and the full sequential
account/export/erasure work, not just one allocation. They exclude the browser
and are not sustained-load or maximum-request measurements. The first unbudgeted
SQL sample approached its 2-GiB ceiling; setting an explicit internal SQL target
reduced this sample's observed memory. Do not extrapolate that to all workloads.

The input became a 521,520-byte JPEG plus 25,061-byte thumbnail. ZIP export was
522,319 bytes and its image hash/contents passed independent verification. The
single browser upload took 1.092 seconds including browser action, HTTPS, decode
and SQL. This is not a latency promise. All generated data/containers were cleaned.

## Read-only deployment baseline

The VPS had about 3815 MiB RAM, 2105 MiB available and 18.5 GiB free disk at the
sample time. Current API/SQL/worker/proxy cgroups report unbounded memory.max.
No limit was applied. The proposed worker limit has not received real reminder
load acceptance in this slice. Neither the Compose example nor the local gateway
cap is an approved live host configuration.

The four proposed application ceilings total 2752 MiB. Adding a 384-MiB decoder
and a separate 128-MiB gateway envelope gives 3264 MiB, leaving about 551 MiB on
this host for the kernel, Docker daemon and other host services. This is a tight
budget that needs mixed worker/export/backup and failure tests before activation.
It is a planning bound, not a reservation or proof that every component stays
within it. Gateway termination/orphan cleanup and actual systemd/user-namespace
mapping must be accepted too. No paid upgrade is selected from these samples.

## Storage and backup growth

Current media limits remain 512 KiB full image + 64 KiB thumbnail, 20 entries per
plant, 50 per account, 20 MiB per account and 512 MiB across the service. At the
maximum pair size, the byte limits admit only 35 pairs per account and 910 across
the service, even though smaller images can reach the count quota. These are raw
media bounds; metadata, indexes, transaction logs and database allocation add more.

Thirty fully changed sets of the service's 512-MiB media quota represent 15 GiB
of logical image versions before SQL/backup overhead. This is a scenario, not a
prediction or upper bound on encrypted repository size. JPEG compression and
restic deduplication must not be assumed to eliminate that growth. The attached
backup drive currently had about 710.9 GiB available; it still depends on the
owner's computer/drive/access. No backup, retention deletion or restore was run.

Read-only SQL inspection found FULL recovery, 72 MiB allocated data (about
9.4 MiB used), and a 72-MiB log (about 4.0 MiB used). The existing capture uses
COPY_ONLY database backups. This slice has not established an ongoing log-backup
chain or tested log reuse under photo churn. Decide and test the intended recovery
and log-growth policy before enabling writes; full database captures are not a
substitute for log backups when a FULL recovery log chain is active.
[Microsoft's log-management guidance](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-log-architecture-and-management-guide?view=sql-server-ver16)
describes the relevant truncation conditions. Do not shrink files or change the
live recovery model as a capacity shortcut.

## Next batch gates

The [2026-11-05 mixed-workload sample](photo-mixed-workload.md) now passes with
100 unique reminders, overlapping photo uploads/exports and SQL backups, including
the proposed 192-MiB worker cap. That sample does not certify largest exports,
encrypted repository overhead or actual VPS headroom. Its per-service memory
measurements and remaining limits are recorded separately.

Keep both photo flags off. Prepare/review the SQL log-growth/recovery policy and
host gateway installation, then accept mixed workload/resource limits, actual
image backup/recovery size/time and interrupted-processing cleanup. Include the
worker and largest permitted bodies/exports. Finish native/container notices.
Group this work with the local Apple refinements and further feature work for
one exact-candidate CI/recovery/staging checkpoint. Real installed Android photo
acceptance remains required; Apple hardware acceptance waits for a device.
