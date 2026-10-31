# Catalog and Garden batch checkpoint

Deployed and accepted on staging on 2026-10-31 at
`6c84b8e1547e569e189f960af196233508172ee2`.
[Full candidate CI](https://github.com/Odin2325/PlantCare/actions/runs/41198159336)
passed. This is a staging checkpoint, not a v1 tag or production cutover.

## Delivered together

The accumulated batch adds Garden upcoming work, related-variety discovery,
exact-cultivar photographs and named pending placeholders, cultivar-photo filters,
direct care comparison, compared growing/harvest guidance and public comparison
links. The catalog has **257 reviewed entries / 115 cultivars / 193 photo assets**.
Of the cultivars, **38 have distinct reviewed photos and 77 remain pending**.
Generic species photographs are no longer presented as exact cultivar photos.
The existing twelve growth galleries and twenty-four reference examples remain.

## Acceptance evidence

- One full CI run covered backend/frontend suites, conservative CI selection,
  retention safety, asset integrity, production containers, real SQL grants and
  account/session isolation, push failures/retries, browser accessibility and PWA.
- Exported committed source only, checked bundle hashes, pinned runtime images and
  compatible erasure/snooze protocols, and verified restricted runtime access.
- A fresh encrypted backup of the old runtime preceded activation. There is no
  schema change relative to the existing successful recovery baseline. This does
  not establish replacement-host recovery.
- All 231 preceding public IDs and their non-profile fields were preserved. All
  257 reviewed profiles match the committed dataset; all 193 image hashes match
  over trusted HTTPS. Intended profile photo/notes updates are included.
- The approved synthetic account's existing service-worker session updated and
  retained its plant IDs and completed history. Offline care icons, public route
  navigation and logout passed without sending test email or push messages.
- Real public API/browser acceptance covered the 38-photo and 77-pending filters,
  Chioggia's distinct image, related comparison, per-plant growing/harvest notes,
  public-only sharing URL, accessibility and 320-pixel reflow. Phone/desktop
  screenshots were inspected. Physical Android clipboard permissions remain
  untested; mocked cross-browser checks do not substitute for that acceptance.
- A fresh deployed-candidate encrypted VPS backup and repository integrity check
  passed. Both default local/VPS backup-health checks were healthy afterward.

## Availability incident during preparation

Before activation, SSH/HTTPS became intermittent and external monitoring reported
connection timeouts. The owner found KVM worked in Edge while Firefox failed to
load it. The initial VPS backup retry failed; the existing notification worker
was later found stopped. With no remote capture active, the old worker was
restarted and readiness/operations recovered before backup and deployment resumed.
Fresh pre/post backups subsequently succeeded. No firewall or SSH security policy
was weakened. The control-panel login email does not establish a VPS access block.

The precise cause of the connection disruption is unconfirmed. Failed capture
recovery should receive an operations follow-up so worker resumption is verified
after interrupted access. This incident is not evidence of independent automatic
recovery. PC, drive, agent and network dependencies remain.

## Next work

The delivery checkpoint in [the feature roadmap](feature-roadmap.md) is complete.
Saved plant shortlists are the next feature; a private photo journal needs its
storage/privacy/deletion design before uploads. Continue exact-cultivar sourcing
in local batches, with targeted checks and a later combined checkpoint.

Acceptance documentation is committed locally without another pipeline or runtime
deployment solely to publish these notes. Deferred public-release decisions remain
open in [current milestones](current-milestones.md).
