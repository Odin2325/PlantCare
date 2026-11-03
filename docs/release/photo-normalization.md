# Photo normalization prototype

Implemented locally on 2026-11-03. This completes the decoder/normalization portion
of the [photo-journal design](photo-journal-design.md), not its process-isolation
gate. Uploads remain unimplemented. No API/worker reference, DI registration,
route, migration, image store, frontend change or deployment was added.
The following [standalone process-isolation increment](photo-process-isolation.md)
adds the Linux supervisor/container boundary and its focused acceptance. Remaining
prototype limitations below describe the original increment unless updated there.

## Dependency decision

The isolated `PlantCare.PhotoProcessing` project pins `Magick.NET-Q8-AnyCPU` and
its resolved `Magick.NET.Core` dependency to 15.0.0. The NuGet version index and
package metadata were inspected on 2026-11-03. The restored package references
upstream commit `79bb8700d7fe3a564f62926ab299fc2520cb2269` and its bundled notice
identifies ImageMagick 7.1.2-33. These are package metadata observations, not a
reproducible-build attestation.

Both NuGet declarations use Apache-2.0. The native package carries extensive
additional codec/delegate notices: its license expression alone is not a complete
native attribution inventory. Unmodified packaged notice files are committed in
`PlantCare.PhotoProcessing/ThirdPartyNotices` and copied to build/publish output.
Their SHA256 values are:

| File | SHA256 |
| --- | --- |
| Magick.NET-15.0.0.txt | `a08ce0f18e36f83c971ab35a7504523fa25a797e99eec57e67c96ab4648b1409` |
| Magick.NET.Core-15.0.0.txt | `0b68e9a3c1456e76d84d57dd42da6fc1c71195a5ad2a466ed480b42e22aaa054` |

NuGet all-dependency audit is enabled, with high/critical advisories treated as
errors. Restore and an explicit transitive vulnerability listing reported no
known vulnerable packages from the configured NuGet sources at this check. This
does not assert the absence of native vulnerabilities. Recheck exact versions and
the selected Linux native assets before distribution; carry the corresponding
notice files in the eventual image. The current API/worker images are unaffected.

ImageSharp was considered but not added. Its current documentation describes
split licensing and build-time license keys; the prototype instead uses the
Magick.NET package's explicit Apache declaration and bundled native notices.
This is a dependency choice, not a blanket licensing assessment for future uses.

Primary references:

- [Magick.NET project](https://github.com/dlemstra/Magick.NET)
- [Pinned package](https://www.nuget.org/packages/Magick.NET-Q8-AnyCPU/15.0.0)
- [Upstream resource-limit implementation](https://github.com/dlemstra/Magick.NET/blob/79bb8700d7fe3a564f62926ab299fc2520cb2269/src/Magick.NET/ResourceLimits.cs)
- [ImageSharp documentation](https://docs.sixlabors.com/articles/imagesharp/)

## Implemented behavior

`PhotoNormalizer.NormalizeAsync` accepts a stream and supported MIME type, without
filenames or URLs. It admits one operation and immediately rejects a second; no
waiting queue is built. The slot covers reading, inspection and processing and
is released after every exception/cancellation. It reads at most 8 MiB plus one
sentinel byte, including for non-seekable streams, entirely in memory.

JPEG/PNG/WebP signatures must match MIME type. Container lengths are checked with
overflow-safe bounds; truncated containers and PNG/WebP animation markers are
rejected before native decoding. JPEG requires its end marker; native warnings
and corrupt inputs are rejected without returning native messages. The selected
format is explicit in decoder settings, rather than guessed from a filename.
Metadata-only ping checks dimensions before raster allocation: at most 10,000
pixels per axis and 20 million pixels total, then dimensions are rechecked after
decode. A successful decode is still required; signature checks are not validation
by themselves.

Processing applies orientation, converts to sRGB, flattens transparency to white,
strips metadata and copies pixels into a fresh image. It does not upscale. The
larger JPEG is at most 1,600 pixels on its longest edge and 512 KiB; the thumbnail
is at most 320 pixels and 64 KiB. Three bounded quality attempts (82/70/58) are
allowed. Encoding streams reject writes above the budget instead of allocating
unlimited output; an incompressible input fails rather than bypassing the cap.
Only normalized bytes/dimensions are returned. Originals are not retained on disk
or logged. Stable failure codes do not contain filenames, native messages or
photograph metadata.

## Isolation boundary still required

This class is a prototype for a **dedicated disposable process**, not an API
service. Native memory/profile/pixel-cache/dimension/thread/list limits are
process-global, and they do not bound all allocations or guarantee a wall-clock
deadline. Disk cache is disabled. ImageMagick's list limit includes intermediate
images, so it is 16; input animation is separately prohibited by container checks.
The native process-lifetime time limit is deliberately not used as a resettable
request timeout. Cancellation checks around synchronous decoding cannot interrupt
every native operation.

The next slice must provide a killable process with bounded input/output pipes,
an external wall-clock deadline, OS-enforced memory/CPU limits, read-only filesystem,
no network/delegates and a strict coder policy. Terminate and reap failed or
canceled children and release admission only after exit. Use one request per
child. Prove this on the Linux runtime budget with hostile/slow fixtures and
synthetic supervisor failures before adding any upload route. Do not register the
normalizer directly in API DI, assume `Task.Run` isolates it or treat cancellation
as a hard native timeout. No source-image fixtures from real users were used.

## Focused verification

Fifteen new tests passed locally on Windows/.NET 10. They cover all three accepted
formats, both JPEG size caps, no upscaling, white transparency, EXIF orientation,
profile/comment removal, unsupported MIME before reading, mismatched content,
corrupt/truncated input, header-only excessive dimensions, animation markers,
overflowing chunk lengths, non-seekable oversize input, actual high-entropy output
rejection, prompt busy rejection and cancellation releasing admission.

The new project/tests are included in the solution, so full CI will build and
exercise them at the next checkpoint. Existing conservative scope rules classify
these new code/project paths as full scope; no selector rule was changed. Linux
native execution, hard process bounds and actual published-image attribution
acceptance remain unperformed. No full suite, CI, push or deployment was run for
this local increment.
