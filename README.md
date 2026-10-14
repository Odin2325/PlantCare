# PlantCare

PlantCare is an Angular and ASP.NET Core plant-care scheduling application.

## Release preparation

Start with the [v1 acceptance checklist](docs/release/v1-acceptance.md),
then follow the [staging runbook](docs/release/staging.md).
The [domain and hosting handoff](docs/release/domain-hosting.md) covers
`plantacare.org`, the staging DNS record and choosing a compatible server.
For the selected 4 GB OVHcloud VPS, use the [prebuilt-image deployment guide](docs/release/budget-staging.md).
The [public staging record](docs/release/public-staging.md) covers the running
synthetic environment at <https://staging.plantacare.org> and remaining release gates.
The [release backlog](docs/release/backlog.md) records current blockers and
the local verification results.
Use the [administrator and catalog bootstrap guide](docs/release/bootstrap.md)
for first setup, extended care-journey checks, and SSMS connection details.
Use the [recovery runbook](docs/release/recovery.md) to test database backups,
key-ring restoration, and worker recovery in isolated Docker stacks.
Run the [SQL Server account-isolation checks](docs/release/account-isolation.md)
to verify private data boundaries with real sign-in sessions.
Use the [password-reset and session-security checks](docs/release/authentication-security.md)
to verify reset emails, session invalidation, and antiforgery protection.
Run the [desktop/mobile browser journey](docs/release/browser-journey.md)
to verify the account, plant-care, history, calendar, and password-reset forms.
See the [weekday and time-zone policy](docs/release/weekday-scheduling.md)
for same-day care and daylight-saving behavior.
Use the [push retry and reliability checks](docs/release/push-reliability.md)
to verify partial failures, bounded retries, and queue progress on SQL Server.
See [restricted database access](docs/release/database-access.md) for runtime
principals, explicit migrations, permission checks and restored login mapping.
See [encrypted local backups](docs/release/local-backups.md) for the external-drive
snapshot, Windows schedule, health checks and separate recovery-key requirements.
See [VPS backups and monitoring](docs/release/vps-backups.md) for encrypted
off-server snapshots, the tested SQL restore and remaining independent alerts.
See [uptime monitoring](docs/release/uptime-monitoring.md) for the controlled
public outage drill and [catalog roadmap](docs/release/catalog-roadmap.md) for
reviewed species content and deferred image features.
See [VPS application recovery](docs/release/vps-application-recovery.md) for
the isolated restored-account, worker, care-data and failed-activation drill.
See [PWA acceptance](docs/release/pwa.md) for offline behavior, controlled updates
and the remaining physical-device installation checks.
See [accessibility and beta checks](docs/release/accessibility-beta.md) for keyboard
navigation, automated scans and the manual device/two-care-cycle record.

## Production deployment

The Docker deployment runs four containers:

- Caddy terminates HTTPS and forwards requests to the application.
- The ASP.NET Core API serves both the API and the compiled Angular PWA.
- The worker generates notifications and delivers web push messages.
- SQL Server stores application and Identity data in a named volume.

### Prerequisites

- A Linux host with Docker Engine and the Docker Compose plugin
- A public DNS record pointing your domain to that host
- Inbound TCP ports 80 and 443 open (and UDP 443 for HTTP/3)
- SMTP credentials and the existing VAPID push-notification keys

### Launch

1. Copy the environment template:

   ```bash
   cp .env.example .env
   ```

2. Replace every placeholder in `.env`. Keep this file private; Git ignores it.

3. Build, migrate/provision with the operator job, then start runtime services
   (stop on any failure):

   ```bash
   docker compose config --quiet
   docker compose build api worker
   docker compose up -d --wait database
   docker compose stop api worker
   docker compose run --rm --no-deps migrate --migrate-database
   docker compose run --rm --no-deps migrate --provision-runtime-users
   docker compose up --no-build -d --wait --wait-timeout 300
   ```

4. Follow startup and migration logs:

   ```bash
   docker compose logs -f api worker proxy
   ```

