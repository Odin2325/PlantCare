# Administrator and catalog bootstrap

These explicit operator commands exit without starting HTTP or running startup
migrations. Validate/migrate the intended database first. Normal startup does
not promote accounts. Use the deployment's intended connection/environment settings.

## First administrator

Register an ordinary account through the application and confirm its email using
the delivered link. On the application host, run:

```bash
docker compose exec -T api dotnet PlantCare.Api.dll --bootstrap-admin operator@example.com
```

For the isolated local stack:

```powershell
docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local exec -T api dotnet PlantCare.Api.dll --bootstrap-admin operator@example.com
```

The command rejects missing/unconfirmed accounts, creates the Admin role if
necessary, and is safe to repeat. It never creates an account, sets a password,
or bypasses email confirmation. Log in again for updated role claims. Access
requires operator/container privileges; there is no public promotion endpoint.

For a native Windows deployment, use `dotnet run --project PlantCare.Api --
--bootstrap-admin operator@example.com`, with the intended connection string
and environment. The existing localhost database was inspected read-only during
this work and has not been migrated or modified by these checks.

## Populate the catalog

Prepare a JSON array matching `deploy/catalog.example.json`. That file contains
synthetic demonstration data, not reviewed botanical guidance or a launch catalog.
Replace it with reviewed species data before public release, including care
instructions and toxicity information. Then copy/import your file:

```bash
docker compose cp ./reviewed-catalog.json api:/tmp/reviewed-catalog.json
docker compose exec -T api dotnet PlantCare.Api.dll --import-catalog /tmp/reviewed-catalog.json
```

The importer validates all input, rejects unknown JSON fields, and uses a
transaction so failures do not leave partial imports. Reimports skip existing
species by trimmed, case-insensitive scientific name, or common name if scientific
name is absent. They do not overwrite existing records; use the administrator API
to edit them. Do not run concurrent imports. A nonzero exit code means failure.

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
| User | `sa` (current local Compose configuration) |
| Password | `SQL_PASSWORD` in the private `.env.local` file |
| Database | `PlantCare` |
| Certificate | Trust the server certificate for this local development connection |

The port is bound to loopback only. Your existing Windows-authenticated instance
stays at `localhost`. Restricted runtime database credentials remain OPS-01.
