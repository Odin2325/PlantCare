# Planner and catalog staging checkpoint

Activated on 2026-11-10 on the existing OVH staging VPS. Runtime candidate
`065c57bb2a264ac22e9faf1ad6a1f7bb88dc7e53` supersedes
`d8ff22b2fc66e4444032796015cd58a1deade5a8`. This is a staging update. The
application runtime diff contains no new SQL schema migrations.

## Scope and evidence

- The planner now covers named layouts, light zones, room boundaries,
  obstacles, pot arrangements, shared-pot care checks, and placement ideas
  for owned or unowned catalog plants. Layouts are browser-local and scoped
  to the signed-in account.
- Staging contains 316 reviewed English profiles and one legacy entry.
  There are 167 named cultivars, 50 verified exact-cultivar photos,
  117 cultivar photos pending, and 212 licensed assets. All 212 public image
  URLs returned the expected type and size. Three sample downloads matched
  their source manifest hashes.
- [Full CI for the exact candidate](https://github.com/Odin2325/PlantCare/actions/runs/42710206159)
  passed. The added 320px planner browser journey passed on desktop Chromium,
  mobile Chromium, and Firefox, including an accessibility scan and a reload
  with no collection API writes.
- A fresh encrypted VPS backup passed repository verification and was
  restored locally against the candidate. The isolated drill verified SQL,
  keys, account-erasure reconciliation, restricted runtime access, session
  and care data, and recovery from a deliberately failed activation.
- The public PWA shell and its manifest agree on the new planner assets.
  A signed-in 390px staging browser accepted the existing-worker update,
  kept its session, collection and completed history, planned an unowned
  catalog plant in a matching zone, and found the pot after reload. The
  owned-plant path is covered by the local browser fixture. Physical-device
  acceptance remains separate.
- Photo journal activation remains a separate release gate.

## Backup overlap and recovery

A backup started after the initial activation and restarted the API and
worker using the previous configured candidate. Container events identified
the replacement and the public shell confirmed it. The retry held the backup
lock through the deployment handoff. A local SSH client hung after the remote
activation completed. Remote job exit codes, running image tags, and public
health verified completion before the local handoff resumed. The backup
pointer was advanced while holding the lock, and the signed-in browser
verified the stable retry.

The backup capture now checks the configured candidate against both running
image tags before it stops any service. An isolated negative check rejected
a stale candidate without creating capture scratch or calling a mutating
Docker command. Future cutovers should keep the local backup lock from the
runtime switch through the backup pointer update. See the
[VPS backup runbook](vps-backups.md).

A fresh post-deployment encrypted capture passed repository and backup-health
checks. Both candidate containers were still active after capture, and public
readiness and operations returned HTTP 200.
