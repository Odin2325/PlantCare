# Deploy prebuilt images to the budget VPS

Status 2026-10-14: owner purchased OVHcloud VPS-1 2027, 2 vCores, 4 GB RAM,
40 GB NVMe, UK, Ubuntu 26.04. Checkout showed one month at EUR 5.34 including
VAT and an included promotional automated backup. Budget is EUR 15/month total.
Initial order validation/delivery was pending when this guide was prepared.
Update 2026-10-14: host setup, reboot/key access, Docker and bundle import now
pass; see [staging-host.md](staging-host.md). Later on 2026-10-14 the app and real
SMTP checks also passed; [public-staging.md](public-staging.md) records the scope
and remaining gates.
Confirm IPv4 inclusion and renewal pricing when delivery completes. UK is the
selected location; Europe and EU are not interchangeable descriptions.

The current SQL Server/API/worker/Caddy stack will be measured on this machine
before beta acceptance. Four GB is a starting estimate, not verified capacity.
Build on the owner's Windows PC, transfer only the runtime bundle over SSH, and
keep one worker. Do not build Angular/.NET on the small VPS. Docker supports
Ubuntu 26.04; the database remains in its Linux amd64 container.

## Prepare the candidate on Windows

With Docker Desktop running Linux containers and PowerShell 7:

```powershell
./scripts/Export-StagingBundle.ps1
# Or select an exact committed candidate:
./scripts/Export-StagingBundle.ps1 -Candidate '<full 40-character commit SHA>'
```

The exporter uses `git archive` for committed source only, builds Linux amd64
API/worker images, and saves both to `images.tar`. It never packages the working
directory, `.env.local`, SQL data, key ring or backup repository. It does not
stop or recreate local services, retag existing local-deployment images or send traffic
to the VPS. Builds can download dependencies/base images.

Output is ignored `.operations-local/deployment-bundles/<candidate-sha>/`:

- `images.tar`, with SHA-tagged application images.
- Build-free `compose.yaml`, `images.env`, and `manifest.json` with image IDs.
- `environment.example` with placeholders and the two allowlisted deploy files.
- `SHA256SUMS` for the seven payload files, using Linux-compatible line endings.

An existing candidate bundle is preserved, not overwritten. Build/transfer
checks establish packaging only; use a candidate whose CI gates pass. Mutable
base-image tags still mean separate builds of the same source need not have the
same bytes. Deploy the exported archive, verify its hash/image IDs, and retain
that exact artifact for rollback. Checksums detect corruption, not an attacker
who can replace both the payload and its checksum list; transfer using verified SSH.

## First connection after delivery

Open OVHcloud Bare Metal Cloud > Virtual private servers > the delivered VPS.
Record IPv4, OS and login method. Ubuntu normally uses `ubuntu` with sudo.
The owner must handle temporary-password setup locally if no SSH key was selected.
Verify the SSH host fingerprint through the provider console before accepting it.
Install the owner's public SSH key, confirm a second key-based session works,
then disable password login. Never disable the only working access method.

Install Docker Engine and Compose from Docker's official Ubuntu repository.
Check `uname -m` reports `x86_64`, `docker version`, `docker compose version`,
`free -h` and `df -h`. Restrict inbound SSH to the owner's IP and expose only
TCP 80/443 (optional UDP 443) publicly. Docker-published ports can bypass UFW;
do not publish database/API/mail ports. The provided runtime Compose publishes
only Caddy. Confirm the provider's firewall capabilities rather than applying
the earlier Hetzner-specific UI instructions to OVHcloud.

## Transfer and start the first candidate

After key-based SSH is configured, create a private upload directory on the VPS.
Copy the *contents* of the candidate bundle there with `scp`; do not upload the
repository's ignored state directories. Keep application data in the stable
`plantcare-staging` Compose project, even when changing candidate directories.
These commands are for the uploaded bundle directory on the Linux VPS:

