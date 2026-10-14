# VPS backups and monitoring

Verified 2026-10-14 against the public staging runtime candidate
`0acdb2db70a343d1aae977bf4bb19150deaaaf7a`. This is learning-project staging
preparation; production recovery acceptance remains open.

## Capture and storage

`Invoke-VpsBackup.ps1` uses the existing encrypted restic repository on the
owner's external `D:\PlantCareBackups` drive. VPS snapshots have the distinct
host/tag `plantcare-vps-staging`. Local and VPS operations share an exclusive
lock. No retention deletion or generic Docker pruning is performed.

The fixed-target SSH helper briefly stops API/worker writers, captures SQL with
COPY_ONLY/CHECKSUM and VERIFYONLY, all-table counts/checksums, data-protection
keys, private deployment configuration and image identities. Its EXIT trap
resumes API/worker before transfer and encryption. Expect a brief staging outage
during capture. The manifest records SHA-256 for captured files; the exact SQL
image digest is used for verification. No build runs on the VPS.

Captured secrets stay in owner-only scratch, are transferred using the trusted
SSH host key, then encrypted locally. The restic password never goes to the VPS.
Successful encryption and repository data checking precede scratch cleanup.
If encryption fails, the remote capture is preserved for diagnosis/retry; inspect
private state without publishing contents. Failed cleanup is reported as failure.
Failed attempts preserve existing encrypted snapshots.

Private settings are in ignored `.operations-local/vps-backup-config.json`:
fixed host/user, the current runtime candidate, and an existing SSH identity path.
Update the candidate deliberately when deploying a new bundle. The existing
local backup configuration and DPAPI-protected password remain in use. Never
commit configuration, credentials, keys or restored payloads.

## Schedule and operator checks

Windows task `PlantCare-VpsEncryptedBackup` runs daily at **04:00 Berlin time**;
the existing local task remains at 03:00. The VPS task was invoked through Task
Scheduler and completed with result 0. It retries twice at 15-minute intervals,
starts missed work when available, and refuses overlapping task instances.

The PC must be on with the owner logged in, the external drive attached, the SSH
agent unlocked and network access available. The current administrator source IP
must be allowed by the VPS firewall. Docker Desktop is required for the local
backup and restore drill, but not for VPS capture. This setup does not provide
unattended off-server backups while the owner's PC is unavailable.

```powershell
pwsh -NoProfile -File scripts/Invoke-VpsBackup.ps1
pwsh -NoProfile -File scripts/Test-VpsBackupHealth.ps1
pwsh -NoProfile -File scripts/Test-VpsEncryptedRestore.ps1
pwsh -NoProfile -File scripts/Test-VpsBackupFailureChecks.ps1
```

Health checks use a 30-hour freshness threshold, verify the actual encrypted
snapshot exists, check disk space and inspect task state/result. A running task
with result 267009 is not a failure. Private status files are `vps-last-success`,
`vps-last-attempt`, `vps-last-restore-test` and `vps-health` JSON under
`.operations-local`. Do not publish their operational metadata or raw logs.

The existing Codex email monitor now checks both backup types on the owner's
chosen **weekly Sunday 09:00** schedule. It preserves the existing deduplication
baseline and sends only changed problem codes/short actions or one recovery
confirmation to the authorized owner address. Simulated probes send no email.
It defers VPS checks during a recent failure-probe marker and flags a marker
older than 15 minutes. Weekly checks can leave failures unnoticed for days;
Codex/the PC/Gmail connector must be available.

## Evidence and limits

- A manual encrypted VPS backup and a scheduled-task backup passed; repository
  data checking succeeded and staging readiness recovered after capture.
- An encrypted snapshot was decrypted and restored into a fresh, unpublished
  local Docker SQL instance using the captured official image digest. RESTIC
  verification, all-file SHA-256, SQL restore, CHECKDB and all-table
  count/checksum comparisons passed. Key/config file hashes matched.
- Failure probes detected stale/missing snapshots, failed/stuck attempts and
  rejected a missing SSH identity or unexpected target before remote capture.
  Recent active backups and a running task result 267009 remained healthy.
  Original private state was restored and actual health passed afterward.
- The restore drill starts only SQL, so it sends no email/push and does not
  contact integrations. Restored app login/cookies, SQL login remapping and
  integration behavior on a replacement VPS still need a full recovery drill.
- The external drive is separate from the VPS but not an immutable backup.
  Separate-machine recovery, a portable recovery key held outside this PC,
  retention/capacity planning and independent backup monitoring remain open.

## Next: independent public uptime monitoring

The owner created a free UptimeRobot account and confirmed a monitor for
`https://staging.plantacare.org/health/ready` with owner email alerts on
2026-10-14. See [uptime-monitoring.md](uptime-monitoring.md) for the controlled
outage/recovery drill and delivery evidence. It operates outside the PC/VPS;
it does not prove worker processing or backup freshness. No paid add-on is needed.
