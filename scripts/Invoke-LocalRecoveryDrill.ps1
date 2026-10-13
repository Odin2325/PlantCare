# Internal helper called by Test-LocalDeployment.ps1 -IncludeCareJourney -IncludeRecovery.
# Fixed source project only. Restores into a fresh project; never overwrites source data.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][Microsoft.PowerShell.Commands.WebRequestSession]$Session,
    [Parameter(Mandatory)][Guid]$PlantId,
    [Parameter(Mandatory)][Guid]$EventId,
    [Parameter(Mandatory)][Guid]$SpeciesId,
    [Parameter(Mandatory)][string]$Email
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
if (-not $docker) {
    $docker = Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin\docker.exe'
    if (-not (Test-Path $docker)) { $docker = Join-Path $env:ProgramFiles 'Docker\Docker\resources\bin\docker.exe' }
}
$env:Path = (Split-Path $docker -Parent) + ';' + $env:Path
$runId = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$project = 'plantcare-recovery-' + $runId
$backupDir = Join-Path $repoRoot "recovery-artifacts/$runId"
$null = New-Item -ItemType Directory -Path $backupDir
$override = Join-Path $backupDir 'compose.recovery.yaml'
$compose = @('compose', '--env-file', (Join-Path $repoRoot '.env.local'), '-f', (Join-Path $repoRoot 'compose.yaml'), '-f', (Join-Path $repoRoot 'compose.local.yaml'))
$source = $compose + @('--project-name', 'plantcare-local')
$target = $compose + @('-f', $override, '--project-name', $project)

function Invoke-DockerChecked {
    param([string[]]$Arguments)
    $result = & $docker @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Docker operation failed ($($Arguments[0])). Inspect the retained recovery project; no credentials are printed." }
    return $result
}
function Invoke-Sql {
    param([string]$Container, [string]$Query)
    # Expand the existing container environment inside Linux, never on the host command line.
    return Invoke-DockerChecked @('exec', $Container, 'sh', '-c', 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -Q "$1"', 'sql', $Query)
}
function Request {
    param([string]$Origin, [string]$Path, [string]$Method = 'GET', [object]$Body, [int]$Expected = 200)
    $parameters = @{ Uri = $Origin + $Path; Method = $Method; WebSession = $Session; SkipCertificateCheck = $true; SkipHttpErrorCheck = $true; TimeoutSec = 30 }
    if ($null -ne $Body) { $parameters.ContentType = 'application/json'; $parameters.Body = $Body | ConvertTo-Json -Depth 10 }
    $response = Invoke-WebRequest @parameters
    if ([int]$response.StatusCode -ne $Expected) { throw "Recovery $Method $Path returned $($response.StatusCode), expected $Expected." }
    if ($response.Content -and $Path -notlike '/health/*') { return $response.Content | ConvertFrom-Json }
}
function Test-RecoveredJourney {
    param([string]$Origin, [string[]]$ComposeArguments)
    $null = Request $Origin '/health/ready'
    $me = Request $Origin '/api/account/me'
    if ($me.email -ne $Email) { throw 'Existing authentication cookie did not survive recovery.' }
    $history = @(Request $Origin "/api/my-plants/$PlantId/care/history")
    if ($history.Count -ne 1 -or $history[0].id -ne $EventId) { throw 'Recovered care history mismatch.' }
    $Session.Headers['X-XSRF-TOKEN'] = (Request $Origin '/api/antiforgery/token').requestToken
    $probe = Request $Origin '/api/my-plants' 'POST' @{
        plantSpeciesId = $SpeciesId; nickname = 'Recovery worker probe'; wateringIntervalDays = 7
        lastWateredAtUtc = [DateTimeOffset]::UtcNow.AddDays(-8).ToString('O')
    } -Expected 201
    $reminders = @()
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $reminders = @(Request $Origin '/api/notifications' | Where-Object userPlantId -EQ $probe.id)
        if ($reminders.Count) { break }
        Start-Sleep -Seconds 1
    }
    if ($reminders.Count -ne 1) { throw 'Recovered worker failed to create exactly one reminder.' }
    $null = Invoke-DockerChecked ($ComposeArguments + @('restart', 'worker'))
    Start-Sleep -Seconds 12 # More than two local five-second polling intervals.
    $afterRestart = @(Request $Origin '/api/notifications' | Where-Object userPlantId -EQ $probe.id)
    if ($afterRestart.Count -ne 1 -or $afterRestart[0].id -ne $reminders[0].id) { throw 'Worker duplicated or lost a reminder after restart.' }
    $null = Request $Origin "/api/my-plants/$($probe.id)" 'DELETE' -Expected 204
}

