# Photo host preparation for the next batch

This is a reviewable installation/preflight sequence, not an executed install.
Staging uploads remain disabled, the gateway unit is not installed by these
changes, and live memory/recovery settings are unchanged. No additional provider
or store service is required for preparation.

## Inputs and boundaries

Use the exact committed API/worker bundle, the independently
[verified decoder bundle](photo-processing-bundle.md), its trusted manifest hash
and the candidate's notices. Rebuild/recheck those exact artifacts at the combined
checkpoint rather than treating the local samples as future-candidate acceptance.
The test gateway Dockerfile is acceptance infrastructure, not a host deployment
image. Review the actual host Python and Docker CLI/package versions separately.

Place reviewed gateway source under the fixed /opt/plantcare/scripts location
and a reviewed unit under systemd control, root-owned and not writable by the API
or the unprivileged deployment user. The unit must bind the accepted immutable
decoder image ID, not a mutable tag. The existing preparation template runs as
root with primary GID 1654, creates a private root:1654 mode-0750 runtime directory
and mode-0660 socket, uses only AF_UNIX, and has Restart=no for uncertain cleanup.

The host gateway is trusted Docker control-plane code. Only it receives Docker
access. The API receives only the read-only socket-directory mount from the
disabled override example; it receives no daemon socket, root user, new capability
or native codec. Never make the directory/socket world-writable to bypass a
mapping problem.

## Read-only preflight before activation

1. Verify transferred hashes through the trusted manifest digest. Inspect loaded
   API/worker/decoder image IDs, Linux amd64 platform and non-root API user.
   Preserve vendor notices and finish the remaining native/container review.
2. Verify host ownership/modes for source, unit, runtime directory and socket.
   Inspect actual API UID/GID and host-observed peer credentials, including any
   rootless/user-namespace mapping. Local 1654:1654 acceptance does not certify a
   different host mapping. Verify the API's socket mount is read-only and no
   Docker socket appears in its mounts.
3. Validate the pinned unit with the actual host systemd tools before enabling.
   Refuse an unknown pre-existing socket or decoder job. Do not blindly unlink
   sockets, remove unrelated containers or auto-restart a poisoned gateway.
4. Review/apply a tested resource budget at the batch maintenance window. The
   [budget example and capacity record](photo-capacity.md) leave limited host
   headroom; live containers currently have no cgroup ceilings. Include worker,
   browser-sized uploads, largest exports, decoder and backup/restore together.
   The gateway's 128-MiB planning envelope is not automatically enforced by the
   current unit template. Validate any chosen host limit and its failure behavior.
5. Rehearse graceful termination, forced gateway loss/limit failure, decoder crash,
   cancellation and cleanup before service restart. A killed control-plane process
   must not permit overlapping or forgotten jobs. Confirm no owned decoder remains
   and restored acceptance state is not inferred from a CLI's exit alone.
   The [local restart drill](photo-gateway-restarts.md) now passes graceful and
   forced-loss cases. Install protocol-1 gateway state and preserve its runtime
   directory across service stops; `/run` is still lost on host reboot. Actual
   systemd/reboot and limit-failure behavior must be checked on the target host.
6. Review the [snapshot-only option](snapshot-recovery-policy.md) or implement the
   alternative FULL/log-backup policy. Do not silently change recovery models,
   retire an existing chain, alter schedules or shrink files during installation.
7. Capture the installed unit/source hashes, decoder identity, actual resource and
   recovery settings in reviewed deployment/backup metadata. Existing SQL/key/app
   capture does not by itself certify recovery of newly installed host software.
   Test re-provisioning that control plane alongside erasure-safe application
   recovery before claiming replacement-host support.

## Acceptance order

Keep both flags off while installing/checking prerequisites at the batch window.
After full exact-candidate CI, fresh migrated encrypted recovery, host mapping,
resource and failure acceptance, test the real HTTP -> gateway -> decoder -> SQL
path in a controlled staging account. Then accept installed Android
upload/view/delete, metadata handling, account export/erasure and existing-session
update with personal data intact. Apple hardware acceptance remains pending until
a device is available. Store packaging/betas remain separate milestones.

These are the remaining photo-activation gates, not a reason to deploy each small
local change. Further feature/catalog work can continue while the batch grows.
