[CmdletBinding()]
param([ValidateRange(1,168)][int]$MaxAgeHours=30)
. "$PSScriptRoot/VpsBackup.Common.ps1"
$problems=[Collections.Generic.List[string]]::new()
try {
    $repo=Get-BackupConfiguration
    $successPath=Join-Path $BackupState 'vps-last-success.json'
    if(-not(Test-Path -LiteralPath $successPath)){$problems.Add('vps-backup-missing')}
    else {
        $success=Get-Content -LiteralPath $successPath -Raw|ConvertFrom-Json
        if(([DateTimeOffset]::UtcNow-([DateTimeOffset]$success.atUtc)).TotalHours -gt $MaxAgeHours){$problems.Add('vps-backup-stale')}
        if($success.snapshotId -notmatch '^[a-f0-9]{64}$'){$problems.Add('vps-snapshot-invalid')}
        else {$snapshots=Invoke-BackupRestic @('snapshots','--json','--host','plantcare-vps-staging') $repo.repository|ConvertFrom-Json;if(-not($snapshots|Where-Object id -eq $success.snapshotId)){$problems.Add('vps-snapshot-missing')}}
    }
    $attemptPath=Join-Path $BackupState 'vps-last-attempt.json'
    if(Test-Path -LiteralPath $attemptPath){
        $attempt=Get-Content -LiteralPath $attemptPath -Raw|ConvertFrom-Json
        if($attempt.status -eq 'failed'){$problems.Add('vps-backup-failed')}
        if($attempt.status -eq 'running' -and ([DateTimeOffset]::UtcNow-([DateTimeOffset]$attempt.atUtc)).TotalMinutes -gt 30){$problems.Add('vps-backup-running-too-long')}
    }
    if(([IO.DriveInfo]::new([IO.Path]::GetPathRoot($repo.repository))).AvailableFreeSpace -lt 5GB){$problems.Add('vps-backup-destination-low-space')}
    if(([IO.DriveInfo]::new([IO.Path]::GetPathRoot($BackupState))).AvailableFreeSpace -lt 5GB){$problems.Add('vps-backup-source-low-space')}
    $task=Get-ScheduledTask -TaskName 'PlantCare-VpsEncryptedBackup' -ErrorAction SilentlyContinue
    if(-not $task){$problems.Add('vps-backup-task-missing')}
    elseif($task.State -eq 'Disabled'){$problems.Add('vps-backup-task-disabled')}
    elseif($task.State -ne 'Running'){
        $taskInfo=Get-ScheduledTaskInfo -TaskName 'PlantCare-VpsEncryptedBackup'
        if($taskInfo.LastTaskResult -ne 0){$problems.Add('vps-backup-task-not-successful')}
    }
} catch {$problems.Add('vps-backup-monitoring-failed')}
$result=@{healthy=($problems.Count -eq 0);problems=@($problems|Sort-Object -Unique);checkedAtUtc=[DateTimeOffset]::UtcNow.ToString('O')}
Write-BackupStatus 'vps-health' $result
$result|ConvertTo-Json -Depth 4
if(-not $result.healthy){exit 1}