Caddy obtains and renews the TLS certificate automatically. The one-shot operator
job applies migrations/seeds roles and provisions separate API/worker SQL users.
Runtime containers do not receive the operator connection or migrate at startup.
The worker starts after API readiness passes.

### Operations

```bash
# Show service health and status
docker compose ps

# Pull base-image updates and recreate the deployment
docker compose build --pull
docker compose up -d

# Stop without deleting database or certificate volumes
docker compose down
```

Do not run `docker compose down --volumes` unless you intend to permanently
delete the database and Caddy certificate data. Back up the `plantcare_sql-data`
volume before host migrations or destructive maintenance.

The public liveness endpoint is `/health/live`; `/health/ready` additionally
checks database connectivity.

## Google Calendar synchronization

Google synchronization is optional. In Google Cloud Console, enable the Google
Calendar API, configure the OAuth consent screen, and create an OAuth 2.0 Web
application. Add this exact authorized redirect URI:

```text
https://YOUR_PLANTCARE_DOMAIN/api/calendar/integrations/google/callback
```

Then set these values in `.env` and recreate the API container:

```dotenv
GOOGLE_CALENDAR_ENABLED=true
GOOGLE_CALENDAR_CLIENT_ID=your-client-id
GOOGLE_CALENDAR_CLIENT_SECRET=your-client-secret
```

```bash
docker compose up -d --build api
```

OAuth access and refresh tokens are encrypted before they enter the database.
The encryption key ring is stored in the persistent
`plantcare_data-protection-keys` volume; back it up with the database. PlantCare
requests only identity, email, and calendar-event access. Synchronization is
one-way and touches only events tagged as PlantCare exports.

## Outlook and Microsoft 365 synchronization

Microsoft calendar synchronization is optional. In Microsoft Entra, register a
web application and add this exact redirect URI:

```text
https://YOUR_PLANTCARE_DOMAIN/api/calendar/integrations/microsoft/callback
```

Create a client secret, then set these values in `.env` and recreate the API
container:

```dotenv
MICROSOFT_CALENDAR_ENABLED=true
MICROSOFT_CALENDAR_CLIENT_ID=your-application-client-id
MICROSOFT_CALENDAR_CLIENT_SECRET=your-client-secret-value
MICROSOFT_CALENDAR_TENANT=common
```

```bash
docker compose up -d --build api
```

The `common` tenant permits personal Microsoft accounts as well as work and
school accounts. Replace it with your tenant ID to restrict sign-in to one
organization. PlantCare uses authorization-code flow with PKCE and delegated
`User.Read` and `Calendars.ReadWrite` permissions. Tokens are encrypted with
the same persistent data-protection key ring used by Google synchronization.
Each PlantCare user connects and consents to their own calendar; no individual
approval by a PlantCare administrator is required. An organization's Microsoft
Entra policies may still require approval from that organization's own admin.

Synchronization is one-way. PlantCare records which Microsoft events it
created, updates those events on later synchronizations, and does not modify
unrelated calendar events.

## Automatic external-calendar synchronization

The worker automatically synchronizes connected Google and Microsoft calendars.
By default, it checks every 15 minutes and synchronizes each connection at most
once every six hours, processing 25 connections per pass. Configure these values
in `.env` when needed:

```dotenv
EXTERNAL_CALENDAR_SYNC_ENABLED=true
EXTERNAL_CALENDAR_SYNC_POLL_MINUTES=15
EXTERNAL_CALENDAR_SYNC_INTERVAL_HOURS=6
EXTERNAL_CALENDAR_SYNC_BATCH_SIZE=25
```

The API and worker must use the same data-protection key ring because calendar
tokens are encrypted. Docker Compose mounts the shared
`plantcare_data-protection-keys` volume and supplies the same provider settings
to both services automatically. Back up this volume together with the database.

For local worker testing, configure the same Google or Microsoft settings in
both the API and worker user-secrets stores, and give both processes the same
absolute `DataProtection:KeysPath`. Manual synchronization remains available
from the calendar page at any time.
