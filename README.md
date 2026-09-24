# PlantCare

PlantCare is an Angular and ASP.NET Core plant-care scheduling application.

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

3. Build and start PlantCare:

   ```bash
   docker compose up --build -d
   ```

4. Follow startup and migration logs:

   ```bash
   docker compose logs -f api worker proxy
   ```

Caddy obtains and renews the TLS certificate automatically. On startup, the API
waits for SQL Server, applies pending Entity Framework migrations, seeds roles,
and then becomes healthy. The worker starts only after that health check passes.

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
