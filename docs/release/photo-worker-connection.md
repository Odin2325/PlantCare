# Private API-to-photo-worker connection

Implemented locally on 2026-11-03. This follows the
[standalone process boundary](photo-process-isolation.md). The connection is
prepared and tested; **uploads remain disabled**. No public route, storage table,
migration, personal photo, UI control or VPS deployment was added.
The following [storage/lifecycle increment](photo-journal-storage.md) adds private
SQL storage and protected read/edit/delete/export routes. Uploads remain disabled.

## Connection implemented

`PlantCare.PhotoTransport` is a BCL-only library containing `IPhotoProcessor`,
its result/error contract and the Unix-socket client. Infrastructure registers
it as a singleton. The API's `PhotoProcessing.Enabled` setting defaults to false;
disabled calls fail before connecting. An enabled client works only on Linux.
Neither this library nor the API/worker references Magick.NET, launches a process,
calls Docker, or needs the Docker socket. The root Dockerfile copies the new
project file before restoring API/worker dependencies.

The client connects to a configured absolute filesystem socket, authenticates the
server UID through Linux `SO_PEERCRED`, and sends a versioned bounded binary
request. The host gateway independently checks the API peer UID before reading
image bytes. Socket-directory permissions and an API read-only socket-directory
mount complement these identity checks. The default path is
`/run/plantcare-photo/gateway.sock`; the default expected gateway UID is root.
This authenticates the local processes, not a PlantCare account. Future upload
routes must still authenticate the user and check ownership/CSRF before reading
or sending an image. No account identity, email, token or plant record enters IPC.

`scripts/photo_gateway.py` is a small trusted Linux host gateway using Python's
standard library. It accepts only a pinned local decoder image ID of the form
`sha256:<64 lowercase hex digits>`. It uses a fixed local Docker socket and
executable, argument arrays without a shell, three supported MIME values, generated
container names and fixed sandbox options. A request cannot supply an image,
command, URL, filename, Docker flag, environment variable, bind mount or quota.
Images are not pulled during a request.

Every request creates a fresh container with the restrictions already accepted:
384 MiB maximum/no swap, at most one CPU, PID cap 64, no network, read-only root,
no capabilities, no-new-privileges and no Docker log recording. The container's
own startup guard and native policy remain mandatory. The gateway has Docker
privileges; the decoder never receives them. The API receives only a private Unix
socket, not those host privileges. The gateway is part of the trusted host control
plane: Docker access is powerful, so install its source/configuration root-owned,
keep its API narrow and review it at the activation checkpoint. Do not mount the
Docker socket into the API or install the test gateway image as the production API.

## Bounds, failures and lifecycle

Requests use `PCR1`, one MIME code and a 32-bit byte length, followed by at most
8 MiB. Replies use `PCS1`, a success/error flag and bounded length. Success carries
the existing `PCP1` response, at most 20 bytes plus 512 KiB image and 64 KiB
thumbnail. Both gateway and client check dimensions, lengths and JPEG markers.
Error replies contain only an allowlisted code of at most 64 bytes. Unknown,
truncated, extra or oversized replies fail closed. Neither endpoint logs image
bytes or raw subprocess errors. There is no HTTP listener or externally usable
download link.

The API client admits one request without queuing. The gateway separately admits
one request globally and immediately rejects others as busy. Reading a gateway
request has a five-second deadline. A Docker control operation has a five-second
deadline; attached processing has a thirty-second deadline including startup,
while the decoder retains its own ten-second limits. The client default deadline
is forty-five seconds and is capped at sixty. These are photo-transport bounds;
no backup or health-monitoring threshold changed.

The gateway creates a container first, then attaches/starts it. Cancellation during
creation waits for that bounded operation to settle before cleanup; original
bytes are not sent to a half-created job. A client keeps its socket open while
waiting. Disconnect or trailing request bytes cancel processing. The gateway
kills/reaps its Docker CLI, removes only the generated job, and confirms absence
when removal reports an already-removed container. Killing the CLI alone is never
treated as container cleanup. Admission is released only after cleanup. Uncertain
creation/cleanup poisons the launcher so it refuses further jobs rather than
silently overlapping them. Operator review is required before restarting it.
SIGTERM stops acceptance and awaits active connection cleanup.

