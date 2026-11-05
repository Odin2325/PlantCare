# Photo format and entry-count acceptance

Local generated-data check, 2026-11-05. Uploads remain disabled outside the
disposable HTTP project. This opt-in mode is separate from the larger encrypted
recovery and mixed-workload checks:

```powershell
docker build -f tests/photo-format-fixtures.Dockerfile --build-arg DECODER_IMAGE=<accepted-local-decoder-image> -t plantcare-photo-format-fixtures:local .
python scripts/Test-PhotoHttp.py --capacity --budget --formats-counts --api <local-api-image> --bundle <verified-bundle-directory>
```

The test-only generator uses the existing pinned image-processing dependencies to
create a quadrant-colored EXIF-orientation-6 JPEG and transparent PNG/static WebP
files with generated metadata. It has no user images or credentials. The runtime
drill pins its image ID, checks its test-only label, gives it only the generated
fixture directory, and runs without network, extra capabilities or a Docker
socket. Never export/install this generator as a production decoder.

## Real HTTP format gates

Every upload crosses HTTPS, the authenticated API, private IPC, the unchanged
isolated production decoder and SQL. The JPEG must become a 30 × 60 image with
the expected rotated quadrant colors. Transparent PNG and static WebP must become
opaque white JPEGs without upscaling. Both full images and thumbnails must be
private no-store JPEGs without APP1–APP15 source profiles or comment segments.

Unsupported HEIC/GIF MIME probes must return 415; mismatched MIME and a truncated
JPEG must return sanitized invalid-image errors. Framed APNG/WebP animation
headers must return the explicit animation-policy rejection. These header probes
are not complete animations or a native animated-codec test. No HEIC decoding or
real Apple camera compatibility is claimed. Rejections must leave saved counts
unchanged and subsequent valid jobs must work.

## Independent count gates

The coordinator copies only genuinely normalized fixture media within the
UUID-owned SQL database. It first fills one plant to 20 entries while the account
has only 20, so the 21st upload exercises the plant limit independently. A real
delete frees one slot; the replacement succeeds and the next upload is refused.

It then adds two synthetic plants' media to reach 50 account entries distributed
20/20/10, using one detailed original plus 49 small white JPEG/thumbnail pairs.
Total bytes must remain below 2 MiB, well under the 20-MiB account byte ceiling.
An upload to the ten-entry plant must be refused without a count change. This
isolates the account entry-count limit from per-plant and byte quotas.

A password-verified no-store ZIP must contain exactly 50 distinct image files
plus its manifest. Independent verification checks every JPEG/hash and the
expected one-detailed/49-white image mix. After releasing only generated copies,
a valid upload/delete proves reuse. The original plant/care data must match its
pre-upload state, and normal explicit photo deletion/account erasure and SQL
media cleanup still run.

The safe report is `.operations-local/photo-formats-counts.json`; passed requires
the complete browser lifecycle and UUID project cleanup. Failure diagnostics are
fixed phase labels and HTTP status codes, never credentials or raw tool output.
No normal database, backup repository, schedule or staging flag is accessed.

## Local acceptance, 2026-11-05

The focused generated-fixture build and real HTTP run passed with the accepted
API/decoder images. The safe report pins the test-generator and runtime image
IDs. Python/JavaScript syntax and CLI prerequisite checks also passed.

JPEG orientation-6 quadrant positions, opaque-white PNG/WebP output, metadata
segment removal and all six bounded rejection probes passed. The per-plant test
refused entry 21, accepted a replacement after deleting one of 20, and refused
the next. The account test refused entry 51 on a plant with ten entries while
total media used only 562,653 bytes, isolating count from the byte ceiling.

The 555,099-byte ZIP contained 50 distinct JPEGs with verified manifest hashes
and the expected image mix. After fixture release, a real upload/delete succeeded
again. Original plant/care data, final photo/account erasure and SQL media cleanup
passed. No decoder job or UUID scratch directory remained. No normal upload flag,
runtime dependency, backup repository or schedule changed; no push/CI/deployment
was performed.

## Scope

These generated format samples complement the native normalizer tests, existing
large-input/near-byte-quota sample and restored-app checks. They do not cover all
EXIF orientations, real-device camera metadata, every codec/container variation,
diverse-media retention growth, service-wide saturation or actual VPS installation.
Physical Apple acceptance still waits for an available device. Full CI and
deployment stay grouped at the meaningful combined checkpoint.
