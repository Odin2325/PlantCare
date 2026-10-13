# Run interactively with a destination separate from the encrypted repository.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputPath)
. "$PSScriptRoot/LocalBackup.Common.ps1"
$config = Get-BackupConfiguration
$resolved = [IO.Path]::GetFullPath($OutputPath)
if ($resolved.StartsWith([IO.Path]::GetPathRoot($config.repository), [StringComparison]::OrdinalIgnoreCase)) { throw 'Store the recovery key separately from the backup drive.' }
if (Test-Path -LiteralPath $resolved) { throw 'Refusing to overwrite an existing recovery key file.' }
$secret = Import-Clixml -LiteralPath (Join-Path $BackupState 'password.xml')
$stream = [IO.File]::Open($resolved, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $writer = [IO.StreamWriter]::new($stream)
    try { $writer.WriteLine([Net.NetworkCredential]::new('', $secret).Password) }
    finally { $writer.Dispose() }
}
finally { $stream.Dispose() }
Write-Output 'Recovery key exported without displaying it. Store it in a separate password manager/offline secure location, then remove the temporary plaintext file.'
