# Restore only to a fresh, unpublished local Docker database. Never the VPS/source.
[CmdletBinding()]
param()
. "$PSScriptRoot/VpsBackup.Common.ps1"
$lock=Enter-BackupLock
$run=[Guid]::NewGuid().ToString('N')
$scratch=Join-Path $BackupState "scratch/vps-restore-$run"
$project='plantcare-vps-verify-'+$run.Substring(0,12)
$target=@()
$started=$false
$passed=$false
try {
    $repo=Get-BackupConfiguration
    $last=Get-Content -LiteralPath (Join-Path $BackupState 'vps-last-success.json') -Raw|ConvertFrom-Json
    if($last.snapshotId -notmatch '^[a-f0-9]{64}$'){throw 'Invalid VPS snapshot ID.'}
    $snapshots=Invoke-BackupRestic @('snapshots','--json','--host','plantcare-vps-staging') $repo.repository|ConvertFrom-Json
    if(-not($snapshots|Where-Object id -eq $last.snapshotId)){throw 'Snapshot does not belong to the VPS backup host.'}
    Protect-BackupDirectory $scratch
    $null=Invoke-BackupRestic @('restore',$last.snapshotId,'--target',$scratch,'--verify') $repo.repository
    $manifest=Get-Content -LiteralPath (Join-Path $scratch 'manifest.json') -Raw|ConvertFrom-Json
    foreach($file in $manifest.files){
        $path=[IO.Path]::GetFullPath((Join-Path $scratch $file.name))
        if(-not $path.StartsWith($scratch+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Restored manifest escapes private scratch.'}
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256){throw 'Restored VPS file hash mismatch.'}
    }
    $image=$manifest.images.databaseReference
    if($image -notmatch '^mcr\.microsoft\.com/mssql/server@sha256:[a-f0-9]{64}$'){throw 'Restore requires the captured official SQL Server image digest.'}
    $null=Invoke-BackupDocker @('pull',$image)
    $override=Join-Path $scratch 'compose.verify.yaml'
    @"
services:
  database:
    image: $image
    pull_policy: never
    ports: !override []
"@|Set-Content -LiteralPath $override
    $target=@('compose','--env-file',(Join-Path $scratch 'config/.env'),'--env-file',(Join-Path $scratch 'config/images.env'),'-f',(Join-Path $scratch 'config/compose.yaml'),'-f',(Join-Path $scratch 'config/compose.budget.yaml'),'-f',$override,'--project-name',$project)
    $started=$true
    $null=Invoke-BackupDocker ($target+@('up','-d','--no-build','--wait','--wait-timeout','300','database'))
    $container="$project-database-1"
    $null=Invoke-BackupDocker @('cp',(Join-Path $scratch 'database.bak'),"${container}:/var/opt/mssql/recovery.bak")
    $null=Invoke-BackupSql $container "IF DB_ID(N'PlantCare') IS NOT NULL THROW 51000, 'Restore target is not empty.', 1; RESTORE DATABASE [PlantCare] FROM DISK=N'/var/opt/mssql/recovery.bak' WITH CHECKSUM,RECOVERY; DBCC CHECKDB ([PlantCare]) WITH NO_INFOMSGS;"
    $actual=@(Invoke-BackupSql $container $BackupSnapshotSql)
    $expected=@(Get-Content -LiteralPath (Join-Path $scratch 'database-state.txt'))
    if(Compare-Object $expected $actual){throw 'Restored VPS table counts/checksums differ.'}
    $passed=$true
    Write-BackupStatus 'vps-last-restore-test' @{status='success';atUtc=[DateTimeOffset]::UtcNow.ToString('O');snapshotId=$last.snapshotId;checks=@('restic-verify','all-file-sha256','sql-restore','checkdb','all-table-counts-checksums','key-config-hashes')}
    'PASS: VPS encrypted snapshot decrypted; isolated SQL restore/CHECKDB/table comparisons and matching key/config hashes passed.'
} finally {
    try {
        if($started){
            if($project -notmatch '^plantcare-vps-verify-[a-f0-9]{12}$'){throw 'Refusing cleanup of an unexpected Docker project.'}
            $null=Invoke-BackupDocker ($target+@('down','--volumes'))
        }
        Remove-BackupScratch $scratch
        if(-not $passed){Write-BackupStatus 'vps-last-restore-test' @{status='failed';atUtc=[DateTimeOffset]::UtcNow.ToString('O');message='VPS encrypted restore verification failed.'}}
    } finally{$lock.Dispose()}
}
