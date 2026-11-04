# Prepared snapshot-only recovery option

Prepared/tested locally on 2026-11-04. This is an option for the next combined
checkpoint, not a selected or applied live policy. Normal local and staging
databases retain their existing recovery models, schedules and snapshots.

## Recovery choice

The current documented tool restores a complete saved SQL/key/config snapshot.
The owner-dependent daily task does not promise recovery to an exact time between
snapshots, nor guarantee a 24-hour recovery point while the computer/drive/access
is unavailable. Keep that distinction explicit.

For this small beta, SIMPLE recovery is a prepared snapshot-only option: restore
to a complete verified backup, with possible loss of changes made afterward.
It does not provide point-in-time/log-chain recovery. Keeping FULL and supporting
point-in-time recovery instead requires an explicit continuous log-backup,
off-server storage, monitoring and restore-chain design. That alternative is not
implemented by this slice. [Microsoft's recovery-model comparison](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/recovery-models-sql-server?view=sql-server-ver17)
describes the trade-off.

SIMPLE allows log reuse at eligible checkpoints, subject to active transactions
and other blockers; it does not eliminate logging, guarantee a fixed log size,
or shrink physical files. No automatic shrinking, hard file-size cap, backup
expiry, new schedule or wider recovery promise is introduced.

## Guarded preparation script

`deploy/sql/snapshot-only-policy.sql.example` is not used by migrations, app startup
or the activation script. It requires a standalone sysadmin session in PlantCare
and the exact explicit session acknowledgment of snapshot-only/no-point-in-time
recovery. It rejects unavailable/read-only/snapshot/availability-group/mirrored
databases and existing log-backup history requiring separate review.

Before changing recovery, it requires a checksum-protected, undamaged COPY_ONLY
database backup within two hours, with a database GUID matching the current
database. That matches the existing activation freshness window; it changes no
monitoring threshold. SQL history cannot prove off-server encryption, restored
usability, complete backup history, or the absence of a previously pruned log
chain. Operator review and a fresh verified off-server snapshot remain essential.
Do not interpret a missing history row as permission to retire a known log chain.

Only then does the example select SIMPLE and checkpoint, without forcing other
sessions to roll back or shrinking files. Reapplication is idempotent. The live
choice must be reviewed at the larger checkpoint; no approval token was supplied
to any normal environment in this slice. Do not copy the acknowledgment from a
test as a substitute for that decision.

## Local acceptance

The focused existing HTTP tool now supports:

```powershell
python scripts/Test-PhotoHttp.py --capacity --budget --snapshot-policy `
  --api plantcare-api:<accepted-local-api-commit> `
  --bundle .operations-local/photo-processing-bundles/<decoder-source-commit>
```

It always creates its own UUID-named internal Docker project, generated passwords
and a new SQL server/volume. There is no parameter to point this tool at staging,
a saved app connection string, a backup repository or an existing database server.
The ordinary command without --snapshot-policy never applies this policy.

Passing focused evidence:

- Missing acknowledgment and missing fresh backup were refused; the disposable
  initial FULL model remained unchanged. A fresh CHECKSUM/COPY_ONLY backup passed
  VERIFYONLY; the acknowledged change and idempotent reapplication passed.
- The real browser/HTTPS/private decoder/SQL journal lifecycle passed under
  SIMPLE and the proposed local resource budget, including ownership/CSRF,
  normalized JPEGs, editing/export and account media erasure.
- With one real normalized journal photo saved, the browser paused before delete.
  Twelve committed 2-MiB insert/delete cycles on a disposable auxiliary table
  exercised checkpoints; lower logical log usage afterward demonstrated reuse
  for this fixture. This is not a claim of zero log usage or bounded growth under
  long transactions. The auxiliary table was dropped.
- A photo-bearing CHECKSUM/COPY_ONLY backup passed VERIFYONLY and restored to a
  separate empty database in the same disposable SQL server, without REPLACE.
  CHECKDB passed. SIMPLE recovery, journal/media counts, normalized image hash,
  thumbnail hash and edited notes matched. The copied database was dropped before
  the browser continued. No API was ever started against the restored copy.
- After browser deletion/erasure, the drill deliberately established a real FULL
  log chain in that same disposable server. The example refused its retirement
  even with the acknowledgment, and FULL remained unchanged. All temporary
  SQL backups/logs, accounts, volumes and containers were removed on cleanup.

Private results include SQL backup size and local command timings; those are
not encrypted-repository measurements or VPS recovery targets. The report only
marks acceptance after successful cleanup. Generated browser checkpoint metadata
is atomically published and parsed as UUID/hash data, never instructions/SQL.
The final sample's SQL backup was about 31 MiB; backup/VERIFYONLY and
restore/CHECKDB commands took about 0.6 and 1.3 seconds locally. This mixed schema,
churn and image sample is not a per-photo size estimate or an off-server recovery
time promise.

This does not replace the existing fresh-source account-erasure reconciliation,
full application/key/config recovery, replacement-host, interrupted-backup or
physical-device gates. It is not point-in-time recovery and sends no external
mail/push. No remote CI, full application suite or staging deployment ran.

## Combined checkpoint

Before a live choice, review the current recovery requirement and any log chain,
capture/restore fresh off-server evidence, and include recovery-mode metadata in
the reviewed deployment/backup record. Stop writers through the existing guarded
maintenance procedure. A later switch back to FULL alone does not establish
point-in-time recovery: it requires a new appropriate data backup and a verified
log-backup chain. Do not toggle recovery merely to reduce file size.
Keep the public recovery promise limited to what the selected procedure proves.
