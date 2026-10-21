[CmdletBinding()]
param()
. "$PSScriptRoot/LocalBackup.Common.ps1"
$lock = Enter-BackupLock
$runId = [Guid]::NewGuid().ToString('N')
$scratch = Join-Path $BackupState 'scratch/capture'
$sqlFile = "/var/opt/mssql/plantcare-scheduled-$runId.bak"
$stopped = $false
$captured = $false
$ownedScratch = $false
try {
    $config = Get-BackupConfiguration
    # Repository and key must already be usable before stopping application writers.
    $null = Invoke-BackupRestic @('snapshots', '--json') $config.repository
    if (([IO.DriveInfo]::new([IO.Path]::GetPathRoot($config.repository))).AvailableFreeSpace -lt 5GB) { throw 'Backup destination has less than 5 GiB free.' }
    $null = Invoke-BackupDocker @('info', '--format', '{{.ServerVersion}}')
    $images = @{}
    foreach ($service in @('api', 'worker', 'database')) {
        $container = "plantcare-local-$service-1"
        if ((Invoke-BackupDocker @('inspect', '--format', '{{.State.Running}}', $container)).ToString().Trim() -ne 'true') { throw 'Local source services must be running before backup.' }
        $images[$service] = (Invoke-BackupDocker @('inspect', '--format', '{{.Image}}', $container)).ToString().Trim()
    }
    Write-BackupStatus 'last-attempt' @{ status = 'running'; atUtc = [DateTimeOffset]::UtcNow.ToString('O') }
    if (Test-Path -LiteralPath $scratch) { throw 'A previous capture scratch directory remains; inspect it before retrying.' }
    $null = New-Item -ItemType Directory -Path $scratch
    $ownedScratch = $true
    $stopped = $true
    $null = Invoke-BackupDocker ($BackupCompose + @('stop', 'api', 'worker'))
    Invoke-BackupSql 'plantcare-local-database-1' $BackupSnapshotSql | Set-Content -LiteralPath (Join-Path $scratch 'database-state.txt')
    $null = Invoke-BackupSql 'plantcare-local-database-1' "BACKUP DATABASE [PlantCare] TO DISK=N'$sqlFile' WITH COPY_ONLY, CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$sqlFile' WITH CHECKSUM;"
    $null = Invoke-BackupDocker @('cp', "plantcare-local-database-1:$sqlFile", (Join-Path $scratch 'database.bak'))
    $null = Invoke-BackupDocker ($BackupCompose + @('run', '--rm', '--no-deps', '--user', 'root', '--entrypoint', 'sh', '-v', "${scratch}:/recovery", 'api', '-c', 'test -n "$(find /var/lib/plantcare/keys -name ''key-*.xml'' -print -quit)" && tar -cf /recovery/keys.tar -C /var/lib/plantcare/keys .'))
    foreach ($file in @('.env.local', 'compose.yaml', 'compose.local.yaml', 'deploy/Caddyfile')) {
        $target = Join-Path $scratch $file
        $null = New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force
        Copy-Item -LiteralPath (Join-Path $BackupRoot $file) -Destination $target
    }
    $files = @(Get-ChildItem -LiteralPath $scratch -File -Recurse | ForEach-Object { @{ name = [IO.Path]::GetRelativePath($scratch, $_.FullName); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } })
    @{ createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O'); candidate = (& git -C $BackupRoot rev-parse HEAD).Trim(); images = $images; files = $files } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $scratch 'manifest.json')
    $captured = $true
}
catch {
    Write-BackupStatus 'last-attempt' @{ status = 'failed'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); message = 'Capture/preflight failed; previous encrypted snapshots retained.' }
    throw
}
finally {
    try {
        if ($stopped) { $null = Invoke-BackupDocker ($BackupCompose + @('up', '--no-build', '-d', '--wait', '--wait-timeout', '300', 'api', 'worker')) }
    }
    catch { $captured = $false; Write-BackupStatus 'last-attempt' @{ status = 'failed'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); message = 'Application restart failed after backup capture; inspect local services.' }; throw }
    finally {
        if (-not $captured) {
            try {
                if ($stopped) { $null = Invoke-BackupDocker @('exec', 'plantcare-local-database-1', 'rm', '-f', '--', $sqlFile) }
            }
            finally { try { if ($ownedScratch) { Remove-BackupScratch $scratch } } finally { $lock.Dispose() } }
        }
    }
}
try {
    # Writers resume before repository encryption/checking. Fixed logical path
    # keeps snapshots grouped and makes restores independent of random run IDs.
    Push-Location $scratch
    try { $output = Invoke-BackupRestic @('backup', '.', '--host', 'plantcare-local', '--tag', 'plantcare-local', '--json') $config.repository }
    finally { Pop-Location }
    $summary = $output | ForEach-Object { try { $_ | ConvertFrom-Json } catch {} } | Where-Object message_type -eq 'summary' | Select-Object -Last 1
    if (-not $summary.snapshot_id) { throw 'Backup returned no snapshot ID.' }
    $null = Invoke-BackupRestic @('check', '--read-data') $config.repository
    $success = @{ status = 'success'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); snapshotId = $summary.snapshot_id; repository = $config.repository }
    Write-BackupStatus 'last-success' $success
    Write-BackupStatus 'last-attempt' $success
    Invoke-ConfiguredBackupRetention
    Write-Output 'PASS: SQL/key/config snapshot encrypted and repository data checked; writers restarted.'
}
catch {
    Write-BackupStatus 'last-attempt' @{ status = 'failed'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); message = 'Encryption/repository verification failed; previous snapshots retained.' }
    throw
}
finally {
    try {
        # Delete only this generated SQL file and private scratch directory.
        if ($sqlFile -notmatch '^/var/opt/mssql/plantcare-scheduled-[a-f0-9]{32}\.bak$') { throw 'Invalid SQL cleanup path.' }
        $null = Invoke-BackupDocker @('exec', 'plantcare-local-database-1', 'rm', '-f', '--', $sqlFile)
        Remove-BackupScratch $scratch
    }
    catch {
        Write-BackupStatus 'last-attempt' @{ status = 'failed'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); message = 'Backup completed but temporary-file cleanup failed; inspect private scratch and SQL backup files.' }
        throw
    }
    finally { $lock.Dispose() }
}
