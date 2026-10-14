# Independent staging uptime alerts

The owner confirmed on 2026-10-14 that their UptimeRobot monitor checks
`https://staging.plantacare.org/health/ready` and has their email alert contact
enabled. Provider dashboard access was unavailable through the browser tool;
these settings are owner-confirmed rather than independently inspected.

Both Down and Up events must be enabled on an active contact attached to this
monitor. Creating an account/contact alone does not establish alert delivery.
See the [provider notification documentation](https://help.uptimerobot.com/en/articles/11360978-understanding-notification-channels-in-uptimerobot).

## Controlled staging drill

`scripts/Test-StagingUptimeAlerts.ps1` is fixed to the synthetic staging VPS and
shares the backup lock. It requires initial public readiness 200, arms and
verifies a separate twelve-minute systemd recovery timer, then stops only the
API for six minutes by default. SQL, data volumes, keys, configuration, proxy
and firewall are unchanged. No DNS changes are needed. The worker remains running.

The script starts the API in `finally`, waits for readiness 200, and cancels its
own timer only after recovery succeeds. If the controlling PC/session disconnects,
the server timer still attempts recovery. A timer cannot guarantee recovery from
an unrelated broken application, host outage or database failure.

Run only during an agreed synthetic-staging test window; it deliberately causes
an outage and may send the monitor's configured notifications:

```powershell
pwsh -NoProfile -File scripts/Test-StagingUptimeAlerts.ps1
```

The bounded option `-OutageMinutes` accepts six through ten minutes. The monitor
interval, confirmation retries and any notification delay affect alert timing.
The private `.operations-local/uptime-drill.json` records server-test status;
HTTP recovery alone does not prove email delivery. Provider emails with a TEST
subject prove only test-message delivery and are excluded from incident evidence.

The actual readiness-monitor DOWN message was received in the owner's Gmail
INBOX at 13:44:58 Berlin time on 2026-10-14. Its body identifies the exact
`/health/ready` URL and HTTP 502, matching the controlled outage. Extra alerts
also arrived for paths such as `/cart`, `/order`, `/wp-admin` and `/pricing`.
These appear to be unnecessary additional monitors; review and pause them in
the provider dashboard. Keep the readiness monitor. No provider configuration
was changed by this drill.

The API was restored and public readiness returned 200 at 13:47:01 Berlin time.
The real readiness-monitor UP message reached Gmail INBOX at 13:50:04 and also
identifies `/health/ready`. Thus real outage detection, Down email delivery,
API/readiness recovery and Up email delivery all passed. The server recovery
timer was cancelled after readiness recovered. This verifies the current
configuration for one controlled incident, not every failure mode.

## Limits and next acceptance work

The public endpoint checks API/database readiness, not authenticated workflows,
worker progress or backup freshness. Keep the weekly backup email monitor as a
separate, PC-dependent check. Worker/disk alerts and a complete isolated restored
application/login/data/key-ring drill remain open, followed by a tested
schema-compatible deployment rollback. Do not run external integration sends
from a restored copy of production/staging secrets.

The apex `plantacare.org` is reserved for release. Add a separate production
monitor when that deployment exists; preserve staging coverage.
