# Public staging only. Register/reset phases send real mail to two approved aliases.
[CmdletBinding()]
param([ValidateSet('Public','Register','Journey','Request-Reset','Reset','Resend')][string]$Phase='Public',[switch]$SkipBuild,[ValidateSet('desktop-chromium','mobile-chromium','desktop-firefox')][string]$Project)
. "$PSScriptRoot/LocalBackup.Common.ps1"
$private=Join-Path $BackupState 'browser-acceptance'
Protect-BackupDirectory $private
$accounts=Join-Path $private 'accounts.json'
if($Phase -ne 'Public' -and -not(Test-Path $accounts)){throw 'Private approved staging account configuration missing.'}
$null=Invoke-WebRequest 'https://staging.plantacare.org/health/ready' -TimeoutSec 20
$docker=Get-BackupDocker
if(-not $SkipBuild){
    & $docker build --tag plantcare-browser-tests:local (Join-Path $BackupRoot 'tests/browser')
    if($LASTEXITCODE){throw 'Browser tooling build failed.'}
}
$grep=switch($Phase){'Public'{'public pages support'} 'Register'{'register the two'} 'Journey'{'public staging core'} 'Request-Reset'{'request the approved'} 'Reset'{'consume real reset'} 'Resend'{'resend the missing'}}
$address='57.129.182.78'
if($address -notin @([Net.Dns]::GetHostAddresses('staging.plantacare.org')|ForEach-Object ToString)){throw 'Staging address changed; review the fixed browser host mapping.'}
# Docker DNS has intermittently failed. Retain the public hostname and normal
# certificate validation while using the independently verified server address.
$arguments=@('run','--rm','--init','--ipc=host','--add-host',"staging.plantacare.org:$address",'-v',"${private}:/private",'-e',('STAGING_PHASE='+$Phase.ToLowerInvariant()),'plantcare-browser-tests:local','npx','playwright','test','--config','staging.config.ts','--grep',$grep)
if($Project){$arguments+="--project=$Project"}
elseif($Phase -in @('Register','Request-Reset','Reset','Resend')){$arguments+='--project=desktop-chromium'}
& $docker @arguments
if($LASTEXITCODE){throw 'Public browser acceptance failed; only sanitized errors are shown.'}
$results=Get-Content (Join-Path $private 'browser-results.json') -Raw|ConvertFrom-Json
$candidate=$null
$pointer=Join-Path $BackupState 'vps-backup-config.json'
if(Test-Path $pointer){$candidate=(Get-Content $pointer -Raw|ConvertFrom-Json).candidate}
if($candidate -and $candidate -notmatch '^[a-f0-9]{40}$'){throw 'Invalid private runtime candidate pointer.'}
$evidence=@{atUtc=[DateTimeOffset]::UtcNow.ToString('O');phase=$Phase;results=@($results);target='https://staging.plantacare.org';certificateVerification=$true;runtimeCandidate=$candidate}
Write-BackupStatus ('public-browser-'+$Phase.ToLowerInvariant()) $evidence
# Keep each successful run as well as the latest status; targeted reruns should
# not overwrite evidence from earlier browser projects or candidate versions.
$evidence|ConvertTo-Json -Depth 8|Set-Content (Join-Path $private ('evidence-'+$Phase.ToLowerInvariant()+'-'+[Guid]::NewGuid().ToString('N')+'.json'))
