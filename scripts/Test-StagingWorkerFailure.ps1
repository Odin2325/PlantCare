# Freeze only the synthetic staging worker; the API remains available.
[CmdletBinding()]
param([ValidateRange(4,10)][int]$Minutes=9)
. "$PSScriptRoot/VpsBackup.Common.ps1"
$vps=Get-VpsBackupConfiguration
$lock=Enter-BackupLock
$unit='plantcare-worker-recovery-'+[Guid]::NewGuid().ToString('N')
$armed=$false;$paused=$false;$observedFailure=$false;$detectedAt=$null
function Status([string]$Path) {
    try{return [int](Invoke-WebRequest ('https://staging.plantacare.org'+$Path) -TimeoutSec 10 -SkipHttpErrorCheck).StatusCode}catch{return 0}
}
try {
    if((Status '/health/ready') -ne 200 -or (Status '/health/operations') -ne 200){throw 'Both staging endpoints must be healthy before the worker drill.'}
    $state=(Invoke-VpsSsh $vps "sudo -n docker inspect --format '{{.State.Running}} {{.State.Paused}}' plantcare-staging-worker-1").ToString().Trim()
    if($state -ne 'true false'){throw 'Staging worker must be running and unpaused.'}
    $null=Invoke-VpsSsh $vps "sudo -n systemd-run --unit=$unit --on-active=12m /usr/bin/docker unpause plantcare-staging-worker-1 && sudo -n systemctl is-active $unit.timer"
    $armed=$true;$paused=$true
    $null=Invoke-VpsSsh $vps 'sudo -n docker pause plantcare-staging-worker-1'
    $started=[DateTimeOffset]::UtcNow
    $deadline=$started.AddMinutes($Minutes)
    Write-BackupStatus 'worker-drill' @{status='paused';startedAtUtc=$started.ToString('O');emailEvidence='pending'}
    "Worker pause started with independent recovery timer; holding at most $Minutes minutes."
    while([DateTimeOffset]::UtcNow -lt $deadline){
        if((Status '/health/ready') -ne 200){throw 'API readiness failed during the worker drill.'}
        $ops=Status '/health/operations'
        if($ops -eq 503 -and -not $observedFailure){
            $observedFailure=$true
            $detectedAt=[DateTimeOffset]::UtcNow
            Write-BackupStatus 'worker-drill' @{status='stale-detected';startedAtUtc=$started.ToString('O');detectedAtUtc=$detectedAt.ToString('O');apiReady=200;operations=503;emailEvidence='pending'}
            'PASS: stalled running worker detected; API readiness remains 200.'
        }
        elseif($ops -notin @(200,503)){throw 'Unexpected operations response during worker drill.'}
        Start-Sleep -Seconds 15
    }
    if(-not $observedFailure){throw 'Operations endpoint failed to detect the stalled worker.'}
} finally {
    try {
        if($paused){
            $null=Invoke-VpsSsh $vps 'if [ "$(sudo -n docker inspect --format "{{.State.Paused}}" plantcare-staging-worker-1)" = true ]; then sudo -n docker unpause plantcare-staging-worker-1; fi'
            $healthy=$false
            for($n=0;$n -lt 20;$n++){
                if((Status '/health/operations') -eq 200 -and (Status '/health/ready') -eq 200){$healthy=$true;break}
                Start-Sleep -Seconds 5
            }
            if(-not $healthy){throw 'Worker resumed but operations health did not recover; server timer remains armed.'}
            Write-BackupStatus 'worker-drill' @{status=if($observedFailure){'recovered'}else{'incomplete'};startedAtUtc=$started.ToString('O');detectedAtUtc=if($detectedAt){$detectedAt.ToString('O')}else{$null};recoveredAtUtc=[DateTimeOffset]::UtcNow.ToString('O');stalledWorkerDetected=$observedFailure;apiReady=200;operations=200;emailEvidence='pending'}
            'PASS: worker resumed and public operations health recovered.'
        }
        if($armed){$null=Invoke-VpsSsh $vps "sudo -n systemctl stop $unit.timer"}
    } finally {$lock.Dispose()}
}
