$ErrorActionPreference='Stop'
. "$PSScriptRoot/BackupRetention.Common.ps1"
$now=[DateTimeOffset]'2026-10-21T10:00:00Z'
function Snapshot([char]$Id,[string]$HostName,[int]$Age){@{id=([string]$Id)*64;hostname=$HostName;tags=@($HostName);time=$now.AddDays(-$Age).ToString('O')}}
$local=Snapshot a plantcare-local 0;$vps=Snapshot b plantcare-vps-staging 0
$old=Snapshot c plantcare-local 31;$boundary=Snapshot d plantcare-vps-staging 30
$plan=New-BackupRetentionPlan @($local,$vps,$old,$boundary) @($local.id,$vps.id) $now
if($plan.expireIds.Count -ne 1 -or $plan.expireIds[0] -ne $old.id){throw 'Expiry boundary or latest protection failed.'}
foreach($bad in @(@($old,$vps),@($local,$vps,(Snapshot e unknown 31)),@($local,$vps,$local),@($local,$vps,(Snapshot f plantcare-local -1)))){
    $rejected=$false;try{$null=New-BackupRetentionPlan $bad @($local.id,$vps.id) $now}catch{$rejected=$true}
    if(-not $rejected){throw 'Unsafe retention input was accepted.'}
}
'PASS: expiry cutoff, both fresh groups, verified snapshots, unknown groups, duplicates and future dates.'
