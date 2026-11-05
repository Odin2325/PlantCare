# Local mixed photo/reminder/SQL-backup check

Prepared 2026-11-05. This opt-in check uses generated accounts, plants and images
inside the disposable internal Docker project from `Test-PhotoHttp.py`. It reads
no saved app configuration, personal account, external backup repository or VPS.
It uses existing local images and does not build, pull, deploy or enable uploads
in normal environments.

Run it with the accepted API and worker images and a verified local decoder
bundle:

```powershell
python scripts/Test-PhotoHttp.py --capacity --budget --mixed-worker <local-worker-image> --api <local-api-image> --bundle <verified-bundle-directory>
```

The mode requires the preparation-only memory budget and rejects combination
with the separate snapshot-policy rehearsal. Normal focused HTTP checks still
omit the worker. All services use pinned local image identities; only the trusted
test gateway receives the Docker socket. The worker receives its restricted SQL
login and key/heartbeat volumes, with external calendar sync and push disabled.
There is no external mail or push delivery.

## Acceptance gates

After the regular real browser upload/view/edit/export check saves a photo, the
browser pauses at a private generated-data checkpoint. The coordinator seeds 100
due watering schedules for that synthetic owner, starts checksum COPY_ONLY SQL
backups in a background loop, releases the browser and starts one worker with
25-item batches and five-second polling. These settings affect only this project.
Before starting that window, the browser waits for the authentication rate-limit
window used by account setup to expire. Repeated password-verified exports keep
the normal application limits; the test does not relax or bypass them.

The browser performs four additional upload/export/delete cycles, checks private
ZIP responses and waits for all 100 in-app reminders through the authenticated
API. The coordinator checks measured photo request intervals overlap actual SQL
backup command intervals and reminder creation occurs within the browser workload
window. This establishes overlapping operation windows, not an instruction-level
codec/SQL execution trace.

The coordinator checks complete, unique reminders, stable counts after two later
worker polls, a successful worker heartbeat, checksum backup verification and
kernel peak/limit/OOM accounting for API, SQL, gateway, proxy and worker. A stopped,
restarted, OOM-killed or memory-envelope-exhausting service fails acceptance.
The browser then continues normal explicit photo deletion and account erasure.
The original private ZIP hash check and SQL media-erasure check still apply.

The safe report is `.operations-local/photo-mixed-workload.json`; it is accepted
only when the whole HTTP lifecycle and UUID-project cleanup succeed. Failure
reports contain fixed phase labels, never raw browser/SQL output or credentials.
The disposable SQL backup file disappears with its own SQL volume. It is not
placed in, nor removed from, either encrypted backup repository.

## Local acceptance, 2026-11-05

The focused mixed run passed against the API/worker from the accepted mobile
checkpoint and the protocol-1 decoder preparation bundle from commit
`755c477b4f6bf93d100f3610e430c6ba59a79c7f`. Gateway and all other runtime images
were pinned by image ID in the safe local report.

- All eight additional upload/export request intervals overlapped SQL backup
  intervals during a 24.473-second browser workload window.
- The worker generated 100 reminders during that window, without duplicates;
  counts stayed stable after two later polls and its heartbeat was successful.
- Forty-eight checksum COPY_ONLY backups completed into the same disposable
  file; the last backup was 7,360,512 bytes and passed VERIFYONLY.
- The complete browser lifecycle, private ZIP image/hash verification, SQL media
  erasure and UUID project/volume cleanup passed. No decoder job remained.

| Service | Kernel peak MiB | Test memory limit MiB |
| --- | ---: | ---: |
| API | 143.6 | 384 |
| SQL | 681.2 | 2048 |
| Gateway | 33.1 | 128 |
| Proxy | 32.4 | 128 |
| Worker | 75.2 | 192 |

All five services were running with zero restarts and zero kernel OOM/OOM-kill
events. These are individual lifetime peaks, including startup, rather than a
measured simultaneous total. Limits were applied only to the disposable project.

## Limits and remaining work

This is a short local sample with one generated image size and two simultaneous
photo entries. It does not test maximum account/service quotas, largest ZIPs,
sustained multi-user traffic, real push delivery, encrypted capture/retention,
host backup freeze, OOM recovery or VPS/systemd behavior. SQL VERIFYONLY is not a
restore rehearsal; the separate recovery rehearsal remains relevant. The browser
and mail sink are test infrastructure outside the proposed production budget.

The decoder has its existing separate per-job memory/CPU/PID limits. Transient
decoder memory is not included in the long-lived service peak table. Summing
individual service peaks does not establish an observed simultaneous host peak.
Actual host headroom, maximum bodies/exports and encrypted backup/recovery work
remain activation gates. Keep uploads off and batch full CI/deployment as planned.
