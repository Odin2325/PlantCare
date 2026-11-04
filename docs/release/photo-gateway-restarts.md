# Photo gateway restart acceptance

Local preparation, 2026-11-04. Uploads remain disabled in normal environments.

## Behavior

The gateway now holds an exclusive, non-inherited lock in its private runtime
directory. Before asking Docker to create a decoder, it writes and syncs a
root-only pending-job marker containing only a generated job name and pinned
image identity. Confirmed cleanup clears that same marker. Uncertain creation,
failed cleanup or a replaced marker leaves the gateway unavailable for review.

Startup refuses a pending marker, an existing socket or any decoder job with the
reserved name prefix, including stopped/created jobs from older images without
labels. It does not automatically delete unknown jobs or state. The lock covers
one configured runtime directory on one host; it is not a distributed lock.

The unit example preserves its runtime directory across service stops. `/run`
does not survive a host reboot, so the startup Docker inspection is still
required. Host reboot, actual systemd installation and host resource-limit failure
remain acceptance gates.

The interruption drill exposed a shutdown ordering bug: waiting for server
connections to close before cancelling their handlers could leave a paused job
blocking shutdown. Shutdown now rejects new work, closes the listener, cancels
and reaps handlers, then waits for closed transports and removes only its own
socket. Explicit socket cleanup also handles Python 3.13's changed default.

## Local evidence

- The Linux gateway test target passed 37 focused cases, including bundle and
  capacity accounting checks. Twelve bundle cases also passed directly.
- The real IPC drill passed with production decoder identity, UID/GID checks,
  read-only client mount, cancellation cleanup and subsequent reuse.
- `Test-PhotoGatewayInterruption.py` passed duplicate-start refusal, graceful
  termination of a paused owned job, forced-kill restart refusal, reviewed exact
  fixture cleanup/reuse and refusal of an unlabeled created-only orphan.
- The budgeted real-HTTP lifecycle passed again with the updated gateway and
  unchanged production decoder: authenticated upload/view/edit/export/delete,
  CSRF and owner isolation, private ZIP hash verification and SQL media erasure.
  No decoder job remained after cleanup. This uses disposable generated data.

The interruption fixture deliberately delays startup for five seconds before
running the real native decoder. This makes interruption deterministic; it does
not establish kernel OOM behavior or interruption inside native codec execution.
Its test-only image must never be exported or installed. The drill uses UUID
containers and its own private volume, and removes only objects it created.

New exports record `gatewayStateProtocol: 1` and require the preserved-runtime
unit setting. Legacy bundles remain readable preparation artifacts; their old
gateway source is not evidence for these restart guarantees. Decoder code and
the previously accepted decoder image are unchanged.

## Operator recovery and remaining gates

An interrupted gateway requires review of the daemon and recorded job before
clearing state. Confirm the old gateway is dead and no create/remove operation
can still complete, inspect the exact job and its pinned image, remove only the
confirmed owned job if appropriate, and confirm absence before clearing its
matching marker/socket. A marker is a barrier, not permission to delete data.
Never clear it merely to make a restart succeed.

Actual host ownership/mapping, systemd/reboot behavior, mixed worker/export/backup
load, recovery selection and native/container notice review remain on the
[host checklist](photo-host-preparation.md). No live unit, resource limit,
database policy, backup schedule or feature flag changed. CI and deployment stay
grouped at the larger checkpoint.
