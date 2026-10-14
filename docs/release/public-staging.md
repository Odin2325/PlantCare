# Public staging deployment and verification

Latest runtime update 2026-10-14:
`cfbc073cb79fb228149f98fa21e2b257e5a651a5` sends new reminder taps directly
to the dashboard using Angular's `openWindow` operation. See [push-reliability.md](push-reliability.md)
for CI/recovery/activation evidence and the pending physical Android tap retest.
Installation and app launch were reported working by the owner; this does not
close the remaining PWA/device acceptance checks. The records below remain
historical evidence for their stated candidates.
Exact-candidate [CI](https://github.com/Odin2325/PlantCare/actions/runs/38604044126),
fresh encrypted pre-update backup, isolated runtime restore/rollback,
verified image activation, API/worker database access and trusted public endpoint
checks passed. No schema migration was required. Physical dashboard taps remain
pending; see [public browser acceptance](public-browser-acceptance.md) for the
completed automated account/care/reset samples.

Earlier operational update 2026-10-14: the runtime was
`d3d81f30cc2a6bbd06d92cb2781d7b5e40a3924d`, with worker/disk operations health
and a patched cryptography dependency. See [worker-capacity.md](worker-capacity.md)
for exact-candidate CI, isolated compatibility, activation and bounded read
evidence. The original deployment/auth/browser table below records `0acdb2d`;
do not relabel those historical checks as a full new-candidate public acceptance.

Verified 2026-10-14 at <https://staging.plantacare.org>. This is a synthetic
staging environment, not an accepted production release. The apex domain is
reserved for release. Runtime bundle candidate:
`0acdb2db70a343d1aae977bf4bb19150deaaaf7a`.

## Deployment

The four services run in the stable `plantcare-staging` Compose project on the
owner's OVHcloud UK Ubuntu 26.04 x86-64 VPS (4 GB RAM / 40 GB NVMe). API/worker
images came from the original prebuilt bundle; remote image IDs and all seven
payload SHA-256 checks matched. SQL Server Express and Caddy were pulled on the
host. No Angular/.NET compilation was performed there.

Private configuration is `/home/ubuntu/plantcare-staging/.env`, mode 600, under
an owner-only parent. It contains fresh distinct operator/API/worker database
passwords, staging VAPID keys and Brevo SMTP credentials. No values are recorded
here or committed. SMTP upload scratch was removed after trusted STARTTLS and
authentication succeeded. External calendar providers remain disabled.

The selected release directory is
`/home/ubuntu/plantcare-staging/releases/<candidate-sha>/`. Commands use both
`compose.yaml` and a host-specific `compose.budget.yaml`, which sets
`MSSQL_MEMORY_LIMIT_MB=2048`. See [budget-staging.md](budget-staging.md). This is
a SQL memory setting, not a total-host capacity guarantee. Only Caddy publishes
web ports; SQL/API/mail ports are not exposed. SSH remains key-only and restricted
to the administrator's current public IPv4; see [staging-host.md](staging-host.md).

All 15 migrations completed through the explicit operator job, then separate
runtime logins were provisioned. Checks on the actual API connection and separate
worker-check connection passed intended-principal/grant restrictions. An initial
verification invocation incorrectly used the privileged migration connection;
the check correctly rejected it, and verification was rerun on the actual API.
No runtime privilege was widened.

The non-root catalog importer initially could not traverse the transferred
`deploy` directory. Only that non-secret directory was set to 755 and its two
fixture/config files to 644; credential files and private parents remain private.
The clearly labelled `Demo houseplant` synthetic fixture was imported. It is
not reviewed botanical content and must be replaced for release.

## Checks passed

| Area | Evidence |
| --- | --- |
| Public endpoints | DNS matches host; HTTP redirects to the same HTTPS origin; public certificate validates; liveness/readiness succeed; Angular root/login deep link and unknown API 404 pass. No certificate bypass. |
| Real confirmation email | Owner-authorized test message reached Gmail INBOX; provider redirect resolved only to the expected staging confirmation route; confirmation and login succeeded. Login before confirmation was rejected. |
| Real password reset | Owner-authorized reset reached Gmail INBOX; reset route/account checked; new password accepted, old password and reused reset code rejected; existing session revoked; protected logout passed. Passwords/links stayed private. |
| Core care and worker | Ordinary account cannot modify catalog; synthetic plant/tags, overdue dashboard, worker reminder within two default polling intervals plus margin, read acknowledgement, completion, next due date, history and calendar passed. Test plant archived and archived completion rejected. |
| Browser public pages | Existing public keyboard/reflow/axe suite: 3/3 on actual staging in desktop Chromium, mobile-sized Chromium and Firefox. Certificate verification enabled; no screenshots, traces or videos. These were public-page checks, not the entire authenticated browser suite. |
| Restart persistence | SQL/API/worker restarted; readiness recovered; existing cookie session and persisted reminders survived. This is not a database restore drill. |
| Initial resources | After tests/restart: about 2.5 GiB available host RAM, 32 GiB free disk; SQL ~557 MiB, API ~85 MiB, worker ~58 MiB, proxy ~35 MiB. No observed OOM or automatic container restarts. Light test workload only. |

Synthetic test-account credentials remain in ignored private local state. The
owner should use Forgot password to choose their own password before normal
use; the confirmation/reset test links have already been consumed. The test
account remains ordinary, without automatic administrator promotion.

Brevo wrapped the confirmation/reset links in tracking redirects. Their final
routes were validated without printing URLs or forwarding app cookies to the
provider. Review transactional tracking/privacy configuration before release.
No marketing campaign was created. Tests sent only the two authorized app emails.

## Still open

The encrypted VPS snapshot also passed a full local isolated application drill
on 2026-10-14: account recovery through captured mail, sign-in, runtime login
remapping, care/worker/restart behavior and same-schema failed-activation rollback
with session/history/keys preserved. See
[vps-application-recovery.md](vps-application-recovery.md) for evidence and limits.
Live-cookie/saved-password continuity, real providers and replacement-VPS
recovery are not accepted by that drill.

- VPS SQL/key/config encrypted backups and an isolated SQL restore now pass;
  see [vps-backups.md](vps-backups.md). Full replacement-host app recovery and
  independent backup alerts remain open. The daily external-drive task
  and weekly Codex alert checks depend on the owner's PC.
- Owner configured independent UptimeRobot readiness monitoring; see
  [uptime-monitoring.md](uptime-monitoring.md) for real outage/recovery evidence
  and extra monitor cleanup. Worker-progress monitoring remains separate.
- Sustained load, backup-time memory/disk headroom, worker failure and public-host
  recovery/rollback drills. Current idle/light measurements do not establish scale.
- Authenticated public-browser/account-isolation acceptance, real-device push,
  physical PWA installation/upgrades, screen-reader checks and two normal care cycles.
- Reviewed catalog, owner administrator bootstrap when needed, support/privacy,
  data deletion, release packaging and exact-candidate CI/release decisions.

STAGE-01 now has a functioning public deployment and partial evidence; complete
staging acceptance and OPS-01/UX-01/release gates remain open. Do not invite real
users or tag a production release based only on these checks.