```bash
set -euo pipefail
sha256sum --check SHA256SUMS
# The non-root catalog operator needs access to these non-secret mount files.
chmod 755 deploy
chmod 644 deploy/Caddyfile deploy/catalog.example.json deploy/catalog.houseplants.json deploy/catalog-images.json
sudo docker image load --input images.tar
# Compare both loaded IDs/tags against manifest.json before proceeding.
sudo docker image inspect --format '{{.Id}}' 'plantcare-api:<candidate-sha>'
sudo docker image inspect --format '{{.Id}}' 'plantcare-worker:<candidate-sha>'
```

Create the staging-only private `../../.env` in the owner-only staging root
from `environment.example`, using mode 600. Preserve this file across candidate
directories; do not overwrite an existing deployment configuration.
Set `PLANTCARE_DOMAIN=staging.plantacare.org`, three fresh distinct SQL
passwords, real SMTP settings and separate VAPID keys (or leave push disabled
until ready). Keep optional calendar providers disabled. Do not reuse the
local database or credentials. Configure the staging DNS A record as described
in [domain-hosting.md](domain-hosting.md) before starting Caddy.

In the same Bash session:

```bash
cat > compose.budget.yaml <<'EOF'
services:
  database:
    environment:
      MSSQL_MEMORY_LIMIT_MB: '2048'
EOF
dc() { sudo docker compose --project-name plantcare-staging --env-file ../../.env --env-file images.env -f compose.yaml -f compose.budget.yaml "$@"; }
dc config --quiet
dc pull database proxy
dc up --no-build --pull never -d --wait --wait-timeout 300 database
dc stop api worker
dc run --rm --no-deps --pull never migrate --migrate-database
dc run --rm --no-deps --pull never migrate --provision-runtime-users
dc up --no-build --pull never --force-recreate --no-deps -d --wait --wait-timeout 300 api worker
dc up --no-build --pull never -d --wait --wait-timeout 300
dc exec -T api dotnet PlantCare.Api.dll --check-runtime-access api
dc run --rm --no-deps --pull never worker-db-check
dc ps
sudo docker stats --no-stream
free -h
df -h
```

Do not override `PLANTCARE_API_IMAGE`/`PLANTCARE_WORKER_IMAGE` in the shell: shell
variables take precedence over env files. The exported Compose contains no
build instructions. Public SQL/API ports remain unpublished; migrations and
runtime users still use the existing explicit operator jobs. A failed migration
leaves writers stopped; diagnose before proceeding. These are first-deployment
commands, not authorization to upgrade a database without a tested backup and
schema-compatible recovery plan. Follow [staging.md](staging.md) for the rest.

From Windows, run `Test-StagingEndpoint.ps1 -ExpectedIPv4 '<actual IPv4>'`.
Then execute the real-email/auth, care, worker, physical-device/PWA and recovery
acceptance checks. Observe memory, disk and OOM/restart counts during the browser
journey and a backup, not just at idle. If the stack cannot maintain headroom,
stop acceptance and decide whether to resize or change the database. Do not
mask memory exhaustion by claiming swap makes RAM requirements disappear.

Keep the exact archive somewhere off the VPS; remove a transferred archive from
the VPS only after verifying a retained copy and reviewing disk use. Keep image
cleanup targeted to reviewed unused candidates; avoid broad Docker prune commands.
Provider backups complement consistent encrypted database/key-ring backups and
off-server recovery. The local Windows backup scheduler does not back up this VPS.
SMTP, backup storage and renewal prices must fit the remaining monthly budget.

## References

- [OVHcloud first VPS setup](https://docs.ovhcloud.com/en/guides/bare-metal-cloud/virtual-private-servers/starting-with-a-vps)
- [Docker Ubuntu installation and firewall notes](https://docs.docker.com/engine/install/ubuntu/)
- [Docker image save/load archive](https://docs.docker.com/reference/cli/docker/image/save/)
- [Compose env-file precedence](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/)
