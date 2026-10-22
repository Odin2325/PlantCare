# Administrator and catalog bootstrap

These explicit operator commands exit without starting HTTP or running startup
migrations. Validate/migrate the intended database first. Normal startup does
not promote accounts. Use the deployment's intended connection/environment settings.

## First administrator

Register an ordinary account through the application and confirm its email using
the delivered link. On the application host, run:

```bash
docker compose run --rm --no-deps migrate --bootstrap-admin operator@example.com
```

For the isolated local stack:

```powershell
docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local run --rm --no-deps migrate --bootstrap-admin operator@example.com
```

The command rejects missing/unconfirmed accounts, creates the Admin role if
necessary, and is safe to repeat. It never creates an account, sets a password,
or bypasses email confirmation. Log in again for updated role claims. Access
requires operator/container privileges; there is no public promotion endpoint.
Use the same Compose project/environment as the intended deployment (for example,
add `--project-name plantcare-staging` for that staging project). Runtime API
accounts cannot write Identity roles; bootstrap uses the privileged one-shot job.

For a native Windows deployment, use `dotnet run --project PlantCare.Api --
--bootstrap-admin operator@example.com`, with the intended connection string
and environment. The existing localhost database was inspected read-only during
this work and has not been migrated or modified by these checks.

## Populate the catalog

The reviewed English starter dataset is `deploy/catalog.houseplants.json`, with
20 real houseplants, linked care references and licensed photo metadata. See
[the starter record](houseplant-starter-catalog.md) and [flowering additions](flowering-houseplant-batch.md).
`deploy/catalog.example.json` remains a synthetic test fixture.

After migration, apply the reviewed dataset through the privileged one-shot job
using the intended project's environment and pinned images:

```bash
docker compose run --rm --no-deps migrate --upsert-catalog /operator/catalog.houseplants.json
```

This validates the complete dataset, rejects duplicate scientific-name keys and
atomically adds/updates shared species while preserving their IDs and existing
personal plant references, schedules, notes and care history. Do not run concurrent
imports. Staging activation runs this command automatically. Its public `deploy`
directory must be traversable and the JSON readable by the container's non-root
user; the activation helper sets the four known public input files to mode 0644
and `deploy` to 0755 after validating checksums.

For future entries, prepare a reviewed JSON array using the same schema and
attribution checks. The existing add-only importer also remains available:

```bash
docker compose run --rm --no-deps migrate --import-catalog /operator/reviewed-catalog.json
```

The importer validates all input, rejects unknown JSON fields, and uses a
transaction so failures do not leave partial imports. Add-only reimports skip existing
species by trimmed, case-insensitive scientific name, or common name if scientific
name is absent. They do not overwrite existing records; use the reviewed upsert
procedure or administrator API to edit them. A nonzero exit code means failure.

## Repeatable local acceptance

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalDeployment.ps1 -IncludeCareJourney
```

Each extended run creates/confirms a synthetic account, explicitly promotes it,
imports the demo fixture twice, checks catalog authorization, and tests plant tags,
overdue dashboard, worker reminder, read acknowledgement, completion, next due date,
history, calendar, and archiving. The local worker polls every five seconds.
Synthetic accounts, archived plants, and the demo catalog remain in the isolated
Docker database. The separate localhost Windows SQL instance is not modified.

## Inspect the Docker database with SSMS

| Setting | Value |
| --- | --- |
| Server | `localhost,14330` |
| Authentication | SQL Server Authentication |
| User | `sa` for operator inspection; runtime uses `plantcare_api` / `plantcare_worker` |
| Password | `SQL_PASSWORD` in the private `.env.local` file |
| Database | `PlantCare` |
| Certificate | Trust the server certificate for this local development connection |

The port is bound to loopback only. Your existing Windows-authenticated instance
stays at `localhost`. See [database-access.md](database-access.md) for the tested
runtime grants and remaining operations work.
