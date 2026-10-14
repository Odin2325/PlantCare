# Push retry and queue policy

## Notification tap correction (2026-10-14)

Follow-up: the owner reports installed notification taps now open the app, but
land on the catalog. New reminder payloads now target `/dashboard`, where care
actions are available. The regression checks that destination. The previously
deployed `/notifications` payload and old displayed notifications are historical;
the requested dashboard destination still needs a fresh physical-device retest
after its deployment. App opening is user-reported, not independently observed.
The tap operation now uses Angular's `openWindow` with an explicit dashboard
destination. This delegates launching to the browser, including installed-app
handling, instead of choosing the last focused client. Session initialization
already completes before routing, so no authentication guard was weakened.
Chrome Android may reuse an installed standalone app for
[`Clients.openWindow`](https://developer.mozilla.org/en-US/docs/Web/API/Clients/openWindow).
This is the next behavior to verify physically, not proof of the observed
catalog landing's platform-level cause.

Physical Android 17 / Chrome 155.0.8059.79 testing reported successful
installation and app launch, but installed-app notification taps did nothing.
Browser-tab push receipt/tapping had previously been reported as working.
The worker serialized body/icon/tag/data inside `notification.options`;
Angular reads these fields directly from `notification`, so it discarded the
tap action as well as the body and deduplication tag. The corrected payload
places them alongside the title. Its default action navigates/focuses an open
client or opens `/notifications` when no client exists, following
[Angular's payload contract](https://angular.dev/ecosystem/service-workers/push-notifications).

The regression captures the actual delivery-service payload before passing it
through the real encryption transport and verifies the click action, route,
body and stable tag. Existing displayed/provider-queued notifications keep their
old payload; retest with a newly generated reminder after deployment. No
subscription replacement, permission reset or reinstall is required by this fix.
Physical taps with the installed app both backgrounded and fully closed remain
pending until the owner confirms the corrected behavior. Headless tests cannot
certify Android's app-launch behavior.

Candidate `7c78c9426aa423e32b980a6f195434840612e384` is active on staging.
All 126 backend tests and 25 isolated SQL push tests passed. Exact-candidate
[CI run 38595333804](https://github.com/Odin2325/PlantCare/actions/runs/38595333804)
passed on attempt 2: attempt 1 had a Firefox login-page axe scan timeout with
14 other browser checks passing; the identical failed scenario passed locally
in 10 seconds, and the job retry passed without source/assertion changes.
An additional local probe exercised the real Angular push/click handlers and
observed navigation, with only the headless notification display stubbed; this
does not validate native notification display or physical focus.

A fresh encrypted pre-update backup and isolated new-runtime application
restore/rollback drill passed. Activation's local SSH wrapper stalled after its
remote process ended; it was closed, and exact active image IDs, API/worker
restricted database access and public readiness/operations health were verified
independently before updating the private backup candidate pointer. No migration
or credential change was needed. Android notification-tap acceptance remains open.

`WebPushDeliveryService` now keeps a durable outcome for each notification/device
pair in `PushDeliveries`. A notification's target devices are snapshotted before
its first network attempt. Newly registered devices receive future reminders;
they do not cause an old notification to replay. Devices removed before retry
are skipped. The ledger contains IDs/status/timestamps, not endpoints or keys.

Each accepted, expired, failed or skipped device is terminal for that reminder.
Results are saved before attempting another device, so a later partial failure
or an ordinary worker restart does not resend accepted devices.

The delivery policy is:

- 2xx means the provider accepted the message, not that a browser displayed it.
- 404/410 removes the expired subscription. Other permanent HTTP errors,
  including redirects, are terminal for that device/reminder and retain the
  subscription. Redirects are not followed.
- 408/429/5xx, network failures and transport timeouts retry at most five times.
  Waits after attempts 1–4 are 1, 5, 15 and 60 minutes. A longer `Retry-After`
  delta/date is honoured, capped at 24 hours. Malformed argument/format errors
  are terminal; other unexpected transport failures also have the five-attempt cap.
- Requests have a 15-second timeout. Each notification gets at most 16 sends
  per pass; a pass yields after roughly one minute plus any in-flight request
  and database work. Unattempted devices defer for a minute so other users can
  progress. Deferred notifications do not occupy the oldest eligible batch slots.
- Before each retry pass, completed/rescheduled, paused, archived, push-disabled,
  action-disabled, or more-than-24-hour-old reminders are suppressed. In-app
  history is retained. Caller cancellation propagates without counting it as a
  provider error.
- A stable browser notification tag reduces duplicate visible cards. Provider
  retention TTL is one hour instead of the library's default four weeks.
- Unconfigured push leaves the queue untouched; it does not simulate acceptance.

`Notifications.PushSentAtUtc` retains its legacy name but means push processing
has finished, including skipped/expired/failed devices. Use ledger status to
distinguish outcomes. Failure logs contain notification/device IDs, status code,
attempt count or exception type. Provider exception messages, endpoint URLs and
key material are withheld; HTTP client URL logging is disabled for this transport.

## Execution and migration

```powershell
dotnet test PlantCare.slnx --configuration Release
./scripts/Start-LocalDeployment.ps1
./scripts/Test-LocalPushDelivery.ps1
```

The SQL script requires PowerShell 7 and .NET 10 SDK. It reads the private local
Docker password without printing it, creates generated `PlantCarePushTests_*`
databases only on `127.0.0.1,14330`, runs all migrations, then removes only its
generated databases. It never targets Windows SQL Server or the main PlantCare
database. The backend suite runs the same tests against SQLite by default.
CI executes both providers and retains its existing Compose cleanup.

Fault-injection tests use generated ephemeral encryption/VAPID keys and a local
`HttpMessageHandler`. They exercise the real WebPush request encryption and HTTP
adapter without contacting a real push provider. The [WebPush library](https://github.com/web-push-libs/web-push-csharp#api-reference)
generates the encrypted requests; provider outcomes are injected by the tests.
Coverage includes mixed success,
404/410, repeated 5xx, fairness with batch size one/large fanout, 4xx/redirects,
both Retry-After forms, network/timeout failures, disabled push, removed devices,
stale/preference suppression, cancellation, and an interrupted pass after one
accepted device followed by a fresh worker scope.

Migration `20261013110834_AddPushDeliveryRetries` adds a nullable next-attempt
column/index to Notifications and the delivery table. Existing notifications,
users and care history remain intact. Previously completed push rows remain
completed. Previously pending rows get their device snapshot on first processing;
historic device-level successes cannot be reconstructed. The snapshot was
regenerated with EF tooling; schema changes are additive.

Back up SQL plus data-protection keys before public-host upgrades and deploy API
migrations before starting the new worker. Stop the worker during rollback:
the old worker ignores the ledger and can replay partially delivered reminders.
Do not drop the ledger to clear a backlog. Follow [recovery.md](recovery.md).

## Operational limits and remaining acceptance

Run one worker replica, as documented in Compose. Cross-process queue leasing is
not implemented. There remains an unavoidable acceptance/save crash window:
if the provider accepts a message and the process dies before the outcome is
committed, retry can duplicate that send. Delivery is not exactly-once. A browser
may also receive a provider-queued message after care state changes; the one-hour
TTL bounds provider retention but cannot recall an accepted message.

Inspect terminal failures without exposing endpoint secrets:

```sql
SELECT Status, COUNT_BIG(*) AS Deliveries
FROM dbo.PushDeliveries GROUP BY Status;
-- 0 Pending, 1 Accepted, 2 Expired, 3 Failed, 4 Skipped
SELECT COUNT_BIG(*) AS DeferredNotifications
FROM dbo.Notifications
WHERE PushSentAtUtc IS NULL AND PushNextAttemptAtUtc IS NOT NULL;
```

Alerting, retention policy (ledger rows follow notification deletion), and a
manual investigation procedure belong to OPS-01. Real provider credentials,
permission denial, supported physical devices and actual browser delivery still
need staging acceptance. This step validates failure handling, not real push
delivery or the whole v1 reminder gate.
