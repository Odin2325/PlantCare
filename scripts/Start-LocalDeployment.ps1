# Requires Docker Desktop running Linux containers and PowerShell 7.
[CmdletBinding()]
param([switch]$SkipBuild)
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
    if (-not (Test-Path -LiteralPath '.env.local')) {
        $databasePassword = 'Pc9!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
        @(
            'PLANTCARE_DOMAIN=localhost:8443'
            "SQL_PASSWORD=$databasePassword"
            'SMTP_HOST=mailpit'
            'SMTP_PORT=1025'
            'SMTP_USE_SSL=false'
            'SMTP_USERNAME='
            'SMTP_PASSWORD='
            'SMTP_FROM_ADDRESS=plantcare@example.test'
            'SMTP_FROM_NAME=PlantCare Local'
            'WEB_PUSH_SUBJECT='
            'WEB_PUSH_PUBLIC_KEY='
            'WEB_PUSH_PRIVATE_KEY='
            'GOOGLE_CALENDAR_ENABLED=false'
            'MICROSOFT_CALENDAR_ENABLED=false'
            'EXTERNAL_CALENDAR_SYNC_ENABLED=false'
        ) | Set-Content -LiteralPath '.env.local'
    }
    $composeArguments = @('compose', '--env-file', '.env.local', '-f', 'compose.yaml', '-f', 'compose.local.yaml', '--project-name', 'plantcare-local', 'up')
    $composeArguments += if ($SkipBuild) { '--no-build' } else { '--build' }
    $composeArguments += @('-d', '--wait', '--wait-timeout', '300')
    & docker @composeArguments
    if ($LASTEXITCODE -ne 0) { throw 'Local deployment failed. Inspect Compose service logs.' }
    Write-Output 'Local app: https://localhost:8443; captured email: http://localhost:8025'
    Write-Output 'Run scripts/Test-LocalDeployment.ps1 next; running state alone does not verify worker processing.'
}
finally {
    Pop-Location
}