# Compare every user table's row count and aggregate checksum before starting writers.
# This is a diagnostic comparison, not a cryptographic proof of database identity.
$snapshotSql = @'
USE [PlantCare]; SET NOCOUNT ON;
CREATE TABLE #state (TableName nvarchar(300), [RowCount] bigint, RowChecksum int);
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'INSERT #state SELECT N''' + REPLACE(s.name + N'.' + t.name, '''', '''''') + N''', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';'
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0;
EXEC sp_executesql @sql;
SELECT TableName, [RowCount], COALESCE(RowChecksum,0) FROM #state ORDER BY TableName;
'@
$sourceStopped = $false
$success = $false
Push-Location $repoRoot
try {
    $apiImage = (Invoke-DockerChecked @('inspect', '--format', '{{.Image}}', 'plantcare-local-api-1')).ToString().Trim()
    $workerImage = (Invoke-DockerChecked @('inspect', '--format', '{{.Image}}', 'plantcare-local-worker-1')).ToString().Trim()
    $databaseImage = (Invoke-DockerChecked @('inspect', '--format', '{{.Image}}', 'plantcare-local-database-1')).ToString().Trim()
    foreach ($image in @($apiImage, $workerImage, $databaseImage)) {
        $null = Invoke-DockerChecked @('image', 'inspect', '--format', '{{.Id}}', $image)
    }
    @"
services:
  database:
    image: $databaseImage
    pull_policy: never
    ports: !override []
  api:
    image: $apiImage
    pull_policy: never
  migrate:
    image: $apiImage
    pull_policy: never
  worker:
    image: $workerImage
    pull_policy: never
  proxy:
    environment:
      PLANTCARE_DOMAIN: localhost:9443
    ports: !override
      - "127.0.0.1:9443:9443"
  mailpit:
    ports: !override []
"@ | Set-Content -LiteralPath $override
    $sourceStopped = $true
    $null = Invoke-DockerChecked ($source + @('stop', 'api', 'worker'))
    $before = @(Invoke-Sql 'plantcare-local-database-1' $snapshotSql)
    $before | Set-Content (Join-Path $backupDir 'database-state.txt')
    $null = Invoke-Sql 'plantcare-local-database-1' "BACKUP DATABASE [PlantCare] TO DISK=N'/var/opt/mssql/plantcare-$runId.bak' WITH COPY_ONLY, CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/plantcare-$runId.bak' WITH CHECKSUM;"
    $null = Invoke-DockerChecked @('cp', "plantcare-local-database-1:/var/opt/mssql/plantcare-$runId.bak", (Join-Path $backupDir 'database.bak'))
    $null = Invoke-DockerChecked ($source + @('run', '--rm', '--no-deps', '--user', 'root', '--entrypoint', 'sh', '-v', "${backupDir}:/recovery", 'api', '-c', 'tar -cf /recovery/keys.tar -C /var/lib/plantcare/keys .'))
    $keyHashes = @(Invoke-DockerChecked ($source + @('run', '--rm', '--no-deps', '--user', 'root', '--entrypoint', 'sh', 'api', '-c', 'cd /var/lib/plantcare/keys && sha256sum key-*.xml')) | Where-Object { "$_" -match '^[0-9a-f]{64}\s+key-' })
    if (-not $keyHashes.Count) { throw 'No data-protection keys were found to back up.' }
    $keyHashes | Set-Content (Join-Path $backupDir 'key-hashes.txt')
    @{
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        candidate = (& git rev-parse HEAD).Trim()
        apiImage = $apiImage; workerImage = $workerImage; databaseImage = $databaseImage; recoveryProject = $project
        files = @(Get-FileHash (Join-Path $backupDir 'database.bak'), (Join-Path $backupDir 'keys.tar') -Algorithm SHA256 | ForEach-Object { @{ name = Split-Path $_.Path -Leaf; sha256 = $_.Hash } })
    } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $backupDir 'manifest.json')
    $null = Invoke-DockerChecked ($source + @('restart', 'database'))
    $null = Invoke-DockerChecked ($source + @('up', '-d', '--no-build', '--wait', '--wait-timeout', '300'))
    $sourceStopped = $false
    Test-RecoveredJourney 'https://localhost:8443' $source
    Write-Output 'PASS: source database/API restart, existing login/history, worker recovery and reminder deduplication.'

    $null = Invoke-DockerChecked ($target + @('up', '-d', '--no-build', '--wait', '--wait-timeout', '300', 'database'))
    $database = "$project-database-1"
    $null = Invoke-DockerChecked @('cp', (Join-Path $backupDir 'database.bak'), "${database}:/var/opt/mssql/recovery.bak")
    # A fresh isolated instance has no PlantCare database or conflicting file paths.
    # No REPLACE is used: an unexpected existing database must fail safely.
    $null = Invoke-Sql $database "IF DB_ID(N'PlantCare') IS NOT NULL THROW 51000, 'Restore target is not empty.', 1; RESTORE DATABASE [PlantCare] FROM DISK=N'/var/opt/mssql/recovery.bak' WITH CHECKSUM, RECOVERY; DBCC CHECKDB ([PlantCare]) WITH NO_INFOMSGS;"
    $restored = @(Invoke-Sql $database $snapshotSql)
    if (Compare-Object $before $restored) { throw 'Restored database row counts/checksums differ from backup snapshot.' }
    $null = Invoke-DockerChecked ($target + @('run', '--rm', '--no-deps', '--user', 'root', '--entrypoint', 'sh', '-v', "${backupDir}:/recovery:ro", 'api', '-c', 'tar -xf /recovery/keys.tar -C /var/lib/plantcare/keys && chown -R app:app /var/lib/plantcare/keys && chmod 700 /var/lib/plantcare/keys'))
    $restoredKeys = @(Invoke-DockerChecked ($target + @('run', '--rm', '--no-deps', '--user', 'root', '--entrypoint', 'sh', 'api', '-c', 'cd /var/lib/plantcare/keys && sha256sum key-*.xml')) | Where-Object { "$_" -match '^[0-9a-f]{64}\s+key-' })
    if (Compare-Object $keyHashes $restoredKeys) { throw 'Restored data-protection key hashes differ.' }
    # SQL logins live in master, outside the restored application backup. Map
    # restored database users to fresh logins using the matching private config.
    $null = Invoke-DockerChecked ($target + @('run', '--rm', '--no-deps', 'migrate', '--provision-runtime-users'))
    $null = Invoke-DockerChecked ($target + @('up', '-d', '--no-build', '--wait', '--wait-timeout', '300'))
    Test-RecoveredJourney 'https://localhost:9443' $target
    Write-Output 'PASS: isolated SQL restore/CHECKDB, all table counts/checksums, exact key hashes, existing cookie/history, restored worker and deduplication.'
    'PASS: restart and isolated database/key-ring recovery; real OAuth provider recovery NOT RUN.' | Set-Content (Join-Path $backupDir 'result.txt')
    $success = $true
} finally {
    if ($sourceStopped) { $null = Invoke-DockerChecked ($source + @('up', '-d', '--no-build', '--wait', '--wait-timeout', '300')); $sourceStopped = $false }
    if ($success) {
        # Only this randomly named drill project is removed; source volumes and backups remain.
        $null = Invoke-DockerChecked ($target + @('down', '--volumes'))
        Write-Output "Recovery evidence retained privately in recovery-artifacts/$runId; temporary restore stack removed."
    } else {
        Write-Output "Recovery failed; backup/evidence and project $project retained for diagnosis."
    }
    Pop-Location
}
