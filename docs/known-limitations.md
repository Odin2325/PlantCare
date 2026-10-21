# Current limitations

Reviewed 2026-10-21. These describe the staging beta, not a released v1.

| Area | Current status |
| --- | --- |
| Catalog | One synthetic demo species; reviewed real species and attributed care data are required before a useful public launch. |
| Images | Plant thumbnails and healthy growth-stage images are planned, not implemented. |
| Privacy/account tools | New Account controls support password-verified JSON export and active-service deletion. Backups/provider-held copies are separate; proposed 30-day backup expiry and restored-account suppression are not enforced yet. The full privacy notice remains a draft. See release/account-data.md for deployment status. |
| Offline use | Offline explanation and reconnect supported; no offline care writes or full offline collection. |
| Devices | Owner reported successful Android 17 / Chrome 155 installation, reminder handling, offline/reconnect, frontend upgrade and multiple care cycles. Other physical devices and assistive technologies have not received equivalent acceptance. Desktop/mobile-sized browser automation is separate evidence. |
| Calendar | Private feed URLs are bearer secrets. Real calendar-client import/revocation still needs acceptance. Google/Microsoft synchronization remains disabled. |
| Push | Permission/provider/network dependent; timely delivery is not guaranteed. Dashboard remains the place to check due care. |
| Operations | Backups depend on the owner's PC, external drive and, for VPS capture, SSH/network access. The chosen weekly backup alert check is not continuous or independent monitoring. |
| Recovery/capacity | Isolated restore and same-schema rollback are tested. Replacement-host recovery, migration downgrade and sustained authenticated/high-fanout capacity are not established. |
| Availability | Staging may be interrupted for maintenance; no service availability or support response commitment. |
| Licensing | Original project code/documentation is rights reserved; dependencies retain their own licenses. See LICENSE. |

See [release acceptance](release/v1-acceptance.md) for the evidence and remaining
gates, and [catalog roadmap](release/catalog-roadmap.md) for the content/image plan.
