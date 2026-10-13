# Internal helpers for the Windows/Docker local backup tools. Never log secrets.
$ErrorActionPreference = 'Stop'
$BackupRoot = Split-Path $PSScriptRoot -Parent
$BackupState = Join-Path $BackupRoot '.operations-local'
$BackupCompose = @('compose', '--env-file', (Join-Path $BackupRoot '.env.local'), '-f', (Join-Path $BackupRoot 'compose.yaml'), '-f', (Join-Path $BackupRoot 'compose.local.yaml'), '--project-name', 'plantcare-local')

function Protect-BackupDirectory([string]$Path) {
    if (-not $IsWindows) { throw 'Local backup setup requires Windows and PowerShell 7.' }
    $null = New-Item -ItemType Directory -Path $Path -Force
    $sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    & icacls.exe $Path /inheritance:r /grant:r "*${sid}:(OI)(CI)F" '*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not restrict the private backup directory.' }
}

function Get-BackupConfiguration {
    $configuration = Get-Content -LiteralPath (Join-Path $BackupState 'config.json') -Raw | ConvertFrom-Json
    if (-not (Test-Path -LiteralPath $configuration.repository -PathType Container)) { throw 'The configured backup drive/repository is unavailable.' }
    return $configuration
}

function Get-BackupDocker {
    $command = Get-Command docker -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    foreach ($path in @((Join-Path $env:LOCALAPPDATA 'Programs/DockerDesktop/resources/bin/docker.exe'), (Join-Path $env:ProgramFiles 'Docker/Docker/resources/bin/docker.exe'))) {
        if (Test-Path -LiteralPath $path) { $env:Path = (Split-Path $path -Parent) + ';' + $env:Path; return $path }
    }
    throw 'Docker executable is unavailable.'
}

function Invoke-BackupDocker([string[]]$Arguments) {
    $output = & (Get-BackupDocker) @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Backup Docker operation failed ($($Arguments[0])); raw output withheld." }
    return $output
}

function Invoke-BackupSql([string]$Container, [string]$Query) {
    Invoke-BackupDocker @('exec', $Container, 'sh', '-c', 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -Q "$1"', 'sql', $Query)
}

function Invoke-BackupRestic([string[]]$Arguments, [string]$Repository) {
    $names = @('RESTIC_PASSWORD', 'RESTIC_PASSWORD_FILE', 'RESTIC_PASSWORD_COMMAND', 'RESTIC_REPOSITORY', 'RESTIC_REPOSITORY_FILE')
    $saved = @{}
    foreach ($name in $names) { $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process'); [Environment]::SetEnvironmentVariable($name, $null, 'Process') }
    try {
        $secret = Import-Clixml -LiteralPath (Join-Path $BackupState 'password.xml')
        $env:RESTIC_PASSWORD = [Net.NetworkCredential]::new('', $secret).Password
        $output = & (Join-Path $BackupState 'tools/restic.exe') --repo $Repository --cache-dir (Join-Path $BackupState 'cache') @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Encrypted repository operation failed ($($Arguments[0])); raw output withheld." }
        return $output
    }
    finally { foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process') } }
}

function Enter-BackupLock {
    # The open handle is the lock. A crashed process releases it automatically.
    return [IO.File]::Open((Join-Path $BackupState 'operation.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
}

function Remove-BackupScratch([string]$Path) {
    $resolved = [IO.Path]::GetFullPath($Path)
    $parent = [IO.Path]::GetFullPath((Join-Path $BackupState 'scratch')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($parent, [StringComparison]::OrdinalIgnoreCase)) { throw 'Refusing cleanup outside backup scratch.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}

function Write-BackupStatus([string]$Name, [object]$Value) {
    $path = Join-Path $BackupState ($Name + '.json')
    $Value | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath ($path + '.tmp')
    Move-Item -LiteralPath ($path + '.tmp') -Destination $path -Force
}

$BackupSnapshotSql = @'
USE [PlantCare]; SET NOCOUNT ON;
CREATE TABLE #state (TableName nvarchar(300), [RowCount] bigint, RowChecksum int);
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'INSERT #state SELECT N''' + REPLACE(s.name + N'.' + t.name, '''', '''''') + N''', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';'
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0;
EXEC sp_executesql @sql;
SELECT TableName, [RowCount], COALESCE(RowChecksum,0) FROM #state ORDER BY TableName;
'@
