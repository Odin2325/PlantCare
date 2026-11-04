# Local real-HTTP photo-journal acceptance

Accepted locally on 2026-11-04, without pushing, remote CI or staging deployment.
This follows the [decoder bundle/IPC slice](photo-processing-bundle.md). No Apple
device was available; physical Apple acceptance remains pending.

## Reusable focused path

```powershell
python scripts/Test-PhotoHttp.py `
  --api plantcare-api:<accepted-local-api-commit> `
  --bundle .operations-local/photo-processing-bundles/<decoder-source-commit>
```

Existing local Linux amd64 images are required: API, bundle decoder, the gateway
test image, browser test image, SQL Server, Caddy and Mailpit. The tool resolves
and uses immutable local image IDs with pull_policy=never; it never builds/pulls
an API or uses a saved app configuration. Optional --gateway/--browser select
existing local acceptance images. Repeat this focused path when the actual
upload/IPC/storage boundary changes, not for ordinary catalog work.

The generated Compose project has its own UUID, SQL database/volume, API keys,
IPC volume, proxy certificates and internal Mailpit. Passwords are generated in
the ignored private scratch directory and removed after successful cleanup.
It does not read normal app environment files, accounts, snapshots or credentials.
All application services share a Docker internal network and publish no ports;
the gateway uses network=none and only it receives Docker-daemon access. No worker
is started, calendar providers are off and Web Push keys are blank. Email stays
in internal Mailpit; no test email or reminder is sent outside the project.

The API uses Production settings, real confirmed Identity accounts/cookies,
antiforgery, additive SQL migrations and the restricted plantcare_api login. An
operator-only job creates the database/grants and imports the public tiny catalog
fixture. API UID/GID 1654 and the read-only IPC mount are checked; no Docker socket
is mounted into the API. The real production Caddy upload-body matcher is used
with a local internal certificate and a fixed .test host. Only this browser test
context ignores that local certificate; public HTTPS settings remain unchanged.

A phone-sized Chromium browser drives real journal controls. No API routes,
decoder or database results are mocked. Because Docker internal networks have
no default route, the browser's online hint is explicitly modeled as true while
all requests still traverse the isolated HTTPS/API/SQL/decoder path. This is not
an offline-delivery or physical-device test. FormData calls are observed without
changing serialization; request-ID lookup verifies the same ID on the server.
Raw browser request bodies, tokens, cookies and confirmation links are not logged.
There are no traces/videos/screenshots or external destinations.

## Passing checks

- Fresh migrations, restricted API runtime check, real root gateway/private
  socket and isolated decoder using the [accepted bundle](photo-processing-bundle.md).
- Two generated accounts register, receive internal confirmation mail and sign
  in through the real API. The owner creates a synthetic personal plant.
- Browser file/date/note entry sends a real CSRF-protected multipart upload;
  source filename is masked. The 8x4 generated PNG is normalized to JPEG by the
  actual isolated decoder and stored in SQL. Thumbnail/full image decode in the
  browser; private content carries no-store headers.
- Another account cannot list, fetch thumbnail/image, reconcile the request ID
  or delete the owner's entry. Missing-CSRF upload is rejected. Duplicate request
  ID returns the original entry, and invalid PNG bytes return the allowlisted
  photo-invalid response without another saved row. Personal plant/schedule
  values and care history remain unchanged.
- The viewer receives focus, closes correctly, notes editing and the photo date persist over
  reload and password-verified private ZIP export works. Python independently
  checks the ZIP's exact entries, normalized JPEG markers and manifest SHA256.
- Keep photo cancels removal; explicit confirmation deletes it. Re-upload then
  account deletion exercises media erasure with a photo still present. SQL checks
  prove both PlantPhotoEntries and PlantPhotoMedia are empty afterward.
- No decoder job remains. Cleanup removes only the generated UUID project and
  its own volumes/network/private scratch; failure to clean up is reported and
  retains private configuration for inspection. A non-secret local status records
  result, cleanup and pinned image identities.

This run reused API image candidate d8ff22b2fc66e4444032796015cd58a1deade5a8
and decoder bundle candidate 6b725e9db5ed01754179165b277c433389a33590. The backend
and journal UI had no source changes since that accepted API image, so no rebuild
or full suite was needed for this test-only slice. API image ID was
sha256:a52e7fe4a0ff71ff79fac39cdc43ed939054bc4c13b4ab7f24430d7950e2a9bc;
decoder image ID was
sha256:990101a56d7f9dff7959c91e0c1d1fe2c538537e9b7090fad2616749319d1db2.
Use fresh exact-candidate images again at the next combined checkpoint.

## Remaining activation work

The subsequent [capacity slice](photo-capacity.md) adds a larger generated sample,
kernel resource accounting and an optional disposable memory-budget rehearsal.
Its measurements do not change normal app settings or close host activation gates.

Keep normal local/staging uploads disabled. This does not certify the installed
VPS systemd service, actual user-namespace mappings, sustained load, backup-growth
capacity, interrupted-backup behavior or a real camera/device. Next: measure
decoder/API/SQL resource headroom and archive growth, finish host/package notice
review and prepare the combined activation acceptance. Full candidate CI,
fresh migrated recovery and real installed Android upload/view/delete precede
activation. Apple hardware testing can follow when a device becomes available;
do not infer it from Chromium or desktop WebKit results.
