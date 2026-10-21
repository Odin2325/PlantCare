# Preview is the default. Enabling requires the exact repository-state hash.
[CmdletBinding()]
param([switch]$Enable,[ValidatePattern('^[a-f0-9]{64}$')][string]$ExpectedRepositoryStateHash)
. "$PSScriptRoot/LocalBackup.Common.ps1"
. "$PSScriptRoot/BackupRetention.Common.ps1"
$lock=Enter-BackupLock
try {
    $plan=Get-CurrentBackupRetentionPlan
    Write-BackupStatus 'retention-plan' $plan
    if($Enable){
        if(-not $ExpectedRepositoryStateHash -or $ExpectedRepositoryStateHash -ne $plan.repositoryStateHash){throw 'Review the current plan and supply its exact repository-state hash.'}
        Apply-BackupRetentionPlan $plan
        Write-BackupStatus 'retention-policy' @{enabled=$true;days=30;enabledAtUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    }
    $policyFile=Join-Path $BackupState 'retention-policy.json'
    $enabled=(Test-Path $policyFile) -and (Get-Content $policyFile -Raw|ConvertFrom-Json).enabled -eq $true
    @{enabled=[bool]$enabled;days=30;totalSnapshots=$plan.totalSnapshots;expiredSnapshots=@($plan.expireIds).Count;repositoryStateHash=$plan.repositoryStateHash}|ConvertTo-Json -Compress
} finally {$lock.Dispose()}
