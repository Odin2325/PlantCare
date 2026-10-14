# Prepare only the approved A account's privately saved reset message for the browser.
[CmdletBinding()]
param()
. "$PSScriptRoot/LocalBackup.Common.ps1"
. "$PSScriptRoot/PublicBrowserMail.Common.ps1"
$directory=Join-Path $BackupState 'browser-acceptance'
$path=Join-Path $directory 'accounts.json'
$accounts=Get-Content $path -Raw|ConvertFrom-Json
if($accounts.approvedRecipients.Count -ne 2 -or $accounts.a.email -notin $accounts.approvedRecipients -or $accounts.a.email -eq $accounts.b.email){throw 'Unapproved reset account configuration.'}
$file=Join-Path $directory 'reset-a.txt'
$uri=Resolve-StageMailLink $file '/reset-password'
$accounts.a|Add-Member -NotePropertyName resetLink -NotePropertyValue $uri.AbsoluteUri -Force
$accounts|ConvertTo-Json -Depth 5|Set-Content $path
Remove-Item -LiteralPath $file
'PASS: approved reset link resolved privately; contents withheld.'
