[CmdletBinding()]
param([Parameter(Mandatory)][string]$Repository)
. "$PSScriptRoot/LocalBackup.Common.ps1"
if (-not [IO.Path]::IsPathFullyQualified($Repository)) { throw 'Use an absolute backup repository path.' }
if (Test-Path -LiteralPath (Join-Path $BackupState 'config.json')) { throw 'Backups are already configured; refusing to replace their key or repository.' }
if ((Test-Path -LiteralPath $Repository) -and (Get-ChildItem -LiteralPath $Repository -Force | Select-Object -First 1)) { throw 'Choose a new empty repository folder.' }
Protect-BackupDirectory $BackupState
Protect-BackupDirectory (Join-Path $BackupState 'tools')
# Pinned official Windows x64 release; checksum obtained from its SHA256SUMS.
$archive = Join-Path $BackupState 'tools/restic.zip'
Invoke-WebRequest 'https://github.com/restic/restic/releases/download/v0.19.1/restic_0.19.1_windows_amd64.zip' -OutFile $archive -TimeoutSec 120
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne 'DA948AD707ED690426473AABA2046CD61F8F90F6F0E7DAB6BE0D5796531DE67D') { throw 'Restic download checksum mismatch.' }
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $BackupState 'tools') -Force
Move-Item -LiteralPath (Join-Path $BackupState 'tools/restic_0.19.1_windows_amd64.exe') -Destination (Join-Path $BackupState 'tools/restic.exe') -Force
if (-not (Test-Path -LiteralPath (Join-Path $BackupState 'password.xml'))) {
    $password = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    ConvertTo-SecureString $password -AsPlainText -Force | Export-Clixml -LiteralPath (Join-Path $BackupState 'password.xml')
    $password = $null
}
$null = New-Item -ItemType Directory -Path $Repository -Force
$null = Invoke-BackupRestic @('init') $Repository
Write-BackupStatus 'config' @{ repository = [IO.Path]::GetFullPath($Repository); toolVersion = '0.19.1'; createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O') }
Write-Output 'Encrypted repository initialized. Export a separate offline recovery key before relying on these backups.'
