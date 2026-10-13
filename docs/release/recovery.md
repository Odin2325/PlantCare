# Database and key-ring recovery

## Run the local drill

With Docker Desktop running Linux containers, use PowerShell 7:

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalDeployment.ps1 -IncludeCareJourney -IncludeRecovery
```

The drill uses only the fixed `plantcare-local` Docker project and a fresh
`plantcare-recovery-<random-id>` project. It does not connect to Windows SQL
Server at `localhost`. It briefly stops the source API and worker; do not run it
alongside another acceptance run or operator command. Port 9443 must be free.
The drill pins the API, worker, and SQL images currently used by the source.
Those images must still exist locally; if Docker has removed an old worker image,
recreate that worker before retrying:

```powershell
docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local up -d --no-build --force-recreate --wait worker
```

The drill:

1. Stops API/worker writers and captures all user-table row counts/checksums.
2. Takes a SQL Server `COPY_ONLY` full backup with `CHECKSUM`, runs
   `RESTORE VERIFYONLY`, and copies the backup outside the container.
3. Archives the matching data-protection key ring while writers remain stopped.
4. Restarts source SQL/API/worker and checks the existing authentication cookie,
   care history, new worker processing, and reminder deduplication across a
   further worker restart.
5. Starts a fresh isolated SQL instance, refuses an existing PlantCare database,
   restores without `REPLACE`, and runs `DBCC CHECKDB`.
6. Compares all table counts/checksums before starting target writers, restores
   the key ring with `app` ownership, and compares individual key SHA-256 hashes.
   Recreates SQL logins and remaps restored runtime users with the private matched
   configuration; application backups do not include logins in `master`.
7. Starts the restored API/worker/proxy on `https://localhost:9443` and repeats
   cookie, history, reminder, and worker-restart checks. The existing cookie must
   work without logging in again, exercising the restored keys and Identity data.

SQL backup/restore follows the [Microsoft container restore guide](https://learn.microsoft.com/en-us/sql/linux/tutorial-restore-backup-in-sql-server-container?view=sql-server-ver17).
Aggregate SQL checksums are diagnostic, not cryptographic identity proofs;
file SHA-256 hashes are also recorded. The drill performs an actual restore
and application checks in addition to backup verification.

## Evidence and cleanup

Each run retains `recovery-artifacts/<run-id>/` with `database.bak`, `keys.tar`,
table/key comparisons, a manifest containing the source commit and image IDs,
and `result.txt` on success. The SQL backup also remains inside the source SQL
container as `/var/opt/mssql/plantcare-<run-id>.bak`. Artifacts are excluded from
Git and Docker builds. They contain account data and unencrypted private keys:
keep them private, restrict access, and delete them when no longer needed.
Git ignore is not access control or backup encryption.

Successful runs remove only the temporary restore project and its volumes.
Failed runs retain that project and evidence for diagnosis; the source stack is
restarted in `finally`. After inspection, clean up the exact failed project using
its run-specific override (replace both placeholders with the printed run ID):

```powershell
docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml -f recovery-artifacts/<run-id>/compose.recovery.yaml --project-name plantcare-recovery-<run-id> down --volumes
```

Do not run `down --volumes` against the source project. Synthetic accounts and
archived worker probes remain in its database. A failed drill may leave an active
probe there; inspect it before retrying. Local certificate checks are skipped
only for the fixed localhost test origins.

## Before public staging or production

For the Windows development stack, [local-backups.md](local-backups.md) adds
encrypted external-drive snapshots, a daily task, health checks and encrypted
SQL restore verification. It requires a separately preserved recovery key and
does not replace independent production monitoring/off-site recovery.

This is a local full-backup recovery drill, not a scheduled production backup
system. Recovery of real Google/Microsoft OAuth credentials is not validated:
providers remain disabled until their dedicated checks pass.

For a real deployment, the operator must establish:

- An accepted recovery target and retention policy. A proposed starting point
  is a daily full SQL backup, at least seven daily copies, and a pre-upgrade copy.
  This allows up to 24 hours of data loss; the owner must accept or tighten it.
- An encrypted, access-restricted off-host destination for database and matching
  keys, plus a separate protected configuration/secret-store backup. The local
  `.env.local` is deliberately not included in the drill bundle.
- Scheduled jobs with failure alerts and disk-capacity monitoring. SQL Express
  requires an external scheduler; no production job is provisioned here.
- Regular isolated restores with measured recovery time, account access, core
  care, and enabled integrations. Never copy live MDF/LDF files as a backup.
- Retained source/image versions and compatible deployment configuration.
  Roll back code only when the schema is compatible; otherwise stop writers
  and restore a matched database/key/configuration bundle into an isolated target,
  validate it, then switch traffic. Changes after the backup will be lost.

Keep the old target and backup until recovery is accepted. Never automatically
overwrite a running production database or delete its volumes during recovery.
