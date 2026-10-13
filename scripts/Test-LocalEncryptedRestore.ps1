# Decrypt into fresh private scratch and restore SQL into a disposable container.
[CmdletBinding()]
param()
. "$PSScriptRoot/LocalBackup.Common.ps1"
$lock = Enter-BackupLock
$runId = [Guid]::NewGuid().ToString('N')
$scratch = Join-Path $BackupState "scratch/restore-$runId"
$project = 'plantcare-backup-verify-' + $runId.Substring(0, 12)
$target = @()
$targetStarted = $false
$passed = $false
try {
    $config = Get-BackupConfiguration
    $last = Get-Content -LiteralPath (Join-Path $BackupState 'last-success.json') -Raw | ConvertFrom-Json
    if ($last.snapshotId -notmatch '^[a-f0-9]{64}$') { throw 'Invalid recorded snapshot ID.' }
    $null = New-Item -ItemType Directory -Path $scratch -Force
    $null = Invoke-BackupRestic @('restore', $last.snapshotId, '--target', $scratch, '--verify') $config.repository
    $manifest = Get-Content -LiteralPath (Join-Path $scratch 'manifest.json') -Raw | ConvertFrom-Json
    foreach ($file in $manifest.files) {
        $path = [IO.Path]::GetFullPath((Join-Path $scratch $file.name))
        if (-not $path.StartsWith($scratch + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path escapes restored scratch.' }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256) { throw 'Decrypted snapshot file hash mismatch.' }
    }
    if ($manifest.images.database -notmatch '^sha256:[a-f0-9]{64}$') { throw 'Invalid recorded SQL image.' }
    $override = Join-Path $scratch 'compose.verify.yaml'
    @"
services:
  database:
    image: $($manifest.images.database)
    pull_policy: never
    ports: !override []
"@ | Set-Content -LiteralPath $override
    $target = @('compose', '--env-file', (Join-Path $scratch '.env.local'), '-f', (Join-Path $scratch 'compose.yaml'), '-f', $override, '--project-name', $project)
    $targetStarted = $true
    $null = Invoke-BackupDocker ($target + @('up', '-d', '--no-build', '--wait', '--wait-timeout', '300', 'database'))
    $container = "$project-database-1"
    $null = Invoke-BackupDocker @('cp', (Join-Path $scratch 'database.bak'), "${container}:/var/opt/mssql/recovery.bak")
    $null = Invoke-BackupSql $container "IF DB_ID(N'PlantCare') IS NOT NULL THROW 51000, 'Restore target is not empty.', 1; RESTORE DATABASE [PlantCare] FROM DISK=N'/var/opt/mssql/recovery.bak' WITH CHECKSUM, RECOVERY; DBCC CHECKDB ([PlantCare]) WITH NO_INFOMSGS;"
    $actual = @(Invoke-BackupSql $container $BackupSnapshotSql)
    $expected = @(Get-Content -LiteralPath (Join-Path $scratch 'database-state.txt'))
    if (Compare-Object $expected $actual) { throw 'Decrypted SQL restore table counts/checksums differ.' }
    $passed = $true
    Write-BackupStatus 'last-restore-test' @{ status = 'success'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); snapshotId = $last.snapshotId; checks = @('restic-verify', 'all-file-sha256', 'sql-restore', 'checkdb', 'all-table-counts-checksums') }
    Write-Output 'PASS: encrypted snapshot restored; SQL CHECKDB/table comparisons and matching key/config file hashes passed.'
}
finally {
    try {
        if ($targetStarted) { $null = Invoke-BackupDocker ($target + @('down', '--volumes')) }
        Remove-BackupScratch $scratch
        if (-not $passed) { Write-BackupStatus 'last-restore-test' @{ status = 'failed'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); message = 'Encrypted restore verification failed.' } }
    }
    finally { $lock.Dispose() }
}
