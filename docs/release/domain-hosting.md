# plantacare.org: hosting and DNS handoff

Recorded 2026-10-13: the owner purchased `plantacare.org` at Porkbun. The
management screenshot shows auto-renew, domain lock and contact privacy enabled.
Update 2026-10-14: the owner purchased an OVHcloud VPS-1 (4 GB RAM/40 GB NVMe,
UK, Ubuntu 26.04, checkout EUR 5.34/month including VAT). Delivery is pending.
Follow [budget-staging.md](budget-staging.md) for prebuilt images and deployment
within the EUR 15/month budget. The earlier 8 GB Hetzner recommendation below
is superseded for this deployment; its CX plans were unavailable.
No public deployment exists yet. Public DNS checked on 2026-10-13 resolves
the apex and `staging.plantacare.org` to Porkbun's parking service; that is not
PlantCare. The prior application candidate `2c00bb2` passed all three CI jobs
(Frontend, Backend and Production container), run `38468089723`.

## What to do in Porkbun now

Keep auto-renew, domain lock and contact privacy enabled. Keep the current
Porkbun nameservers. Enable account two-factor authentication if not already
enabled. Web/email hosting add-ons, URL forwarding, glue records, custom
nameservers, Cloudflare connection and API access are not needed for this setup.
Certificate issuance will be handled by Caddy on the server.

Use `staging.plantacare.org` for synthetic beta testing. Reserve `plantacare.org`
for release. The purchased spelling is intentional configuration here: do not
substitute `plantcare.org`, which is a different domain.

## Choose the server

Recommended first staging configuration (engineering estimate, not a measured
capacity guarantee): a dedicated Linux VM with Intel/AMD x86-64, 4 shared vCPUs,
8 GB RAM, at least 80 GB SSD and a public IPv4 address. Ubuntu 24.04 LTS is a
suitable Docker host. Builds share this host with SQL/API/worker, so smaller
machines have less headroom. Monitor memory/storage and resize if necessary.

Hetzner Cloud in Germany is a reasonable first option. CX33 matches this size if
available; otherwise choose an available Intel/AMD plan meeting the same size.
Do not substitute an Arm/CAX server: Microsoft's SQL Server Linux containers
support Intel/AMD x86-64. Check the live checkout total, including VAT, IPv4 and
any selected backup options; the owner must approve/pay for the server. A VPS
requires ongoing Linux/Docker patching and operations by its owner.

Use SSH keys. Configure a provider firewall allowing inbound TCP 80/443 from
the internet and TCP 22 only from the administrator's current IP; UDP 443 is
optional. Do not expose SQL Server 1433, API 8080 or Mailpit. Keep one worker.
Provider snapshots complement, but do not replace, consistent encrypted SQL
and key-ring backups stored off the server.

Before deployment, supply only these non-secret details to the assistant:

- Provider, server location/type, public IPv4 and SSH username.
- Whether key-based SSH already works, and the local SSH alias/config path.
- SMTP provider and the path to its private credentials configuration (no secrets
  in chat). App confirmation/reset email requires SMTP; the connected Gmail
  backup-alert connector is not an app SMTP configuration.
- Planned off-server backup destination and operator contact.

Keep SSH private keys, SMTP passwords, database passwords and VAPID private keys
out of chat and Git. Staging needs fresh credentials, isolated volumes and
synthetic data; do not upload the local `.env.local` or user database.

## DNS changes once the server IPv4 is known

In Porkbun **DNS Records**, inspect the two existing records before editing.
Porkbun parking may include a wildcard that also makes staging resolve.

| Type | Host | Answer | TTL |
| --- | --- | --- | --- |
| A | `staging` | Actual server public IPv4 | 600 seconds |

Remove only a conflicting record for the exact `staging` host before adding its
A record. Keep unrelated apex, wildcard, MX/TXT and email records. An explicit
staging record takes precedence over wildcard parking; do not remove the wildcard
merely to create staging. Do not enter `https://`, a port or a path as the answer.
Do not add AAAA until server IPv6 routing and firewall are tested. DNS caches can
retain previous parking answers until their existing TTL expires.

Set `PLANTCARE_DOMAIN=staging.plantacare.org` in the staging server's private
`.env` and follow [staging.md](staging.md) to build, migrate, provision and start
the candidate. DNS plus reachable TCP 80/443 allows Caddy to obtain the public
certificate automatically. Keep the normal TLS verification enabled.

From PowerShell 7 on the owner's computer, after the server has started:

```powershell
./scripts/Test-StagingEndpoint.ps1 -ExpectedIPv4 '<actual server IPv4>'
```

This read-only check fails before HTTP if DNS does not resolve exclusively to
that IPv4 or if IPv6 is present. It then checks the same-host HTTP redirect,
trusted HTTPS, health endpoints, Angular root/login routes and unknown API 404.
It creates no accounts and prints no remote response bodies or headers. It is
intentionally for the first IPv4-only direct deployment, not a CDN or dual-stack
configuration. Do not use certificate bypasses to make the public check pass.

Record real SMTP confirmation/reset, worker reminder/push, mobile installation,
screen-reader and off-server recovery results in [v1-acceptance.md](v1-acceptance.md).
The endpoint check alone cannot accept public staging or launch.

At release, configure the apex separately and decide whether `www` redirects to
it. Do not point the apex at unfinished staging now. DNSSEC can be enabled through
Porkbun's managed option once DNS is settled; verify resolution afterward rather
than entering guessed DS records. No domain/DNS changes are automated by this guide.

## References

- [Porkbun: point a domain with A records](https://kb.porkbun.com/article/54-pointing-your-domain-to-hosting-with-a-records)
- [Hetzner Cloud plans](https://www.hetzner.com/cloud/)
- [SQL Server Linux/container requirements](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/setup?view=sql-server-ver17)
- [Caddy automatic HTTPS](https://caddyserver.com/docs/automatic-https)
