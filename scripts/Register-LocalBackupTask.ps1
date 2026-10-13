[CmdletBinding()]
param([ValidatePattern('^([01][0-9]|2[0-3]):[0-5][0-9]$')][string]$At = '03:00')
. "$PSScriptRoot/LocalBackup.Common.ps1"
$null = Get-BackupConfiguration
$taskName = 'PlantCare-LocalEncryptedBackup'
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) { throw 'Backup task already exists; inspect it before changing its schedule.' }
$script = Join-Path $PSScriptRoot 'Invoke-LocalBackup.ps1'
$pwsh = (Get-Process -Id $PID).Path
$action = New-ScheduledTaskAction -Execute $pwsh -Argument ('-NoProfile -NonInteractive -File "' + $script + '"') -WorkingDirectory $BackupRoot
$trigger = New-ScheduledTaskTrigger -Daily -At $At
$principal = New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType Interactive -RunLevel Limited
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 1) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$null = Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Encrypted local Docker SQL/key/config backup to the configured external drive. Requires logged-in owner, Docker Desktop and attached drive.'
Write-Output "Registered daily backup at $At ($(Get-TimeZone | Select-Object -ExpandProperty Id)); missed runs start when available."
