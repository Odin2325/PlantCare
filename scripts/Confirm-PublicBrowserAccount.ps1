# Consumes only an approved alias's privately saved real confirmation message.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('a','b')][string]$Account)
. "$PSScriptRoot/LocalBackup.Common.ps1"
. "$PSScriptRoot/PublicBrowserMail.Common.ps1"
$directory=Join-Path $BackupState 'browser-acceptance'
$path=Join-Path $directory 'accounts.json'
$accounts=Get-Content $path -Raw|ConvertFrom-Json
$selected=$accounts.$Account
if($accounts.approvedRecipients.Count -ne 2 -or $selected.email -notin $accounts.approvedRecipients -or -not $selected.created){throw 'Unapproved or uncreated confirmation account.'}
$file=Join-Path $directory ('confirmation-'+$Account+'.txt')
$uri=Resolve-StageMailLink $file '/api/auth/confirmEmail'
try {$response=Invoke-WebRequest $uri -MaximumRedirection 0 -SkipHttpErrorCheck -TimeoutSec 30}
catch {throw 'Private confirmation request failed; URL withheld.'}
if([int]$response.StatusCode -ne 200){throw 'Real staging confirmation rejected; response withheld.'}
$selected.confirmed=$true
$accounts|ConvertTo-Json -Depth 5|Set-Content $path
Remove-Item -LiteralPath $file
Write-BackupStatus ('public-browser-confirm-'+$Account) @{status='confirmed';atUtc=[DateTimeOffset]::UtcNow.ToString('O');httpStatus=200}
'PASS: approved staging account confirmation consumed; private link removed.'
