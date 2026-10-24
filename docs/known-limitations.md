# Current limitations

Reviewed 2026-10-24. These describe the staging beta, not a released v1.

| Area | Current status |
| --- | --- |
| Catalog | 58 reviewed English entries are deployed on staging, with 92 alternative names across 46 entries; see release/plant-comparison.md for verification. This is a curated selection, not a complete botanical database. Common names are not unique species identifiers. The legacy demo remains to preserve existing plant references. Suggested care intervals need adjustment for the actual plant and conditions. |
| Images | All 58 reviewed dataset entries have local licensed photos with visible credits. Monstera, spider plant, golden pothos and aloe vera have curated growth-feature examples of different specimens, without timelines or diagnosis. Personal photo journals are not implemented. |
| Pet classification | The non-toxic cat-and-dog filter requires an explicit reviewed source and currently covers twelve entries. Missing warnings are not proof of safety. Other animals are outside this classification; plant material can still cause stomach upset if eaten. |
| Comparison | Up to three public catalog plants, with selection in the URL. It compares curated guidance without assessing a user's room or diagnosing a plant. It does not share personal collection data or store a server-side wishlist. |
| Snoozing | Current reminders support elapsed delays of 1 hour, 24 hours or 3 days. Care remains due until completed; snoozing does not write history or reschedule care. Saved state, SQL worker behavior and live forms are verified; delayed follow-up delivery on an installed physical phone remains open. |
| Privacy/account tools | Account supports password-verified export/deletion. The follow-up adds guarded 30-day expiry and current-authority reconciliation; see release/retention-and-erasure-recovery.md for enabling status. Offline storage can delay cleanup; missing current authority blocks managed app recovery. The full privacy notice remains a draft. |
| Offline use | Offline explanation and reconnect supported; no offline care writes or full offline collection. |
| Devices | Owner reported successful Android 17 / Chrome 155 installation, reminder handling, offline/reconnect, frontend upgrade and multiple care cycles. Other physical devices and assistive technologies have not received equivalent acceptance. Desktop/mobile-sized browser automation is separate evidence. |
| Calendar | Email sharing supports registered users and seven-day signup invitations, accepted only by the matching confirmed account. SMTP acceptance does not guarantee inbox placement; owners can resend. Invitations do not add events to Google Calendar. Private feed URLs are bearer secrets. Real calendar-client import/revocation still needs acceptance. Google/Microsoft synchronization remains disabled. |
| Push | Permission/provider/network dependent; timely delivery is not guaranteed. New reminders use static cartoon care-action icons; unknown actions use the app icon. Physical-device acceptance of the new drawings is pending. The OS controls its separate status-bar badge. Dashboard remains the tap destination and the place to check due care. |
| Operations | Backups depend on the owner's PC, external drive and, for VPS capture, SSH/network access. The chosen weekly backup alert check is not continuous or independent monitoring. |
| Recovery/capacity | Isolated restore and compatible-runtime rollback are tested. Snooze-aware snapshots require a compatible runtime; the snooze migration explicitly rejects downgrade. Replacement-host recovery and sustained authenticated/high-fanout capacity are not established. |
| Availability | Staging may be interrupted for maintenance; no service availability or support response commitment. |
| Licensing | Original project code/documentation is rights reserved; dependencies and reference photos retain their own licenses. See LICENSE and THIRD_PARTY_NOTICES.md. |

See [release acceptance](release/v1-acceptance.md) for the evidence and remaining
gates, and [catalog roadmap](release/catalog-roadmap.md) for the content/image plan.
