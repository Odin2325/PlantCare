# Photo candidate: host and artifact preflight

Reviewed locally and read-only against staging on 2026-11-05. This is candidate
preparation, not host installation or photo activation. Full CI and deployment
remain grouped at the combined checkpoint.

## Host facts

The repeatable `scripts/Inspect-PhotoHost.py` was piped through the existing
trusted SSH connection to Python stdin; no script was installed on the VPS.
It inspects only versions, fixed-path ownership/modes and selected Docker/API
metadata. It neither reads environment values nor changes services or settings.
Its safe operational record is `.operations-local/photo-host-preflight.json`.

| Check | Observed state |
| --- | --- |
| Architecture | x86_64 |
| Python | 3.14.4 |
| Docker server | 29.9.0 |
| systemd | 259, Ubuntu package 259.5-0ubuntu3.4 |
| API process UID/GID | 1654:1654 |
| API configured user | app |
| API Docker/gateway mounts | neither present |
| Docker rootless/user-namespace option | not enabled in inspected metadata |
| API memory limit | currently unlimited (0); no cap applied |
| Gateway source/unit/runtime/socket | not installed/present |
| Inspected decoder image loaded | no |

This supports the intended root-gateway/API identity arrangement but does not
accept SO_PEERCRED mapping on a real socket, an installed systemd unit, reboot/
limit-failure behavior or the proposed host budget. There is no running gateway
to accept yet. Neither image loading nor service activation occurred.

## Exact image and package evidence

[photo-runtime-inventory.json](dependencies/photo-runtime-inventory.json) binds
the shipped decoder dependency manifest to the two Magick.NET 15.0.0 package
content hashes and cached archive checksums. It uses the existing inventory tool;
it is not package-signature verification or legal clearance.

The stopped-container collector `scripts/photo_image_inventory.py` streams an
export of the exact accepted image. It never starts the container or extracts
arbitrary paths. It hashes shipped native files and preserves regular OS,
framework and Magick notice bytes in an independent archive. Notice links are
recorded as metadata; shared license texts are retained separately. Its verifier
checks archive and individual hashes, bounded regular members, exact membership
and duplicate/link rejection without Docker or extraction. Compare the inventory
digest through the trusted channel as well; an archive checksum is not a signature.

The original portable image from bundle
`755c477b4f6bf93d100f3610e430c6ba59a79c7f` was
`sha256:2e7091bab8f422325d3739917a9fbfe6342b0bc601af58187095ec4ec0cb2fad`.
It contained Ubuntu 24.04.5, .NET 10.0.14, 98 installed OS package records,
112 retained regular notice files and four notice links. Its seven native assets
included Linux, musl Linux, ARM, macOS and Windows variants. The full retained
notice archive is 1,884,160 bytes. Historical bundle `6b725e9` has a different
image identity and cannot substitute for this exact-image evidence.

## Publish correction

The decoder Dockerfile now restores/publishes `linux-x64`, framework-dependent,
for its already-declared Linux amd64 runtime. This selects platform-specific
native dependencies, as described in Microsoft's
[RID catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) and
[publishing documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/).
It does not change the application's supported upload MIME types, library
versions, native policy, non-root identity or vendor notice files. New preparation
manifests declare the RID and reject a mismatched dependency target; legacy
preparation bundles remain verifiable.

Focused native decoder/supervisor checks passed (29), as did bundle-integrity
checks (13) and notice-archive integrity checks (5). Exact committed-source export,
new-image inventory and focused HTTP acceptance follow before treating the
publish correction as an accepted artifact. No full suite/remote CI ran here.
The five archive cases are also wired into the existing Linux gateway test target;
all 43 gateway/bundle/capacity/archive cases passed in that focused target.

## Remaining activation gates

- Finish the native linked-component/source/terms mapping and the remaining
  backend/vendor distribution review. The vendor notice has 39 component headings,
  including sections with LGPL/GPL terms; headings and keyword hits alone do not
  prove a component's linkage, chosen alternative license or obligations. Disabling
  a codec in policy does not remove its compiled assets. `licenseReview` remains
  `incomplete`; preserved notices alone do not close this gate. The upstream
  [Magick.Native project](https://github.com/dlemstra/Magick.Native) and packaged
  exact-version notice text are review inputs, not a substitute for that mapping.
- Bind the exact combined API/worker/decoder artifacts and gateway source/unit
  hashes, then run one exact-candidate CI/recovery checkpoint. Keep flags off.
- At the reviewed host window, install/validate root-owned files and the pinned
  unit, real socket identity/read-only mount, chosen caps and failure/restart
  behavior. The local test gateway image is not the host unit.
- Select and apply a reviewed live recovery/log-growth policy. The snapshot-only
  alternative remains a disposable rehearsal; live recovery settings are intact.
- Capture/recover the added host software and test installed Android photo
  upload/view/delete, export/erasure and existing-session update. Physical Apple
  acceptance remains deferred until hardware is available.

Existing public/staging endpoints, account data, backup repositories, schedules
and resource/recovery settings were unchanged by this review.
