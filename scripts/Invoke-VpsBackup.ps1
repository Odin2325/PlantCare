[CmdletBinding()]
param()
. "$PSScriptRoot/VpsBackup.Common.ps1"
$lock=Enter-BackupLock
$run=[Guid]::NewGuid().ToString('N')
$scratch=Join-Path $BackupState 'scratch/vps-capture'
$owned=$false
$captureAttempted=$false
$encrypted=$false
try {
    Write-BackupStatus 'vps-last-attempt' @{status='running';atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    $repo=Get-BackupConfiguration
    $vps=Get-VpsBackupConfiguration
    $null=Invoke-BackupRestic @('snapshots','--json') $repo.repository
    foreach($path in @($repo.repository,$BackupState)){
        if(([IO.DriveInfo]::new([IO.Path]::GetPathRoot($path))).AvailableFreeSpace -lt 5GB){throw 'VPS backup source/destination requires 5 GiB free.'}
    }
    $null=Invoke-VpsSsh $vps 'sudo -n true'
    if(Test-Path -LiteralPath $scratch){throw 'Previous VPS capture scratch exists; preserve and inspect it.'}
    Protect-BackupDirectory $scratch
    $owned=$true
    $sqlScript=Join-Path $BackupState 'scratch/vps-snapshot.sql'
    [IO.File]::WriteAllText($sqlScript,$BackupSnapshotSql.Replace("`r`n","`n"),[Text.UTF8Encoding]::new($false))
    $destination=$vps.user+'@'+$vps.host+':/home/ubuntu/plantcare-staging/'
    Invoke-VpsScp $vps (Join-Path $PSScriptRoot 'Capture-VpsBackup.sh') ($destination+'backup-capture.sh')
    Invoke-VpsScp $vps $sqlScript ($destination+'backup-snapshot.sql')
    Remove-Item -LiteralPath $sqlScript
    $captureAttempted=$true
    $null=Invoke-VpsSsh $vps ("set -e; sed -i 's/\r$//' /home/ubuntu/plantcare-staging/backup-capture.sh; bash -n /home/ubuntu/plantcare-staging/backup-capture.sh; sudo -n bash /home/ubuntu/plantcare-staging/backup-capture.sh $run $($vps.candidate)")
    # Remote EXIT trap resumes writers before this network transfer/encryption.
    Invoke-VpsScp $vps ($destination+"backup-scratch/$run/.") $scratch -Recurse
    $manifest=Get-Content -LiteralPath (Join-Path $scratch 'manifest.json') -Raw|ConvertFrom-Json
    if($manifest.candidate -ne $vps.candidate){throw 'Captured VPS candidate mismatch.'}
    foreach($file in $manifest.files){
        $p=[IO.Path]::GetFullPath((Join-Path $scratch $file.name))
        if(-not $p.StartsWith($scratch+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Captured manifest path escapes private scratch.'}
        if((Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash -ne $file.sha256){throw 'VPS transfer file hash mismatch.'}
    }
    Push-Location $scratch
    try{$output=Invoke-BackupRestic @('backup','.','--host','plantcare-vps-staging','--tag','plantcare-vps-staging','--json') $repo.repository}
    finally{Pop-Location}
    $summary=$output|ForEach-Object{try{$_|ConvertFrom-Json}catch{}}|Where-Object message_type -eq summary|Select-Object -Last 1
    if($summary.snapshot_id -notmatch '^[a-f0-9]{64}$'){throw 'VPS encryption returned no valid snapshot ID.'}
    $null=Invoke-BackupRestic @('check','--read-data') $repo.repository
    $encrypted=$true
    $success=@{status='success';atUtc=[DateTimeOffset]::UtcNow.ToString('O');snapshotId=$summary.snapshot_id;repository=$repo.repository;candidate=$vps.candidate}
    Write-BackupStatus 'vps-last-success' $success
    Write-BackupStatus 'vps-last-attempt' $success
    'PASS: VPS SQL/key/config backup transferred, encrypted and repository checked; app resumed before transfer.'
} catch {
    Write-BackupStatus 'vps-last-attempt' @{status='failed';atUtc=[DateTimeOffset]::UtcNow.ToString('O');message='VPS backup failed; previous encrypted snapshots retained. Check SSH, drive and source readiness.'}
    throw
} finally {
    try {
        if($captureAttempted){
            # Preserve remote capture on failed encryption for diagnosis/retry.
            if($encrypted){$null=Invoke-VpsSsh $vps ("sudo -n bash /home/ubuntu/plantcare-staging/backup-capture.sh $run $($vps.candidate) cleanup")}
        }
        if($owned){Remove-BackupScratch $scratch}
    } catch {
        Write-BackupStatus 'vps-last-attempt' @{status='failed';atUtc=[DateTimeOffset]::UtcNow.ToString('O');message='VPS encrypted backup exists but temporary capture cleanup failed.'}
        throw
    } finally {$lock.Dispose()}
}
