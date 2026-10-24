# Third-party software notices

PlantCare's [rights-reserved notice](LICENSE) applies to its original source and
documentation. It does not replace the licenses, copyright notices, permissions
or restrictions of third-party software.

## Browser application

The production Angular build generates `3rdpartylicenses.txt`. The API image build
now copies that file into `wwwroot/3rdpartylicenses.txt` alongside the browser
assets; CI checks that the packaged file is nonempty and contains license terms.
This packaging correction is included in the deployed English catalog runtime.

A reviewed build-output snapshot is in
[frontend-notices.txt](docs/release/dependencies/frontend-notices.txt).
It includes notices for Angular, RxJS and tslib. Preserve the actual generated
notices with every build instead of replacing them with this summary. Generated
notices may change when dependencies change.

## Plant reference photos

The reviewed catalog includes CC BY / CC BY-SA photographs and a CC0 photograph.
See [photo credits](plant-care-web/public/images/plants/README.md) and
[the exact image manifest](deploy/catalog-images.json). Each photo retains its
listed license or public-domain dedication; the original application's
rights-reserved notice does not cover it.
The UI displays the supplied title, author, source and license. Original image
bytes are preserved and are scaled for display. See
[catalog review](docs/release/houseplant-starter-catalog.md) and
[flowering batch](docs/release/flowering-houseplant-batch.md),
[larger expansion](docs/release/houseplant-expansion.md) and
[comparison batch](docs/release/plant-comparison.md) for the care sources.

## Backend and container software

The [runtime inventory](docs/release/dependencies/runtime-inventory.json) records
the NuGet packages declared by the actual API/worker manifests for application
candidate `677b91a0083cdfec6e5f2656a82c9594e49d07b0`. It records package metadata
and packaged-notice fingerprints, not a complete redistributable notice bundle or
license clearance. In particular, URL-only declarations, missing notice texts and
the native SQL client package require review before distributing release artifacts.

The .NET shared framework, Linux/base-image packages, SQL Server, Caddy and
build/test tooling have separate terms and attribution obligations. SQL Server
must not be described as covered by PlantCare's license or as wholly open source.
Keep vendor notices intact and review the actual artifacts and intended mode of
distribution. Future botanical content, images and advertising SDKs also require
their own rights review; no image/content license is granted here.

See the [release dependency review](docs/release/dependency-notices.md) for exact
evidence, generation instructions and remaining release gates. This document does
not grant extra rights or certify that distribution review is complete.
