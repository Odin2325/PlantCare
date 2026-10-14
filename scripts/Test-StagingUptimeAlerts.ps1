# Fixed synthetic staging only. No database/config/firewall changes.
[CmdletBinding()]
param([ValidateRange(6,10)][int]$OutageMinutes=6)
. "$PSScriptRoot/VpsBackup.Common.ps1"
$vps=Get-VpsBackupConfiguration
$lock=Enter-BackupLock
$unit='plantcare-uptime-recovery-'+[Guid]::NewGuid().ToString('N')
$armed=$false
$stopped=$false
$startedAt=[DateTimeOffset]::UtcNow.ToString('O')
function Get-StagingReadiness {
    try {return [int](Invoke-WebRequest 'https://staging.plantacare.org/health/ready' -TimeoutSec 15 -SkipHttpErrorCheck).StatusCode}
    catch {return 0}
}
try {
    if((Get-StagingReadiness) -ne 200){throw 'Staging must be healthy before the outage drill.'}
    $null=Invoke-VpsSsh $vps "test `"`$(sudo -n docker inspect --format '{{.State.Running}}' plantcare-staging-api-1)`" = true"
    # Establish and verify an independent recovery timer BEFORE stopping the API.
    $null=Invoke-VpsSsh $vps "sudo -n systemd-run --unit=$unit --on-active=12m /usr/bin/docker start plantcare-staging-api-1 && sudo -n systemctl is-active $unit.timer"
    $armed=$true
    $stopped=$true
    $null=Invoke-VpsSsh $vps 'sudo -n docker stop --time 30 plantcare-staging-api-1'
    $downStatus=Get-StagingReadiness
    if($downStatus -eq 200){throw 'Outage drill did not make readiness unavailable.'}
    Write-BackupStatus 'uptime-drill' @{status='outage';startedAtUtc=$startedAt;httpStatus=$downStatus;emailEvidence='pending'}
    "Staging outage started; server-side recovery armed. Holding for $OutageMinutes minutes for the external monitor."
    $deadline=[DateTimeOffset]::UtcNow.AddMinutes($OutageMinutes)
    while([DateTimeOffset]::UtcNow -lt $deadline){Start-Sleep -Seconds 15}
} finally {
    try {
        if($stopped){
            $null=Invoke-VpsSsh $vps 'sudo -n docker start plantcare-staging-api-1'
            $ready=$false
            for($i=0;$i -lt 24;$i++){
                if((Get-StagingReadiness) -eq 200){$ready=$true;break}
                Start-Sleep -Seconds 5
            }
            if(-not $ready){throw 'Readiness has not recovered; independent server timer remains armed.'}
            Write-BackupStatus 'uptime-drill' @{status='recovered';startedAtUtc=$startedAt;recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('O');httpStatus=200;emailEvidence='pending'}
            'PASS: controlled API outage and public readiness recovery; external alert delivery must be checked separately.'
        }
        if($armed){$null=Invoke-VpsSsh $vps "sudo -n systemctl stop $unit.timer"}
    } finally {$lock.Dispose()}
}
