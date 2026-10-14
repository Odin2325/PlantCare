[CmdletBinding()]
param([ValidatePattern('^([01][0-9]|2[0-3]):[0-5][0-9]$')][string]$At='04:00')
. "$PSScriptRoot/VpsBackup.Common.ps1"
$null=Get-BackupConfiguration
$null=Get-VpsBackupConfiguration
$taskName='PlantCare-VpsEncryptedBackup'
if(Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue){throw 'VPS backup task already exists; inspect before modifying its schedule.'}
$script=Join-Path $PSScriptRoot 'Invoke-VpsBackup.ps1'
$action=New-ScheduledTaskAction -Execute (Get-Process -Id $PID).Path -Argument ('-NoProfile -NonInteractive -File "'+$script+'"') -WorkingDirectory $BackupRoot
$trigger=New-ScheduledTaskTrigger -Daily -At $At
$principal=New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType Interactive -RunLevel Limited
$settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 1) -RestartCount 2 -RestartInterval (New-TimeSpan -Minutes 15) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$null=Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Encrypted VPS SQL/key/config backup to the configured external drive. Requires logged-in owner, unlocked SSH agent, trusted host key, permitted source IP and attached drive. Retries twice; brief app maintenance during capture.'
'Registered daily VPS backup at '+$At+' ('+(Get-TimeZone).Id+'); PC/agent/drive/network dependencies remain.'
