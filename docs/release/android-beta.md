# Physical Android beta

Target: installed PlantCare at `https://staging.plantacare.org`.
Current runtime: `82de1584291f7238e9e2fd5d6a677d0fa6808257` (Care details button styling).
The installed Android frontend upgrade was accepted on `dbf65b8`; the small
button-styling follow-up is verified in public desktop Chromium only.
The earlier phone checks below were reported on `cfbc073`; do not relabel them
as a full physical acceptance of the new calendar frontend.
Device versions supplied by the owner: Android 17, Chrome 155.0.8059.79.
Record results as owner-reported unless independently observed.

| Check | Result as of 2026-10-14 |
| --- | --- |
| Installation and app launch | Owner reported working |
| Notification opens installed app | Owner reported working on previous payload |
| Fresh notification opens Dashboard | Passed, owner-reported on cfbc073 |
| Offline cold launch and explanation | Passed, owner-reported on cfbc073 |
| Reconnect, sign-in if needed and fresh plant data | Passed, owner-reported on cfbc073 |
| Immediate care completion, history/calendar and next-due behavior | Owner reported working; elapsed-cycle evidence separate |
| Consented frontend update in the installed app | Passed, owner-reported cfbc073 → dbf65b8; nickname links and data intact |
| Two normal elapsed care cycles | Pending |

The calendar frontend deployment now allows the real phone update check from
`cfbc073` to `dbf65b8`. Return to the installed app online, save unfinished work,
choose Update now when offered, then check nickname/action links, the personal
plant page, collection/history and offline/reconnect. Desktop automation passed
this real old-to-new build upgrade. The owner subsequently confirmed Update now,
nickname/action links, personal-plant navigation and preserved plants/history.
The small follow-up Care details button styling is separate from that accepted
upgrade; preserve the tested candidate labels.

On 2026-10-14 the owner answered "All 3 passed" to the installed-app dashboard
tap, airplane-mode cold launch/offline message, and reconnect/account-data checks.
This accepts those three reported scenarios on the stated Android/Chrome versions;
it does not establish a frontend upgrade, offline write behavior, every supported
device, or either normal elapsed care cycle.
The subsequent care-flow confirmation is recorded as immediate functional
behavior. Both actual future daily due times have not been supplied, so the two
elapsed-cycle rows remain pending.

## Fresh reminder and dashboard action

Close PlantCare tabs in Chrome to distinguish the installed app from browser tabs.
Use a newly received reminder after the current deployment; earlier notifications
retain their original destination. Background the installed app or swipe it out of
the recent-app list, then tap the reminder. Do not use Android's Force stop for
this test, which can affect background delivery.

Expected: the installed app opens Dashboard, with the care action available.
If a reminder is needed immediately, create a temporary synthetic test plant
through the app, set watering to every one day, and set its initial last-watered
date two days in the past. Keep real plants' care schedules unchanged. Allow the
worker's normal 60-second poll and up to two intervals plus network delivery.
Provider acceptance alone does not prove display on the phone.

Complete the overdue action once. Reload and check that exactly one matching
history entry and completed calendar entry exist, with a new next-due time.
This overdue setup check is not one of the two normal elapsed care cycles below.
Report the destination and observed behavior without passwords or private links.

## Offline and reconnect

1. Open the installed app online first so its shell is installed and usable.
2. Turn on airplane mode and ensure Wi-Fi is also off. Close/reopen the app
   from its own home-screen icon.
3. Expect a usable shell and the message that care changes need a connection.
   A cold offline launch need not show account or plant data: APIs are not cached
   and care changes are not queued offline. A failed/offline action must not be
   presented as successfully saved.
4. Restore the connection and reopen/reload. Sign in if needed, then verify that
   the current collection, history and next-due time load correctly. Check that
   the offline message clears and no duplicate completion was created.

Suggested result report: `Dashboard tap: pass/fail; offline message: pass/fail;
reconnect and plant data: pass/fail`, with a short description of any failure.

## Installed frontend update

The current reminder destination change is server-side. It requires no reinstall
and does not itself produce an Update now prompt. Absence of that prompt is not
an update failure.

This check requires a separately built frontend version deployed while the phone
has the previous version installed. Record both candidates. Leave the old app
open, return to it online after deployment and wait for the update offer. Save
unfinished input, choose Update now, and verify navigation, account, collection
and history afterward. Reopen once more and repeat offline/reconnect. Preserve
the installation and storage during the upgrade check. Do not mark this accepted
from automated A/B shell fixtures or a server-only deployment.

## Two actual care cycles

After the immediate checks pass, use the temporary daily test plant above. Record
the displayed next-due time and saved time zone. Let that time actually arrive;
do not change the host/device clock or mark future care complete. Observe the
reminder, tap it, complete once, and verify history/calendar and the next due time
after reload. Repeat after the following real due time.

| Evidence | Cycle 1 | Cycle 2 |
| --- | --- | --- |
| Candidate, device/browser, actual due time and saved zone | Pending | Pending |
| Reminder arrival and dashboard destination | Pending | Pending |
| One completion and matching history/calendar entry | Pending | Pending |
| Correct next due time after reload | Pending | Pending |
| Duplicate/stale reminders or other defects | Pending | Pending |

Archive the temporary plant after the test. No recurring automation or changed
care/backup schedule is implied by this checklist. Keep the broader
[v1 acceptance gates](v1-acceptance.md) open until their own evidence is complete.
