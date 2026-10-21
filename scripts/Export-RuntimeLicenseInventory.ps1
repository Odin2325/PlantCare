# Inspect shipped .deps.json manifests and their exact NuGet archives. Does not
# restore packages, execute package code or treat metadata as a legal clearance.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$DependencyManifest,
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{40}$')][string]$Candidate,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$PackageCache = (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages')
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 is required.' }
$packages = @{}
foreach ($manifestPath in $DependencyManifest) {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    foreach ($property in $manifest.libraries.PSObject.Properties) {
        if ($property.Value.type -ne 'package') { continue }
        if ($property.Name -notmatch '^([A-Za-z0-9_.-]+)/([A-Za-z0-9.+-]+)$') { throw 'Unexpected package identifier.' }
        $id = $Matches[1]; $version = $Matches[2]
        $key = "$id/$version"
        if ($packages.ContainsKey($key)) {
            if ($packages[$key].expectedHash -ne $property.Value.sha512) { throw 'Conflicting package hashes.' }
            continue
        }
        $packages[$key] = @{id=$id;version=$version;expectedHash=$property.Value.sha512}
    }
}
$records = foreach ($key in ($packages.Keys | Sort-Object)) {
    $package = $packages[$key]
    $directory = Join-Path (Join-Path $PackageCache $package.id.ToLowerInvariant()) $package.version.ToLowerInvariant()
    $archivePath = Join-Path $directory ($package.id.ToLowerInvariant()+'.'+$package.version.ToLowerInvariant()+'.nupkg')
    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) { throw "Exact package archive unavailable: $key" }
    # Signed NuGet content hashes exclude signature metadata and are not the
    # whole-archive hash. Match restore metadata to .deps, then check the archive
    # against its separate SHA512 sidecar. This is not signature verification.
    $restoreMetadata = Get-Content -LiteralPath (Join-Path $directory '.nupkg.metadata') -Raw | ConvertFrom-Json
    if (('sha512-' + $restoreMetadata.contentHash) -cne $package.expectedHash) { throw "Restore content hash does not match shipped manifest: $key" }
    $archiveHash = [Convert]::ToBase64String([Convert]::FromHexString((Get-FileHash -LiteralPath $archivePath -Algorithm SHA512).Hash))
    if ($archiveHash -cne (Get-Content -LiteralPath ($archivePath+'.sha512') -Raw).Trim()) { throw "Cached package archive checksum mismatch: $key" }
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $nuspecs = @($archive.Entries | Where-Object { $_.FullName -match '^[^/]+\.nuspec$' })
        if ($nuspecs.Count -ne 1) { throw "Unexpected package metadata structure: $key" }
        $stream = $nuspecs[0].Open()
        $settings = [Xml.XmlReaderSettings]::new()
        $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $reader = [Xml.XmlReader]::Create($stream,$settings)
        try { $xml = [Xml.XmlDocument]::new(); $xml.XmlResolver=$null; $xml.Load($reader) }
        finally { $reader.Dispose(); $stream.Dispose() }
        $metadata = $xml.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
        function Read-Metadata([string]$Name) {
            $node = $metadata.SelectSingleNode('*[local-name()="'+$Name+'"]')
            if ($node) { return $node.InnerText.Trim() }
            return $null
        }
        if ((Read-Metadata 'id') -ine $package.id -or (Read-Metadata 'version') -ne $package.version) { throw "Package metadata mismatch: $key" }
        $license = $metadata.SelectSingleNode('*[local-name()="license"]')
        $licenseType = if ($license) { $license.GetAttribute('type') } else { $null }
        $licenseValue = if ($license) { $license.InnerText.Trim() } else { $null }
        $noticeEntries = @($archive.Entries | Where-Object {
            $_.Name -match '^(?i:licen[sc]e|notice|third[._-]party[._-]notices)(?:\..*)?$' -or
            ($licenseType -eq 'file' -and $_.FullName -ceq $licenseValue)
        })
        $texts = @($noticeEntries | Sort-Object FullName | ForEach-Object {
            if ($_.Length -gt 2MB) { throw "Oversized notice requires separate review: $key" }
            $textReader = [IO.StreamReader]::new($_.Open())
            try {
                $text = $textReader.ReadToEnd().Replace("`r`n","`n")
                $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text))
                [ordered]@{path=$_.FullName;characters=$text.Length;normalizedTextSha256=[Convert]::ToHexString($hash).ToLowerInvariant()}
            }
            finally { $textReader.Dispose() }
        })
        [ordered]@{
            id=$package.id;version=$package.version
            restoreContentHash=$package.expectedHash;archiveSha512=$archiveHash
            declaredLicenseType=$licenseType;declaredLicense=$licenseValue
            licenseUrl=(Read-Metadata 'licenseUrl');projectUrl=(Read-Metadata 'projectUrl')
            authors=(Read-Metadata 'authors');copyright=(Read-Metadata 'copyright')
            packagedNotices=$texts
            reviewStatus= $(if ($texts.Count -eq 0) {'notice-text-review-required'} else {'packaged-notice-available-review-required'})
        }
    } finally { $archive.Dispose() }
}
$report = [ordered]@{
    schemaVersion=1;applicationCandidate=$Candidate
    scope='NuGet packages listed by the supplied shipped application dependency manifests; excludes shared frameworks, base-image OS packages, SQL Server, Caddy and build/test-only tools.'
    packageCount=@($records).Count;packages=@($records)
}
$json = ($report | ConvertTo-Json -Depth 12).Replace("`r`n","`n") + "`n"
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath),$json,[Text.UTF8Encoding]::new($false))
Write-Output "PASS: $(@($records).Count) runtime package content-hash associations and archive checksums matched; not signature verification or legal clearance."
