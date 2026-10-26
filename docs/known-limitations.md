# Current limitations

Reviewed 2026-10-26. These describe the staging beta, not a released v1.

| Area | Current status |
| --- | --- |
| Catalog | 180 reviewed English entries, including 38 cultivars, are deployed on staging, with 262 alternative names across 131 entries; see [harvest-record acceptance](release/harvest-records.md). This is a curated selection, not a complete botanical database. The Growth references filter finds the twelve species with galleries and combines with the other filters. Common names are not unique species identifiers. The legacy demo remains to preserve existing plant references. Suggested care intervals need adjustment for the actual plant and conditions. |
| Images | All 180 reviewed entries have credited reference photos, drawn from 156 licensed originals. Cultivars using a shared species photo are explicitly marked unverified. Twenty-four curated growth references cover twelve species and show different specimens without timelines or diagnosis. Personal photo journals are not implemented. |
| Garden/harvests | Garden shows saved active crops and target dates, without weather-based forecasts or automatic care completion. Personal harvest logs display the latest 100 records; older records remain in private export. Different quantity units are not summed. Archived plants are read-only until restored. |
| Pet classification | The non-toxic cat-and-dog filter requires an explicit reviewed source and currently covers thirteen entries. Missing warnings are not proof of safety. Other animals are outside this classification; plant material can still cause stomach upset if eaten. |
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
