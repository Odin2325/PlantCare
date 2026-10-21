# Third-party software notices

PlantCare's [rights-reserved notice](LICENSE) applies to its original source and
documentation. It does not replace the licenses, copyright notices, permissions
or restrictions of third-party software.

## Browser application

The production Angular build generates `3rdpartylicenses.txt`. The API image build
now copies that file into `wwwroot/3rdpartylicenses.txt` alongside the browser
assets; CI checks that the packaged file is nonempty and contains license terms.
This packaging correction is prepared in source and verified in a rebuilt image;
it has not yet been deployed to public staging.

A reviewed build-output snapshot is in
[frontend-notices.txt](docs/release/dependencies/frontend-notices.txt).
It includes notices for Angular, RxJS and tslib. Preserve the actual generated
notices with every build instead of replacing them with this summary. Generated
notices may change when dependencies change.

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
