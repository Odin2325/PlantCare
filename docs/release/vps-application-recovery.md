# Isolated VPS application recovery and activation rollback

`scripts/Test-VpsApplicationRecovery.ps1` restores the latest recorded encrypted
VPS snapshot into a new local Docker project. It never deploys to the VPS or
overwrites source data. Use the existing private backup configuration, unlocked
restic key and Docker Desktop; matching prebuilt API/worker image IDs and a local
Caddy and Mailpit images must be available. The saved synthetic account address
must identify a confirmed account in the snapshot. Do not paste credentials,
reset links or cookies into chat or logs.

```powershell
pwsh -NoProfile -File scripts/Test-VpsApplicationRecovery.ps1
```

To test a new exported runtime and its actual Compose configuration against the
existing encrypted data before VPS activation, add `-RuntimeCandidate <40-char-sha>`.
The tool checks allowlisted payload hashes and image IDs, records snapshot and
tested runtime candidates separately, and keeps all networks internal with no
published ports. This compatibility mode also checks the operations endpoint.
It passed for `d3d81f30cc2a6bbd06d92cb2781d7b5e40a3924d` on 2026-10-14.

## Verified result (2026-10-14)

The drill passed against the encrypted snapshot of runtime candidate
`0acdb2db70a343d1aae977bf4bb19150deaaaf7a`: decryption/all-file hashes,
CHECKDB/all-table comparisons, key restoration, runtime login remapping,
internal-mail password recovery and reset-code reuse rejection, fresh login,
worker reminder/restart deduplication, care completion/history and logout.

The deliberately failed API activation exited 42 and was rejected. Reapplying
the pinned original runtime recovered readiness, preserved the authenticated
session/care history and retained identical key hashes. The disposable project,
its volumes/network and private scratch were removed after the successful run.
No backup snapshot or source application data was restored over or deleted.

During development the isolated HTTP client/configuration needed correction;
the final successful run kept every network internal and all ports unpublished.
A proposed non-internal frontend network was rejected by automatic approval
review and was not executed. The internal client removed that requirement.

## Isolation and procedure

- The exclusive backup lock serializes the drill with scheduled local/VPS
  backups. Restored secret files live only in restricted private scratch.
- Restic decryption/verification and all manifest SHA-256 hashes are checked.
  SQL uses the captured official image digest; API/worker use their captured
  image IDs. The locally installed Caddy image is pinned by ID for the drill;
  it is verification infrastructure rather than proof of an identical VPS proxy.
- Every service uses a Docker **internal** network, with no published ports.
  A disposable credential-free curl client exercises HTTPS inside that network.
  Only the drill's local Caddy certificate verification is bypassed; public
  staging HTTPS verification is not disabled. No DNS/firewall changes are made.
- External calendar providers/sync are disabled. SMTP points only to an internal
  Mailpit capture service with blanked username/password. Network isolation
  blocks external push traffic. Password-recovery mail is captured inside the
  disposable environment; no messages are sent to the real owner or providers.
- SQL is restored into an empty instance without REPLACE. CHECKDB and all-table
  count/checksum comparisons run before application writes. Keys are extracted
  from the verified archive, permissions restored and fresh master logins mapped
  to the restored restricted database users. No schema migration runs.
- The saved test password was rejected, so the snapshot's copied synthetic
  account uses standard password recovery through internal captured mail.
  The reset link is validated, consumed and rejected on reuse. A fresh random
  password signs in through production Identity and antiforgery. The restored
  worker generates one overdue reminder, retains it
  across restart without duplication, and care completion/history are exercised.
  Polling is accelerated to five seconds only in the disposable copy.
- A failing API activation uses an explicit exit-42 entrypoint. Compose must
  reject startup and the actual exit code must be 42. Reapplying the original
  pinned configuration recreates the API; readiness, image identity, the existing
  cookie session, care history and unchanged key hashes are checked. Logout must
  then reject the old session.
- Only the randomly named drill project/volumes and its validated scratch path
  are removed. Encrypted snapshots and live staging are preserved. Non-secret
  results are kept privately in `.operations-local/vps-last-app-recovery.json`.

## Acceptance limits

Follow-up 2026-10-24: [reminder snoozing](reminder-snoozing.md) adds a runtime
compatibility protocol. Snapshot manifests record its required version; managed
app recovery rejects a bundle below that version. Activation blocks downgrades,
and a failed first upgrade from a snooze-unaware worker stops writers instead of
reapplying that old runtime. Use compatible forward recovery. The snooze-aware
candidate's isolated restore and failed-activation recovery passed before staging
activation; this does not establish migration downgrade or replacement-host recovery.

This is recovery from the real encrypted VPS snapshot onto a separate local
Docker instance. It is not a complete replacement-VPS exercise or proof that
credentials can be recovered after losing this PC/DPAPI profile. Keep the
portable offline recovery-key and replacement-host procedures open.

The rollback probe tests a failed activation with the **same schema**. It does
not validate downgrading a migrated database or arbitrary older application
images. Before a real upgrade, record old/new image identities and schema
compatibility; use tested database recovery if rollback cannot safely reuse the
new schema. Never run down-migrations or restore over the live database as a probe.

An initial attempt to obtain a live staging cookie returned 401 with the saved
test credentials; no reset was sent and no further live login was attempted.
Existing live-cookie and saved-password recovery are therefore not accepted here.
The final drill resets only the snapshot's copied account and validates its new
cookie across API replacement. The original saved password file and live account
are preserved. Real OAuth/provider recovery and email/push delivery remain untested
from restored configuration. This drill also does not establish VPS capacity.

## Planting-plan migration follow-up

The [planting-plan candidate](planting-plan.md) passed an additional isolated
restore on 2026-10-25. Its nullable date columns were migrated into the restored
copy; authenticated save/reload and preservation after rejected API activation
passed without changing care history. The capability probe remains optional for
older runtimes. Live source data was unchanged; replacement-host and independent
erasure-authority limits above remain open.

## Harvest-record capability acceptance (2026-10-26)

The isolated drill for `2a50bb1f1eba52e6d7f2db0a2acf28d01ac2b9ed` passed forward migration to the new harvest table, create/read and private export under the restricted runtime, unchanged care, and harvest/planting-plan preservation after a rejected API activation. The candidate is matched through committed source capability inspection. This does not accept replacement-host recovery or old-runtime export of newer harvest data.

## 2026-10-30 seasonal preference migration

Exact candidate `3d3237745496b1e85ff20ee3bd2e14d86ca651ca` passed isolated application recovery from a fresh
encrypted VPS snapshot. Fresh live erasure authority was applied before target
start; the additive UserPlants migration, runtime login remapping and internal
mail/password recovery passed. The restored target saved and exported seasonal
preferences and preserved them through an intentionally rejected API activation
and reapplication of the pinned runtime. Planting dates, harvests, worker restart
deduplication, care history, session/key hashes and logout checks also passed.
Source data was not restored or overwritten. Replacement-VPS and independent
erasure-authority acceptance remain separate requirements.
