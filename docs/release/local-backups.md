# Encrypted external-drive backups (Windows local preparation)

This implements local OPS-01 preparation with PowerShell 7, Docker Desktop and
[restic](https://restic.readthedocs.io/en/stable/). It does not provision a public
host or claim production disaster-recovery acceptance.

## Setup and operation

```powershell
./scripts/Initialize-LocalBackups.ps1 -Repository 'D:\PlantCareBackups'
./scripts/Invoke-LocalBackup.ps1
./scripts/Test-LocalEncryptedRestore.ps1
./scripts/Test-LocalBackupHealth.ps1
./scripts/Test-LocalBackupFailureChecks.ps1
./scripts/Register-LocalBackupTask.ps1 -At '03:00'
```

Initialization downloads official restic 0.19.1 for Windows x64 and verifies its
archive against a pinned release SHA-256. It refuses an existing configuration
or a nonempty new repository. Review tool/security updates before upgrading the
pin. The executable and private configuration live in `.operations-local/`,
excluded from Git and Docker builds. Directory ACLs admit only the current
Windows user, SYSTEM and Administrators. A random repository password is stored
with Windows DPAPI through SecureString/Export-Clixml, bound to this user/machine.
It reaches restic through the child process environment, never command arguments
or logs. Restic encrypts/authenticates repository contents and metadata.

Each backup:

1. Verifies that the configured repository/key, Docker and source services are
   available and the destination has at least 5 GiB free. There is no fallback
   destination if the external drive is disconnected.
2. Stops API/worker writers, captures all user-table counts/checksums, makes a
   SQL `COPY_ONLY` backup with `CHECKSUM`, and runs `RESTORE VERIFYONLY`.
3. Captures the matching data-protection key archive, `.env.local`, Compose and
   Caddy configuration, SHA-256 manifest, commit and actual SQL/API/worker image
   IDs. These include secrets and remain private.
4. Restarts writers in `finally`, then encrypts the captured files in one restic
   snapshot and runs `check --read-data`. Success is recorded only after checking.
5. Removes only this run's generated SQL backup and private plaintext scratch.
   Cleanup failures report failure. A crash can leave scratch; inspect it safely
   before removing it, and never remove another run's files.

Backup/restore scripts share an exclusive file lock. Other deployment/recovery
scripts do not yet acquire it: do not run acceptance, migration, key rotation,
credential changes or recovery concurrently with scheduled backups.

## Schedule and local failure signals

The Windows task `PlantCare-LocalEncryptedBackup` runs daily at 03:00 in the
computer's configured timezone (Berlin on the tested machine), under the
current user with limited privileges and interactive logon. No Windows password
is stored. Missed runs start when available; overlapping task invocations are
ignored. The task allows battery operation and has a one-hour execution limit.
The user must be logged in, Docker Desktop running and the backup drive attached.
It does not wake a sleeping computer. A forced task termination can interrupt
cleanup/restart; inspect source services after a timed-out run.

```powershell
Get-ScheduledTaskInfo -TaskName PlantCare-LocalEncryptedBackup
Start-ScheduledTask -TaskName PlantCare-LocalEncryptedBackup
Disable-ScheduledTask -TaskName PlantCare-LocalEncryptedBackup
```

`last-attempt.json`, `last-success.json`, `last-restore-test.json` and `health.json`
in the private state directory record non-secret results. Health exits nonzero
for unavailable repositories/status, missing recorded snapshots, backups older
than 30 hours, failed last attempts, runs exceeding 30 minutes, or source/backup
disk space below 5 GiB. The threshold is a starting point; larger databases need
capacity planning and tighter recovery targets. Health probes do not restore or
write application data.

The owner's local Codex heartbeat checks hourly and uses the connected Gmail
account to send a failure email to the separately authorized recipient. It
records the last sent problem set to avoid repeated messages while unchanged,
and sends one recovery email when healthy again. A test email verifies the
connector path. Email includes only problem codes and a short next action, or a
brief recovery confirmation; private operational metadata is excluded. This
depends on Codex, the computer and connector availability;
it cannot report a whole-computer outage while the computer is offline. No SMTP
password, Gmail token, recipient address or automation identity is committed.
Public staging needs an independent uptime/backup alert service.

On 2026-10-13, the first scheduled run completed successfully and its encrypted
snapshot passed the restore checks. The actual source C: disk had about 3.6 GiB
free, below the default warning threshold; a minimal warning/test email was sent.
The owner needs to free space. Failure probes restore their temporary status and
configuration in finally and rerun health with the real default thresholds.

## Recovery key: required owner action

The DPAPI file alone is not a portable recovery key. Losing Windows/user access
can make it unusable. Export the repository password interactively to a private
temporary file on a different device from the backup drive:

```powershell
./scripts/Export-LocalBackupRecoveryKey.ps1 -OutputPath '<separate-private-location>\plantcare-restic-key.txt'
```

Move its contents into a password manager or secured offline recovery kit, then
remove the temporary plaintext file. Never paste it into chat, commit it, email
it or store it beside the repository. The export command refuses an existing
file and a destination on the repository drive. Its parent folder must already
exist and have private permissions. Verify recovery on a separate machine using
this saved password before treating the backup as disaster recovery.

## Restore verification and retention

`Test-LocalEncryptedRestore.ps1` decrypts the recorded successful snapshot into
a fresh private scratch directory, verifies every manifest SHA-256, starts a
disposable SQL container pinned to the recorded image, restores without REPLACE,
runs CHECKDB and compares all table counts/checksums. It removes only that
generated project's volumes and scratch. Source SQL, Windows SQL and source
volumes are never restore targets. A failed cleanup leaves named artifacts for
manual inspection. Required source images must still exist locally.

This validates SQL restoration and the exact matched key/config bytes. The
existing [full recovery drill](recovery.md) separately tests restored cookies,
history and worker behavior. Integration credential recovery remains unverified.

No automated snapshot deletion is enabled. Review a retention policy such as
seven daily, four weekly and six monthly snapshots, and use restic `forget`
`--dry-run` before enabling pruning. Keep pre-upgrade backups. Monitor capacity;
encryption does not prevent deletion by a compromised host with write access.
Disconnect/rotate or replicate the external drive and store a copy off-site.
Retain reviewed application images/source as well as the recorded IDs; a hash
does not preserve an image.

Still open: portable-key recovery, off-site/immutable storage, accepted recovery
point/time targets, independent alerts and public-host deployment/recovery tests.
