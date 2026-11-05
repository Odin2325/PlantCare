# Encrypted photo capture and SQL recovery sample

Local preparation, 2026-11-05. No normal backup repository or staging service is
accessed. Uploads stay disabled outside the disposable fixture.

```powershell
python scripts/Test-PhotoHttp.py --capacity --budget --large-account --encrypted-recovery --api <local-api-image> --bundle <verified-bundle-directory>
```

This opt-in mode extends the [near-quota account check](photo-large-account.md).
It requires Windows and the already-installed local restic tool. It creates one
new repository beneath the UUID-owned private test directory with a random
test-only password passed only in the child environment. It removes inherited
RESTIC settings and uses an explicit local repository/cache. It does not read
saved backup configuration, existing repository keys, DPAPI state or normal
application environment files. No expiry, prune, schedule or retention operation
runs.

## Capture and recovery gates

The browser pauses while its generated account contains the near-byte-quota
dataset. The coordinator captures a checksum COPY_ONLY database backup, verifies
it, copies the generated app's key directory to a private tar archive and writes
a file-hash manifest with pinned runtime image identities. The source is paused
by the test protocol, not by stopping a production application. The minimal
manifest does not replace a complete deployment/configuration recovery artifact.

The coordinator encrypts these files into the new repository, runs a full data
check, confirms a wrong password cannot list snapshots, and decrypts the snapshot
into a separate target. It independently checks that all payload hashes match and
no unexpected files or links were restored. It copies the recovered SQL backup
into the same disposable SQL server and restores a separate named database to
new files. CHECKDB, photo-entry/media totals, byte totals, notes and every image
and thumbnail SHA256 must match. The recovered database is then dropped without
starting an app or worker against it.

The original fixture continues its normal quota-release, upload reuse, explicit
deletion and account-erasure checks. The new repository, cache, plaintext capture,
decrypted target and generated SQL volumes are removed with the UUID project.
Only a safe size/time/acceptance report remains in
`.operations-local/photo-encrypted-recovery.json`; acceptance also requires the
complete browser lifecycle and project cleanup to pass. Snapshot IDs, passwords,
key contents, cookies and raw tool output are not reported.

## Local acceptance, 2026-11-05

The focused run passed with the existing restic 0.19.1 Windows/amd64 tool,
accepted mobile-checkpoint API and protocol-1 photo bundle. The large-account
browser lifecycle, subsequent quota reuse/account erasure, SQL media cleanup and
project teardown also passed. No decoder job or generated scratch directory
remained. No push, remote CI or deployment was run.

| Measurement | Local result |
| --- | ---: |
| Restored photos | 38 |
| Source normalized image + thumbnail bytes | 20,770,078 |
| SQL backup bytes | 27,582,464 |
| Key tar bytes | 10,240 |
| Total capture bytes including manifest | 27,593,506 |
| New repository initial bytes | 628 |
| Repository bytes after backup | 20,915,090 |
| Repository growth bytes | 20,914,462 |
| SQL backup + VERIFYONLY command time | 0.681 s |
| SQL/key/manifest copy time | 0.839 s |
| Repository initialization time | 2.823 s |
| Encrypted backup time | 1.136 s |
| Full repository data-check time | 0.828 s |
| Verified decryption time | 0.933 s |
| SQL copy/restore/CHECKDB/drop time | 4.102 s |

Every payload file matched its source hash; every restored JPEG and thumbnail
matched its stored SHA256, and photo counts/bytes/notes matched. The separate
restored database was dropped. Kernel lifetime peaks were API 218.4 MiB, SQL
731.8 MiB, gateway 44.7 MiB and proxy 35.1 MiB, under their preparation-only caps,
with zero limit/OOM events or restarts. These are separate peaks, not a measured
simultaneous host total, and exclude the Windows encryption process.

## Measurement scope

The report separates SQL backup/verify, capture copy, repository initialization,
encrypted backup, full repository data check, verified decryption and SQL
restore/CHECKDB/drop command times. It records plaintext capture sizes, repository
initial size and actual repository growth. These are local wall times, not a VPS
restore-time or downtime guarantee.

The dataset repeats one real normalized image across many entries. Both SQL
backup layout and restic deduplication/compression can reduce repository growth
substantially. Do not extrapolate its growth to diverse camera images, 512 MiB of
service media or thirty retained versions. No retention-sizing claim follows.
Host cache and source/target disk caches also affect timings. The Windows restic
process sets `GOMAXPROCS=2` and a 256-MiB Go runtime memory target; that target is
not an OS limit and its peak working set is not measured here. Application/SQL
cgroup peaks are measured by the surrounding large-account check.

External-drive/network transfer, genuinely independent/replacement-host recovery,
runtime-login remapping, restored-app/key use, restored-account erasure, full
configuration reprovisioning and actual VPS/systemd/resource-limit acceptance
remain checkpoint gates. This does not install a gateway, change live recovery
models or alter the user's external repositories.
