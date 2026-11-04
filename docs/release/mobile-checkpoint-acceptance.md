# Mobile staging checkpoint acceptance

Candidate `d8ff22b2fc66e4444032796015cd58a1deade5a8` was activated on
2026-11-04 at 16:14 Berlin time. This is a staging update, not a public release
or production cutover.

## Scope

The accumulated batch includes account-synced shortlists, additive shortlist and
photo-journal schema, and the [shared visual refresh](visual-refresh.md) and
[mobile-first layout](mobile-first-layout.md). Phones use bottom navigation;
tablets use a rail. Catalog search remains visible while advanced filters expand
on demand. The compact month calendar opens a readable selected-day agenda.
Reminder settings are collapsed initially and reminder history is paginated.

The reviewed catalog remains 257 entries, including 115 named cultivars; the
existing legacy entry remains intact. No new catalog batch is part of this UI
checkpoint. Both photo-processing and photo-upload flags remain disabled. The
separate decoder/gateway is not installed or activated by this deployment.

## Accepted evidence

- [Full exact-candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/41816795736)
  passed Backend, Frontend and Production container jobs. Earlier failed browser
  journeys were corrected to open real disclosures and wait for loaded controls;
  their fixes are included in this candidate. No test scope or budget was relaxed.
- A fresh encrypted VPS capture was restored into isolated local containers
  before activation. Database integrity/table comparisons, key hashes, fresh
  erasure authority, target migrations, restricted runtime access, private
  shortlist persistence/export, disabled journal capability, password recovery
  through internal Mailpit, worker restart deduplication and care history passed.
  Deliberately failed API activation was rejected; pinned recovery preserved the
  synthetic session, history and keys. External email/push was not used by this
  isolated drill.
- Transferred bundle checksums and actual API/worker image identities passed.
  Additive migrations, runtime grants and the repeatable reviewed catalog upsert
  completed. Public readiness/operations and restricted API/worker access passed.
- A real staging browser session spanning the old and new service workers used
  Update now, retained authentication, personal plant IDs and completed history,
  checked offline action-icon caching, all 257 reviewed entries, catalog images,
  Garden/comparison/filter routes and logout.
- Live Chromium checks at 390x844 and 768x1024 passed navigation, collapsed and
  expanded filters, no horizontal overflow, compact month/day agenda, collapsed
  reminder settings, and saved shortlist reload. The temporary shortlist addition
  was removed through the authenticated API and its original set was verified.
  Screenshots of catalog, calendar, reminders and tablet layout were inspected.
- After activation, the existing scheduled VPS backup task captured the migrated
  database successfully and finished with result 0. That new encrypted snapshot
  passed the full isolated application recovery drill, including shortlist/journal
  reads, runtime grants, worker/history checks and failed-activation recovery.
  Local and VPS backup health checks reported no problems; both scheduled tasks
  last finished with result 0. Final public readiness and operations returned
  Healthy/HTTP 200 with API and worker pinned to the deployed candidate.

## Operational interruption and limits

OVH reported a backup operation in progress while SSH/HTTPS were unavailable or
intermittent. Activation was held, with no completed runtime switch during that
outage. After the provider state returned to Active, the old image and healthy
endpoints were verified and fresh backup/recovery gates were repeated before
activation. No abortSnapshot call, guest-agent disable, host reboot, reinstall or
older-data restore was performed for this checkpoint.

This records browser acceptance, not a new physical Android/tablet acceptance.
The owner should accept Update now after saving drafts and review the installed
app's catalog filters, month/day agenda and reminder controls. Provider-side
backup reliability and replacement-host recovery remain separate follow-ups.
Photo uploads retain their own capacity, isolation, recovery and device gates.
