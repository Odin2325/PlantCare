# Dependency attribution and packaging review

Reviewed 2026-10-21. This work closes a verified frontend packaging gap and starts
the backend/container distribution review. It does not accept a public release.
The staging application remains `677b91a`; the correction below is not yet live.

## Frontend finding and correction

Inspection of a stopped disposable container from the exact existing API image
found `/app/wwwroot/3rdpartylicenses.txt` absent. Angular produces that notice file
at the build-output root, beside `browser/`; the Dockerfile copied only `browser/`.

Production now explicitly enables `extractLicenses`, and Docker copies the
generated notice file separately into `PlantCare.Api/wwwroot` before publishing.
The build fails if the source file is absent. CI also checks the final API image
for a nonempty notice file with copyright and permission text. No dependency
version or application behavior was changed.

The corrected API image built successfully using the supported Node runtime in
Docker. A disposable, non-application container passed the exact new CI attribution
check. The current host Node 24.13.0 remains below Angular's minimum and was not
upgraded; local host frontend verification was therefore replaced by the real
Docker production build. Existing nullable/CSS-budget warnings were not hidden.

[frontend-notices.txt](dependencies/frontend-notices.txt) is the exact generated
text from that reviewed build, covering eight package headings: six Angular
packages (MIT), RxJS (Apache-2.0) and tslib (0BSD). It is an attribution artifact,
not a complete SBOM of every library compiled into dependencies or build tooling.
The source `plant-care-web/pnpm-lock.yaml` SHA256 at review was
`fdbb25d0fdaaa7be57891df2ea320155001bcdf15e78961271ac7544aa8deab8`.
Regenerate notices when dependencies change; do not hand-edit their license text.

## Shipped NuGet inventory

[runtime-inventory.json](dependencies/runtime-inventory.json) comes from the real
API and worker `.deps.json` files copied from stopped containers for staging
candidate `677b91a0083cdfec6e5f2656a82c9594e49d07b0`. There are 29 distinct package
ID/version pairs. This is the manifest's package set, not a claim that every
platform-specific asset in every listed package is shipped or executed on Linux.

[Export-RuntimeLicenseInventory.ps1](../../scripts/Export-RuntimeLicenseInventory.ps1)
matches each package's NuGet restore content hash to the shipped manifest, then
checks the cached package archive against its separate SHA512 sidecar. It reads
metadata and notice entries from those archives without extracting/executing code.
No machine-local cache path, environment values or account data enters the report.
The report includes normalized notice-text hashes rather than treating license
names/URLs as complete notice texts. Twenty-one packages lack a packaged notice
entry under the collector's rules; eight have entries available for review.

NuGet content hashes exclude signing metadata and can differ from whole-archive
hashes. Both associations are recorded distinctly. This is local restore/cache
evidence, not independent NuGet signature verification. The initial raw-archive
comparison rejected a signed package correctly as unequal; the collector was
corrected to use the separate content/archive checks. See the
[NuGet metadata design](https://github.com/NuGet/Home/wiki/Nupkg-Metadata-File).

Generation with PowerShell 7, after obtaining the exact image manifests privately:

```powershell
./scripts/Export-RuntimeLicenseInventory.ps1 `
  -DependencyManifest @('path/to/api.deps.json', 'path/to/worker.deps.json') `
  -Candidate '<full application commit SHA>' `
  -OutputPath 'docs/release/dependencies/runtime-inventory.json'
```

The collector was run repeatedly with identical output. A deliberately altered
manifest content hash was rejected before an output file was written. Deterministic
ordering was corrected during verification. No normal account or database was used.

## Remaining gates before artifact distribution

1. Review/carry required notices and terms for every actually distributed backend
   asset, including packages whose metadata only supplies an expression or URL.
   WebPush and Portable.BouncyCastle use legacy URL declarations; obtain the
   appropriate source/version text rather than assuming current website terms.
   Microsoft.Data.SqlClient.SNI.runtime declares a packaged `LICENSE.txt`; review
   it and determine which native platform assets are actually distributed.
2. Account separately for the .NET runtime and exact container OS/vendor contents,
   SQL Server and Caddy. Preserve included vendor notices. The NuGet inventory
   intentionally does not clear these or the build/test-only dependency set.
3. Review dependency licenses/notices included inside another dependency; the
   eight frontend package headings are not proof of complete nested attribution.
4. For a release candidate, rebuild from that exact committed source, verify notice
   files in the actual exported image/bundle, and check the public notice URL after
   authorized staging activation. The old staging runtime still lacks this file.
5. Keep original-project rights reserved separate from third-party permissions and
   any later species/image licenses. Do not mark the full distribution/license
   gate complete based on automated inventory alone.
