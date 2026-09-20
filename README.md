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
