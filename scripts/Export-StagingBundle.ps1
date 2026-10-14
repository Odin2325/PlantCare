# Build committed source only; never package the working directory or local data.
[CmdletBinding()]
param([ValidatePattern('^(HEAD|[a-fA-F0-9]{40})$')][string]$Candidate = 'HEAD')

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 is required.' }
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
$scratch = $null
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        foreach ($path in @(
            (Join-Path $env:LOCALAPPDATA 'Programs/DockerDesktop/resources/bin/docker.exe'),
            (Join-Path $env:ProgramFiles 'Docker/Docker/resources/bin/docker.exe')
        )) {
            if (Test-Path -LiteralPath $path) { $env:Path = (Split-Path $path -Parent) + ';' + $env:Path; break }
        }
    }
    $sha = (& git rev-parse --verify "$Candidate^{commit}").Trim()
    if ($LASTEXITCODE -ne 0 -or $sha -notmatch '^[a-f0-9]{40}$') { throw 'Candidate commit was not found.' }
    $engine = & docker info --format '{{.OSType}}'
    if ($LASTEXITCODE -ne 0 -or $engine -ne 'linux') { throw 'Docker with Linux containers must be running.' }
    $bundleRoot = Join-Path $repoRoot '.operations-local/deployment-bundles'
    $destination = Join-Path $bundleRoot $sha
    if (Test-Path -LiteralPath $destination) { throw 'Bundle already exists. Preserve it; choose another committed candidate.' }
    $null = New-Item -ItemType Directory -Path $bundleRoot -Force
    $scratch = Join-Path $bundleRoot ('scratch-' + [Guid]::NewGuid().ToString('N'))
    $source = Join-Path $scratch 'source'
    $payload = Join-Path $scratch 'bundle'
    $null = New-Item -ItemType Directory -Path $source, $payload
    $archive = Join-Path $scratch 'source.tar'
    & git archive --format=tar --output=$archive $sha
    if ($LASTEXITCODE -ne 0) { throw 'Committed source export failed.' }
    & tar -xf $archive -C $source
    if ($LASTEXITCODE -ne 0) { throw 'Committed source extraction failed.' }

    $api = "plantcare-api:$sha"
    $worker = "plantcare-worker:$sha"
    foreach ($build in @(@{target='api';tag=$api}, @{target='worker';tag=$worker})) {
        & docker build --platform linux/amd64 --target $build.target --tag $build.tag $source
        if ($LASTEXITCODE -ne 0) { throw "Image build failed: $($build.target)." }
    }
    $images = @()
    foreach ($tag in @($api, $worker)) {
        $description = & docker image inspect --format '{{.Id}} {{.Os}} {{.Architecture}}' $tag
        if ($LASTEXITCODE -ne 0 -or $description -notmatch '^(sha256:[a-f0-9]{64}) linux amd64$') {
            throw 'Export requires Linux amd64 application images.'
        }
        $images += @{tag=$tag;id=$Matches[1]}
    }
    & docker image save --output (Join-Path $payload 'images.tar') $api $worker
    if ($LASTEXITCODE -ne 0) { throw 'Image archive export failed.' }

    # Fail closed if the committed Compose build structure changes. The runtime
    # bundle has no build context, source tree or fallback to local image tags.
    $compose = Get-Content -LiteralPath (Join-Path $source 'compose.yaml') -Raw
    $buildPattern = '(?m)^    build:\r?\n      context: \.\r?\n      target: (?:api|worker)\r?\n'
    if ([regex]::Matches($compose, $buildPattern).Count -ne 2) { throw 'Compose build structure changed; review exporter.' }
    $compose = [regex]::Replace($compose, $buildPattern, '')
    if ($compose -match '(?m)^\s+build:') { throw 'Runtime Compose must not contain build instructions.' }
    $compose = $compose.Replace('${PLANTCARE_API_IMAGE:-plantcare-local-api}', '${PLANTCARE_API_IMAGE:?Set PLANTCARE_API_IMAGE from images.env}')
    $compose = $compose.Replace('${PLANTCARE_WORKER_IMAGE:-plantcare-local-worker}', '${PLANTCARE_WORKER_IMAGE:?Set PLANTCARE_WORKER_IMAGE from images.env}')
    Set-Content -LiteralPath (Join-Path $payload 'compose.yaml') -Value $compose -Encoding utf8NoBOM
    Copy-Item -LiteralPath (Join-Path $source '.env.example') -Destination (Join-Path $payload 'environment.example')
    $null = New-Item -ItemType Directory -Path (Join-Path $payload 'deploy')
    foreach ($file in @('Caddyfile', 'catalog.example.json')) {
        Copy-Item -LiteralPath (Join-Path $source "deploy/$file") -Destination (Join-Path $payload "deploy/$file")
    }
    Set-Content -LiteralPath (Join-Path $payload 'images.env') -Encoding utf8NoBOM -Value @(
        "PLANTCARE_API_IMAGE=$api", "PLANTCARE_WORKER_IMAGE=$worker"
    )
    @{candidate=$sha;platform='linux/amd64';createdAtUtc=[DateTimeOffset]::UtcNow.ToString('O');images=$images} |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $payload 'manifest.json') -Encoding utf8NoBOM
    $checksums = foreach ($file in @('images.tar', 'compose.yaml', 'environment.example', 'images.env', 'manifest.json', 'deploy/Caddyfile', 'deploy/catalog.example.json')) {
        (Get-FileHash -LiteralPath (Join-Path $payload $file) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $file
    }
    # Linux sha256sum expects LF line endings, including when exported on Windows.
    [IO.File]::WriteAllText((Join-Path $payload 'SHA256SUMS'), ($checksums -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $payload -Destination $destination
    Write-Output "Bundle ready: $destination"
    Write-Output "Candidate: $sha. Images are built, not accepted as a public deployment."
} finally {
    if ($scratch -and (Test-Path -LiteralPath $scratch)) {
        $resolved = [IO.Path]::GetFullPath($scratch)
        $expectedParent = [IO.Path]::GetFullPath((Join-Path $repoRoot '.operations-local/deployment-bundles'))
        if ([IO.Path]::GetDirectoryName($resolved) -ne $expectedParent -or
            [IO.Path]::GetFileName($resolved) -notmatch '^scratch-[a-f0-9]{32}$') {
            throw 'Refusing cleanup outside this exporter scratch directory.'
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    Pop-Location
}
