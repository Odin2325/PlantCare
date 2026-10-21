[CmdletBinding()]
param([ValidateRange(0.000001, 720)][double]$MaximumAgeHours = 30, [ValidateRange(0, 1000000)][double]$MinimumFreeGigabytes = 5, [ValidateRange(0, 1000000)][double]$MinimumSourceFreeGigabytes = 5)
. "$PSScriptRoot/LocalBackup.Common.ps1"
$problems = [Collections.Generic.List[string]]::new()
try {
    $config = Get-BackupConfiguration
    if (([IO.DriveInfo]::new([IO.Path]::GetPathRoot($config.repository))).AvailableFreeSpace -lt ($MinimumFreeGigabytes * 1GB)) { $problems.Add('low-backup-disk-space') }
    if (([IO.DriveInfo]::new([IO.Path]::GetPathRoot($BackupRoot))).AvailableFreeSpace -lt ($MinimumSourceFreeGigabytes * 1GB)) { $problems.Add('low-source-disk-space') }
    $last = Get-Content -LiteralPath (Join-Path $BackupState 'last-success.json') -Raw | ConvertFrom-Json
    if (([DateTimeOffset]::UtcNow - [DateTimeOffset]$last.atUtc).TotalHours -gt $MaximumAgeHours) { $problems.Add('backup-stale') }
    $attempt = Get-Content -LiteralPath (Join-Path $BackupState 'last-attempt.json') -Raw | ConvertFrom-Json
    if ($attempt.status -eq 'failed') { $problems.Add('last-backup-failed') }
    if ($attempt.status -eq 'running' -and ([DateTimeOffset]::UtcNow - [DateTimeOffset]$attempt.atUtc).TotalMinutes -gt 30) { $problems.Add('backup-running-over-30-minutes') }
    $snapshots = (Invoke-BackupRestic @('snapshots', '--json', '--tag', 'plantcare-local') $config.repository) -join "`n" | ConvertFrom-Json
    if (-not ($snapshots | Where-Object { $_.id -eq $last.snapshotId })) { $problems.Add('verified-snapshot-missing') }
    $retention=Join-Path $BackupState 'retention-last-attempt.json'
    if((Test-Path $retention) -and (Get-Content $retention -Raw|ConvertFrom-Json).status -eq 'failed'){$problems.Add('backup-retention-failed')}
}
catch { $problems.Add('backup-repository-or-status-unavailable') }
$status = @{ checkedAtUtc = [DateTimeOffset]::UtcNow.ToString('O'); healthy = $problems.Count -eq 0; problems = @($problems.ToArray()) }
Write-BackupStatus 'health' $status
$status | ConvertTo-Json -Compress
if ($problems.Count) { exit 1 }
