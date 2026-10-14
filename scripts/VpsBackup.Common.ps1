. "$PSScriptRoot/LocalBackup.Common.ps1"
function Get-VpsBackupConfiguration {
    $c=Get-Content -LiteralPath (Join-Path $BackupState 'vps-backup-config.json') -Raw|ConvertFrom-Json
    if($c.host -ne '57.129.182.78' -or $c.user -ne 'ubuntu' -or $c.candidate -notmatch '^[a-f0-9]{40}$') { throw 'Review the fixed staging backup configuration before using another host.' }
    if(-not(Test-Path -LiteralPath $c.identityFile -PathType Leaf)){throw 'VPS SSH identity file unavailable.'}
    return $c
}
function Invoke-VpsSsh([object]$Config,[string]$Command) {
    $result=& ssh -o BatchMode=yes -o StrictHostKeyChecking=yes -o IdentitiesOnly=yes -o ConnectTimeout=15 -i $Config.identityFile ($Config.user+'@'+$Config.host) $Command 2>&1
    if($LASTEXITCODE -ne 0){throw 'VPS SSH operation failed; raw output withheld. Check agent, firewall and server health.'}
    return $result
}
function Invoke-VpsScp([object]$Config,[string]$Source,[string]$Destination,[switch]$Recurse) {
    $scpArguments=@('-o','BatchMode=yes','-o','StrictHostKeyChecking=yes','-o','IdentitiesOnly=yes','-o','ConnectTimeout=15','-i',$Config.identityFile)
    if($Recurse){$scpArguments+='-r'}
    $result=& scp @scpArguments $Source $Destination 2>&1
    if($LASTEXITCODE -ne 0){throw 'VPS backup file transfer failed; raw output withheld.'}
}
