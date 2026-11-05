# Photo-processing preparation bundle

This local slice packages the isolated decoder separately from the API/worker.
It does not install a gateway, mount a socket into the app or enable uploads.
Apple physical-device acceptance remains pending because the owner has no Apple
device available. Routine work retains focused checks and local commits; a full
deployment waits for the next substantial batch.

## Committed-source export

With Python 3.12+ and Linux Docker running:

```powershell
python scripts/photo_bundle.py export --candidate HEAD
python scripts/photo_bundle.py verify --bundle .operations-local/photo-processing-bundles/<full-sha>
```

The exporter reads only the selected Git commit, never the current source tree,
environment files, account data or working-directory photos. Its allowlisted
source archive contains the decoder project, decoder Dockerfile, gateway script
and two preparation templates. The decoder is built for Linux amd64 and inspected
for the expected non-root user and normalization entrypoint. A stopped container
provides the actual shipped dependency manifest, notices and native policy; its
notice/policy hashes must match committed source. The container is never started.

The resulting directory contains a separate decoder image archive, pinned
gateway service example, gateway source, disabled Compose override example,
actual decoder dependency manifest, policy and two Magick.NET notice files.
Every payload has a SHA256 in `manifest.json`; the exporter reports the manifest
hash separately. Final directories are published only after verification and
existing bundles are never overwritten. Failed scratch is removed without
deleting unrelated artifacts or images.

Verification rejects changed/missing/unexpected files, links/junctions, unknown
manifest paths, wrong platform/identity, changed service-image binding, removed
notices and an upload-enabled override. It reads files only: no Docker load,
package execution, installation, socket connection or remote action. Hashes do
not authenticate an attacker-rewritten manifest; compare its digest through the
trusted deployment channel and verify the loaded image identity at activation.

`licenseReview` remains `incomplete`: these two notice files do not close the
container/.NET/native nested-license review. Keep vendor notices in the image,
review the exact dependency manifest and finish attribution before distribution.

## Host/API boundary preparation

`deploy/compose.photo-processing.yaml.example` keeps both feature flags false,
expects a root gateway and adds only the private socket directory as a read-only
API bind. It forbids Docker Compose from silently creating a missing host path.
It does not mount the Docker socket, add privileges or alter the worker. Existing
base Compose and saved deployment configuration remain unchanged.

The accepted host arrangement must be checked again on the actual VPS:

- Gateway source/unit are root-owned, not writable by the API. The host gateway
  is trusted Docker control-plane code; only it receives Docker access.
- Runtime directory is root:1654 mode 0750; socket is root:1654 mode 0660.
  API process uses UID/GID 1654:1654 and expects gateway UID 0 through SO_PEERCRED.
- API socket-directory mount is read-only, and no daemon socket is present.
  User-namespace/rootless mappings need explicit acceptance; do not weaken peer
  identity checks merely to make a mapping work.
- Load/inspect the exact accepted decoder image and bind the service to its
  immutable image ID. Do not enable the example unit or flags as part of export.
- Measure actual decoder peak memory/time, API/SQL headroom and backup size/time
  on the 4GB VPS before the later activation checkpoint. Verify the real
  HTTP → gateway → decoder → private SQL path, migrated restore/erasure handling,
  rollback/fail-closed behavior, and installed-device upload/view/delete.

The synthetic `Test-PhotoGateway.py` drill now uses the intended UID/GID instead
of giving its API-side client group 0. It checks real directory/socket ownership,
two consecutive client/decoder jobs, rejection of a different connecting UID and
wrong expected gateway UID, invalid images, cancellation/reuse and cleanup.
A separate directory-owner probe confirms the client mount returns EROFS on a
write; ordinary API permissions remain more restrictive. This drill grants the
Docker socket only to its disposable trusted gateway, never the .NET client or
decoder. It contains generated images and sockets only, no account credentials.

## Local acceptance (2026-11-04)

- Ten bundle-integrity cases passed locally. The existing Linux gateway test
  target now runs these alongside its nine gateway cases: nineteen passed.
  This wires the new checks into the existing full runner at the next checkpoint
  without changing the conservative test selector or starting a CI run.
- The disabled override merged with base Compose using public example settings;
  no services were started by that configuration check.
- Exact committed source `6b725e9db5ed01754179165b277c433389a33590` built and exported
  successfully. Its separate archive, shipped dependency manifest, notices/policy,
  pinned service example and disabled override passed independent read-only
  bundle verification. The bundle stays in ignored local operational storage.
- The real IPC drill passed against that exported decoder with root:1654 gateway
  and 1654:1654 client identities, actual 0750/0660 ownership, consecutive jobs,
  wrong-UID rejection, read-only mount enforcement, invalid input and cancellation
  cleanup/reuse. The first run exposed a group-0 assumption in the old test
  launcher; matching the existing systemd Group=1654 fixed it without granting
  extra capabilities or weakening peer checks. No decoder job remained.

This is packaging and transport evidence. It does not certify a root-owned host
installation, full HTTP upload/storage flow, VPS capacity or enabled phone uploads.
No normal local/staging account, database, backup, schedule or feature flag was
changed. No push, remote CI or deployment was performed.

## Next checkpoint

New bundles include `gatewayStateProtocol: 1` and the preserved-runtime unit
setting for the [restart barrier](photo-gateway-restarts.md). Legacy bundles
remain verifiable but do not certify the new gateway behavior. Export the exact
combined candidate again before host installation; never substitute the delayed
interruption-test decoder for the production decoder.

New exports also declare `decoderRuntimeIdentifier: linux-x64` and check that
the dependency target matches. The [host/artifact preflight](photo-candidate-preflight.md)
records the portable-image finding, focused publish correction and remaining
license/host gates. This RID declaration does not mark attribution review complete.

Keep uploads disabled. [Local real-HTTP upload acceptance](photo-http-acceptance.md)
now passes with the isolated gateway/decoder. Next is capacity and host-install
preparation. Group the
result with Apple refinements and other substantial changes for full candidate
CI, artifact/license review, fresh recovery and one staging deployment. Physical
Apple and store beta work remain separate; no device purchase is required now.
