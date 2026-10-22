# Current limitations

Reviewed 2026-10-22. These describe the staging beta, not a released v1.

| Area | Current status |
| --- | --- |
| Catalog | 20 reviewed English houseplant entries with linked care references; a starter selection, not a complete botanical database. Eight species have reviewed alternative common names for search; these names are not unique species identifiers. The legacy demo entry remains to preserve existing plant references. Suggested care intervals need adjustment for the actual plant and conditions. |
| Images | All 20 reviewed entries have local licensed reference photos with visible credits. Monstera, spider plant, golden pothos and aloe vera have curated growth-feature examples of different specimens, without timelines or diagnosis. Personal photo journals are not implemented. |
| Privacy/account tools | Account supports password-verified export/deletion. The follow-up adds guarded 30-day expiry and current-authority reconciliation; see release/retention-and-erasure-recovery.md for enabling status. Offline storage can delay cleanup; missing current authority blocks managed app recovery. The full privacy notice remains a draft. |
| Offline use | Offline explanation and reconnect supported; no offline care writes or full offline collection. |
| Devices | Owner reported successful Android 17 / Chrome 155 installation, reminder handling, offline/reconnect, frontend upgrade and multiple care cycles. Other physical devices and assistive technologies have not received equivalent acceptance. Desktop/mobile-sized browser automation is separate evidence. |
| Calendar | Private feed URLs are bearer secrets. Real calendar-client import/revocation still needs acceptance. Google/Microsoft synchronization remains disabled. |
| Push | Permission/provider/network dependent; timely delivery is not guaranteed. Owner confirmed the correct plant photo and Dashboard tap destination on a newly received reminder in the installed Android app. Other device layouts and image-failure rendering remain unverified. Missing/invalid metadata uses the app icon; network image failure has no automatic picture replacement. The OS controls its separate status-bar badge. Dashboard remains the place to check due care. |
| Operations | Backups depend on the owner's PC, external drive and, for VPS capture, SSH/network access. The chosen weekly backup alert check is not continuous or independent monitoring. |
| Recovery/capacity | Isolated restore and same-schema rollback are tested. Replacement-host recovery, migration downgrade and sustained authenticated/high-fanout capacity are not established. |
| Availability | Staging may be interrupted for maintenance; no service availability or support response commitment. |
| Licensing | Original project code/documentation is rights reserved; dependencies and reference photos retain their own licenses. See LICENSE and THIRD_PARTY_NOTICES.md. |

See [release acceptance](release/v1-acceptance.md) for the evidence and remaining
gates, and [catalog roadmap](release/catalog-roadmap.md) for the content/image plan.
