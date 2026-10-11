# First staging deployment

Status on 2026-10-11: local Docker validation is available; no public staging host
exists. The public-host guide below uses the existing production Compose definition
on a dedicated staging host so that production behavior is exercised.

## Local validation on Windows

Start Docker Desktop with Linux containers, then run these commands in PowerShell 7
from the repository root:

```powershell
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalDeployment.ps1
# Include explicit bootstrap and the real SQL Server care journey:
./scripts/Test-LocalDeployment.ps1 -IncludeCareJourney
```

The start script creates a private, ignored `.env.local` with a random SQL password
on first use. It preserves that file and existing volumes on subsequent runs.
The override binds only localhost ports: the app at `https://localhost:8443` and
Mailpit at `http://localhost:8025`. Caddy uses its local CA; the smoke script skips
certificate verification only for its fixed localhost origin. Browser certificate
trust must be configured separately for interactive browser/PWA testing.

The smoke script creates synthetic accounts and captures their confirmation emails
locally. It verifies HTTPS, readiness, SPA serving, API routing, production email
confirmation enforcement, real cookie login, account identity, and protected logout.
Push and external calendars are deliberately unconfigured in the generated local
environment and remain unverified. The extended option provisions a synthetic
administrator and demo catalog, then executes the core care journey. See
[bootstrap.md](bootstrap.md) for production setup and SSMS access at `localhost,14330`.
Run `./scripts/Test-LocalAccountIsolation.ps1` for real-cookie cross-account checks
against the same SQL Server stack; see [account-isolation.md](account-isolation.md).
Run `./scripts/Test-LocalAuthenticationSecurity.ps1` for reset email and browser
session checks; see [authentication-security.md](authentication-security.md).

To stop the local stack and preserve its data:

```powershell
docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local down
```

Fresh images initialize the key directory for the non-root `app` user. If an older
deployment already created a root-owned key volume, stop API/worker and repair
ownership on that volume before restarting; rebuilding does not change existing
volume ownership. Back up production keys before any ownership maintenance.
For the local stack only:

```powershell
docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local stop api worker
docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local run --rm --no-deps --user root --entrypoint sh api -c 'chown app:app /var/lib/plantcare/keys && chmod 700 /var/lib/plantcare/keys'
./scripts/Start-LocalDeployment.ps1
```

Five previously undiscoverable migrations now run during startup. Before upgrading
any database whose tables were created manually or with `EnsureCreated`, reconcile
its schema and migration history on a backup copy; do not assume it can safely
apply migrations over preexisting tables.

## Provision the environment

1. Choose a dedicated Linux host compatible with the SQL Server container and
   sized for SQL Server, image builds, API, and worker. Record its owner, access
   method, available storage, and backup destination. Keep staging data synthetic.
2. Install Docker Engine and the Compose plugin. Verify `docker version` and
   `docker compose version`. Do not reuse an existing production Compose project.
3. Point a dedicated staging DNS name at the host; permit inbound 80/443
   (optionally UDP 443) and restrict administrative access.
4. Provision staging SMTP delivery and separate VAPID keys. Store credentials in
   the host's private `.env` or secret store. Keep optional calendar providers
   disabled for the first core-care validation.
5. Follow [bootstrap.md](bootstrap.md) to promote a confirmed administrator and
   import reviewed catalog data through explicit operator commands.

## Start a candidate

On the isolated Linux staging host, check out the candidate commit, then run:

```bash
git clone https://github.com/Odin2325/PlantCare.git
cd PlantCare
git checkout <candidate-sha>
cp .env.example .env
chmod 600 .env
```

Replace every placeholder privately. Use a strong database password, staging
domain/email values, and actual staging VAPID keys. Never commit `.env`.
Record the candidate SHA and non-secret configuration choices in the acceptance
record. Commands below use a separate project name to isolate volume names:

```bash
docker compose --project-name plantcare-staging config --quiet
docker compose --project-name plantcare-staging up --build -d --wait --wait-timeout 300
docker compose --project-name plantcare-staging ps
docker compose --project-name plantcare-staging logs --tail=100 api worker proxy
```

`--wait` checks available service health checks/running state; the worker has no
health check, so inspect its logs and verify actual reminder processing.
Confirm that migrations completed and both API and worker can write/read the
shared data-protection volume as their non-root container users. A successful
image build alone does not establish this.

```bash
curl --fail --silent --show-error https://YOUR_STAGING_DOMAIN/health/live
curl --fail --silent --show-error https://YOUR_STAGING_DOMAIN/health/ready
```

Open the HTTPS site in a supported browser and execute the acceptance checklist,
beginning with register -> confirm -> login -> add plant -> configure schedule
-> receive reminder -> complete care -> verify history and next due date.
Use a short interval/preferred time in synthetic test data rather than altering
the host clock. Test account B for ownership isolation.

## Restart, upgrade, and recovery checks

Record the existing plant/history/notification state, restart services, and verify
the same state and functioning sign-in/reminder delivery:

```bash
docker compose --project-name plantcare-staging restart api worker database
docker compose --project-name plantcare-staging ps
```

Allow database/API readiness to recover before repeating the journey. Review
logs for worker recovery. Run upgrades against preserved staging data, not only
empty databases. Before an upgrade, take a consistent SQL Server backup plus a
matching protected copy of the key ring. Restore them into another isolated
environment and verify recovery. The [recovery runbook](recovery.md) supplies an
executable local SQL/key-ring restore drill and proposed retention. An off-host
destination, scheduler, alerts, and public-staging recovery evidence remain
required under DATA-01/OPS-01.

Startup migrations can make an older application incompatible with the new
schema. Record a schema-compatible rollback or tested database recovery path
before upgrading; switching only the source commit is not a recovery plan.

Stop staging without deleting its data:

```bash
docker compose --project-name plantcare-staging down
```

Do not remove volumes to simulate a fresh deployment. Use another isolated
project/host for fresh-database checks and retain failed-run evidence.

## Record failures

For each failure, create a backlog entry/issue with: candidate SHA, environment,
preconditions, exact reproduction, expected result, actual result, sanitized
evidence, severity, and acceptance condition. Mark tests blocked by prerequisites
as NOT RUN; never mark them passed. Update backlog.md and the acceptance record
after each deployment attempt.
