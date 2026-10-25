# PowerShell 7 and Docker Desktop/Linux containers. Uses only the local stack.
[CmdletBinding()]
param([switch]$SkipBuild, [switch]$CatalogOnly)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        $dockerBin = Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin'
        if (-not (Test-Path (Join-Path $dockerBin 'docker.exe'))) {
            $dockerBin = Join-Path $env:ProgramFiles 'Docker\Docker\resources\bin'
        }
        $env:Path = $dockerBin + ';' + $env:Path
    }
    $null = Invoke-WebRequest 'https://localhost:8443/health/ready' -SkipCertificateCheck -TimeoutSec 30
    if (-not $SkipBuild) {
        docker build --tag plantcare-browser-tests:local tests/browser
        if ($LASTEXITCODE -ne 0) { throw 'Browser test image build failed.' }
    }
    # Operator import is transactional/repeatable and does not promote test users.
    $importOutput = docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local run --rm --no-deps migrate --import-catalog /operator/catalog.example.json 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Browser catalog fixture import failed; inspect local operator logs.' }
    $reviewedImport = docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local run --rm --no-deps migrate --upsert-catalog /operator/catalog.houseplants.json 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Reviewed houseplant catalog import failed; inspect local operator logs.' }
    # Share the proxy network namespace: both email URLs and the browser retain
    # the real https://localhost:8443 origin. Mailpit resolves on the Compose network.
    $browserArguments = @('run', '--rm', '--init', '--ipc=host', '--network', 'container:plantcare-local-proxy-1', 'plantcare-browser-tests:local')
    if ($CatalogOnly) {
        $browserArguments += @('npx', 'playwright', 'test', 'catalog.spec.ts', 'garden.spec.ts', '--grep', 'garden collection|garden catalog|catalog light filters combine|comparison selection survives')
    }
    docker @browserArguments
    if ($LASTEXITCODE -ne 0) { throw 'Local browser journey failed.' }
    if ($CatalogOnly) {
        Write-Output 'PASS: focused garden catalog, category/navigation/comparison and accessibility/reflow checks across desktop/mobile Chromium and Firefox. Full account/reminder/PWA suite not run.'
    } else {
        Write-Output 'PASS: Chromium desktop/mobile and Firefox core journeys, axe/keyboard/reflow checks and real-worker PWA offline/update/failure checks.'
    }
    Write-Output 'Synthetic accounts, messages and archived plants remain in Docker only; no traces or credentials are exported.'
}
finally { Pop-Location }
