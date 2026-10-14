# Bounded private-state probes. No remote capture, email or task changes.
[CmdletBinding()]
param()
. "$PSScriptRoot/VpsBackup.Common.ps1"
$lock=Enter-BackupLock
$names=@('vps-last-success','vps-last-attempt','vps-backup-config')
$saved=@{}
$probe=Join-Path $BackupState 'vps-failure-probe.lock'
$marker=$null
$scratch=Join-Path $BackupState ('scratch/vps-health-probe-'+[Guid]::NewGuid().ToString('N'))
function Read-Problems {
    $output=& pwsh -NoProfile -NonInteractive -File (Join-Path $PSScriptRoot 'Test-VpsBackupHealth.ps1')
    $result=($output -join "`n")|ConvertFrom-Json
    return @($result.problems)
}
function Expect-Problem([string]$Code) {
    if($Code -notin @(Read-Problems)){throw "VPS health probe missed $Code."}
    Write-Output "PASS: $Code."
}
try {
    $marker=[IO.File]::Open($probe,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    foreach($name in $names){$saved[$name]=[IO.File]::ReadAllText((Join-Path $BackupState ($name+'.json')))}
    if(@(Read-Problems).Count){throw 'VPS backup health must be healthy before probes.'}
    $success=$saved['vps-last-success']|ConvertFrom-Json
    $success.atUtc=[DateTimeOffset]::UtcNow.AddHours(-31).ToString('O')
    Write-BackupStatus 'vps-last-success' $success
    Expect-Problem 'vps-backup-stale'
    $success=$saved['vps-last-success']|ConvertFrom-Json
    $success.snapshotId='0'*64
    Write-BackupStatus 'vps-last-success' $success
    Expect-Problem 'vps-snapshot-missing'
    [IO.File]::WriteAllText((Join-Path $BackupState 'vps-last-success.json'),$saved['vps-last-success'])
    Write-BackupStatus 'vps-last-attempt' @{status='failed';atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    Expect-Problem 'vps-backup-failed'
    Write-BackupStatus 'vps-last-attempt' @{status='running';atUtc=[DateTimeOffset]::UtcNow.AddMinutes(-31).ToString('O')}
    Expect-Problem 'vps-backup-running-too-long'
    Write-BackupStatus 'vps-last-attempt' @{status='running';atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    if(@(Read-Problems).Count){throw 'An active recent VPS backup was incorrectly classified as failed.'}
    'PASS: recent active backup is not a failure.'
    [IO.File]::WriteAllText((Join-Path $BackupState 'vps-last-attempt.json'),$saved['vps-last-attempt'])
    Protect-BackupDirectory $scratch
    $runner=Join-Path $scratch 'running-task.ps1'
    @"
function Get-ScheduledTask { param(`$TaskName) [pscustomobject]@{State='Running'} }
function Get-ScheduledTaskInfo { param(`$TaskName) [pscustomobject]@{LastTaskResult=267009} }
. '$PSScriptRoot/Test-VpsBackupHealth.ps1'
"@|Set-Content -LiteralPath $runner
    $output=& pwsh -NoProfile -NonInteractive -File $runner
    if($LASTEXITCODE -ne 0 -or -not(($output -join "`n"|ConvertFrom-Json).healthy)){throw 'Running task 267009 was incorrectly treated as failure.'}
    'PASS: running Windows task result 267009 is not a failure.'
    $c=$saved['vps-backup-config']|ConvertFrom-Json
    $c.identityFile=Join-Path $scratch 'nonexistent-key'
    Write-BackupStatus 'vps-backup-config' $c
    $rejected=$false
    try{$null=Get-VpsBackupConfiguration}catch{$rejected=$true}
    if(-not $rejected){throw 'Missing SSH identity accepted.'}
    'PASS: missing SSH identity rejected before remote capture.'
    $c=$saved['vps-backup-config']|ConvertFrom-Json
    $c.host='127.0.0.1'
    Write-BackupStatus 'vps-backup-config' $c
    $rejected=$false
    try{$null=Get-VpsBackupConfiguration}catch{$rejected=$true}
    if(-not $rejected){throw 'Unexpected VPS target accepted.'}
    'PASS: unexpected SSH target rejected before remote capture.'
} finally {
    try {
        foreach($name in $saved.Keys){[IO.File]::WriteAllText((Join-Path $BackupState ($name+'.json')),$saved[$name])}
        if(Test-Path -LiteralPath $scratch){Remove-BackupScratch $scratch}
    } finally {
        if($marker){$marker.Dispose();Remove-Item -LiteralPath $probe}
        $lock.Dispose()
    }
}
$result=Read-Problems
if(@($result).Count){throw 'VPS health did not recover after private-state probes.'}
'PASS: original VPS backup state restored; health is healthy.'