Original bytes and normalized output stay in bounded process/pipe memory. There
is no image queue on disk, no network transfer to an image provider and no cache
or backup scope change in this increment. Failed normalization creates no private
record, reminder, schedule, notification or harvest.

## Preparation, not activation

The host-service template is
`deploy/plantcare-photo-gateway.service.example`. Its placeholder image ID is
deliberately invalid until replaced with an accepted local image ID. It is not
installed or enabled. Prepare the dedicated runtime group/directory and install
the gateway root-owned; verify actual API UID/GID and user-namespace mappings on
the VPS rather than assuming they match the local test. A Unix socket is local
to its host kernel. Only the runtime socket directory may be mounted read-only
into the API; no source, daemon socket or upload directory is shared with it.

Enabling the transport setting is not an upload feature flag: no upload route
exists yet. Do not activate it while the storage, ownership, export and recovery
gates remain unfinished. The final deployment packaging/attribution review must
cover the exact decoder image and host Python/Docker installations. The Dockerfile
under `tests/` produces synthetic acceptance tools, not publication artifacts.

## Focused verification

- API builds successfully. One isolated API registration test passed: the default
  service fails closed, and API Infrastructure/transport assembly references do
  not include the decoder or Magick.NET. It uses an in-memory test host; no normal
  local or staging database was changed.
- Twelve client tests passed on real Linux sockets: request framing, successful
  decoding of the result envelope, server-UID rejection before sending bytes,
  bounded/malformed/truncated/extra responses, safe errors, busy admission,
  deadlines, cancellation, socket closure and reuse.
- Nine gateway tests passed on Linux: caller identity, invalid format/oversize
  rejection before launch, busy/disconnect/trailing-data cleanup, bounded reads,
  image pinning, durable cleanup failure and cancellation during creation.
- Synthetic end-to-end acceptance passed through the same .NET client, real
  `SO_PEERCRED` checks, gateway and fixed accepted decoder image. Consecutive jobs
  returned correct dimensions; invalid images and wrong gateway identity failed
  safely. Cancellation allowed subsequent reuse and left no decoder job behind.
  Test containers/volumes contained only generated images and the test socket,
  and were removed by their uniquely scoped cleanup.

Repeat only these focused checks when their code changes:

```powershell
dotnet test PlantCare.Api.IntegrationTests/PlantCare.Api.IntegrationTests.csproj `
  --filter FullyQualifiedName~PhotoTransportRegistrationTests
docker build -f tests/photo-gateway.Dockerfile --target client `
  -t plantcare-photo-client-tests:local .
docker build -f tests/photo-gateway.Dockerfile --target gateway-tests `
  -t plantcare-photo-gateway-tests:local .
python scripts/Test-PhotoGateway.py
```

The last command requires the local decoder image from the previous milestone.
Only its disposable trusted gateway is granted the local Docker socket for this
test; client and decoder containers have no such mount. No existing application
containers, volumes, backup repositories, schedules or credentials are modified.

These checks join the existing full-scope CI step at the next batch checkpoint.
Unknown/sensitive changes and the workflow edit still select full validation.
No full suite, remote CI run, push, installation or deployment occurred here.
Primary IPC reference: [Linux Unix-domain sockets and peer credentials](https://man7.org/linux/man-pages/man7/unix.7.html).

## Next slice

Implement the private photo-entry/media schema, transactional quotas, owner/CSRF
protection and export/deletion/erasure-recovery behavior. Put upload admission
before reading a multipart body; the byte-array transport gate alone does not
bound concurrent HTTP request buffering. Then connect the owned-plant journal UI.
Keep uploads off until full candidate CI, migrated backup/recovery and measured
VPS gateway/decoder capacity pass. This transport acceptance does not certify
the actual VPS configuration or open public uploads.
