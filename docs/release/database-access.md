# Runtime database access and controlled migrations

Docker deployments use three SQL identities:

| Identity | Used by | Scope |
| --- | --- | --- |
| `sa` | SQL bootstrap/health and one-shot `migrate` service | Operator-only schema/account administration |
| `plantcare_api` | HTTP API | App/Identity data needed by routes; Roles/UserRoles/RoleClaims read-only; Notifications read/update; migration history read-only |
| `plantcare_worker` | Single worker | Care/plant/preferences reads; notification/delivery insert/update; expired subscription deletion; external-calendar synchronization tables |

Runtime users have no server roles, database roles, schema ownership or DDL
grants. The worker cannot read Identity tables or modify the catalog. Neither
runtime principal can assign Identity roles or alter migration history. The API
still needs broad access to account and collection data to implement its routes;
SQL permissions do not replace the existing per-user authorization checks.
Table grants are explicit: new schema/features must review this allowlist.

This follows Microsoft's guidance to use [a separate deployment identity](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
and [object-level grants](https://learn.microsoft.com/en-us/sql/t-sql/statements/grant-object-permissions-transact-sql?view=sql-server-ver17).

## Local setup and checks

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalDatabaseAccess.ps1
./scripts/Test-LocalDeployment.ps1 -IncludeCareJourney
```

The start script preserves existing `.env.local` values/volumes, appends missing
random API/worker passwords, builds before stopping writers, starts SQL/mail,
stops API/worker, then runs migration and account provisioning as one-shot jobs.
It starts runtime containers only when both operator steps succeed. A failed
operator step leaves writers stopped for investigation; it does not fall back
to `sa` or silently resume an older image. `-SkipBuild` requires current images.

API startup migrations are disabled in Compose. The `migrate` profile service
uses the API image and exits without serving HTTP. It is not started by normal
`compose up`, has no restart policy, and its admin connection is not injected
into API/worker containers. Docker/host administrators and the private environment
file still control these credentials; environment variables are not a secret vault.

Runtime passwords must be distinct from each other/operator, contain 32–128
characters, and use letters/digits or `!@#%^*_+=.-` to remain safe in Compose
connection strings. The generated local values satisfy SQL password policy.
Keep private configuration access restricted; do not run `compose config`
without `--quiet` in shared logs. Never commit resolved environment/config output.

Provisioning is repeatable and remaps database users after restore. It refuses
unexpected server/database role membership, schema/database ownership and
unmanaged explicit permissions rather than silently keeping an elevated user.
Review such conflicts manually. Public-role defaults of a non-dedicated SQL
instance also require operator review; the tested target is the dedicated SQL
Server container. Provisioning is operator-only, and password changes can cause
a maintenance interruption while runtime containers are stopped.

`Test-LocalDatabaseAccess.ps1` rejects invalid API/worker password inputs, checks
the actual connection principal and required table permissions, and attempts
protected SQL operations. DDL probes roll back if unexpectedly allowed; other
write probes affect zero rows or fail required constraints. Any unexpected
permission is a test failure. A temporary role-name read grant also verifies
rejection of unmanaged permissions; a finally block removes only that probe
grant. CI runs these checks before account isolation.

## Staging deployment sequence

After reviewing a candidate and taking matched SQL/key/config backups:

```bash
docker compose --project-name plantcare-staging config --quiet
docker compose --project-name plantcare-staging build api worker
docker compose --project-name plantcare-staging up -d --wait database
docker compose --project-name plantcare-staging stop api worker
docker compose --project-name plantcare-staging run --rm --no-deps migrate --migrate-database
docker compose --project-name plantcare-staging run --rm --no-deps migrate --provision-runtime-users
docker compose --project-name plantcare-staging up --no-build -d --wait --wait-timeout 300
docker compose --project-name plantcare-staging exec -T api dotnet PlantCare.Api.dll --check-runtime-access api
docker compose --project-name plantcare-staging run --rm --no-deps worker-db-check
```

Supply the three distinct SQL passwords in the private `.env` first. Stop on any
failure; do not proceed to subsequent commands. The commands use the candidate
API image (default local tag `plantcare-local-api`); optional `PLANTCARE_API_IMAGE`
and `PLANTCARE_WORKER_IMAGE` can specify reviewed image references.
Only one operator/deployment should run at a time. EF migration locking does
not serialize credential rotation or unrelated operator work.

Bootstrap Admin and reviewed-catalog import use `migrate` as documented in
[bootstrap.md](bootstrap.md). Ordinary API startup only checks the existing
Admin role; migration creates it first. No runtime account is granted role-write
permissions to make bootstrap succeed.

## Recovery and remaining operations work

The SQL application backup contains database users/grants but not server logins
from `master`. After restoring the database on an isolated SQL instance, run
`migrate --provision-runtime-users` with the matched private credentials before
starting API/worker. The [recovery drill](recovery.md) now does this and pins the
operator image to the source API image. It never copies or restores the source
`master` database. Retain a protected configuration backup separately from the
SQL/key bundle.

Native Windows deployments need their own reviewed principals/configuration;
the local Docker scripts never modify the existing Windows SQL instance.
SQL Express container health still uses `sa`; changing that bootstrap account's
handling is separate from runtime application access.

Public staging host/domain, scheduled encrypted off-host backups, alerting,
secret-store integration, disk monitoring, measured recovery targets and a
public-host deployment/rollback exercise remain open under STAGE-01/OPS-01.
This is a tested database-access improvement, not acceptance of all operations.
