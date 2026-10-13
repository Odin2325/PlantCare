# Bounded local acceptance probes; restores all private config/status in finally.
[CmdletBinding()]
param()
. "$PSScriptRoot/LocalBackup.Common.ps1"
$lock = Enter-BackupLock
$configPath = Join-Path $BackupState 'config.json'
$attemptPath = Join-Path $BackupState 'last-attempt.json'
$configText = Get-Content -LiteralPath $configPath -Raw
$attemptText = Get-Content -LiteralPath $attemptPath -Raw
$pwsh = (Get-Process -Id $PID).Path
function Probe-Health([string[]]$Parameters, [string]$ExpectedProblem) {
    $output = & $pwsh -NoProfile -NonInteractive -File (Join-Path $PSScriptRoot 'Test-LocalBackupHealth.ps1') @Parameters
    $code = $LASTEXITCODE
    $health = $output -join "`n" | ConvertFrom-Json
    if ($ExpectedProblem) {
        if ($code -ne 1 -or $health.problems -notcontains $ExpectedProblem) { throw "Expected health failure was not detected: $ExpectedProblem" }
    }
    elseif ($code -ne 0 -or -not $health.healthy) { throw 'Healthy repository probe failed.' }
}
try {
    Probe-Health @('-MinimumSourceFreeGigabytes', '0') ''
    Probe-Health @('-MaximumAgeHours', '0.000001', '-MinimumSourceFreeGigabytes', '0') 'backup-stale'
    Probe-Health @('-MinimumFreeGigabytes', '1000000', '-MinimumSourceFreeGigabytes', '0') 'low-backup-disk-space'
    Write-BackupStatus 'last-attempt' @{ status = 'failed'; atUtc = [DateTimeOffset]::UtcNow.ToString('O'); message = 'Bounded acceptance probe.' }
    Probe-Health @('-MinimumSourceFreeGigabytes', '0') 'last-backup-failed'
    $attemptText | Set-Content -LiteralPath $attemptPath
    $config = $configText | ConvertFrom-Json
    $repository = $config.repository
    $config.repository = Join-Path $repository ('unavailable-probe-' + [Guid]::NewGuid().ToString('N'))
    Write-BackupStatus 'config' $config
    Probe-Health @('-MinimumSourceFreeGigabytes', '0') 'backup-repository-or-status-unavailable'
    $configText | Set-Content -LiteralPath $configPath
    # Wrong password must not open the encrypted repository. Do not alter its key.
    $savedPassword = $env:RESTIC_PASSWORD
    $savedPasswordFile = $env:RESTIC_PASSWORD_FILE
    $savedPasswordCommand = $env:RESTIC_PASSWORD_COMMAND
    try {
        $env:RESTIC_PASSWORD = 'deliberately-wrong-acceptance-probe'
        $env:RESTIC_PASSWORD_FILE = $null; $env:RESTIC_PASSWORD_COMMAND = $null
        $null = & (Join-Path $BackupState 'tools/restic.exe') --repo $repository snapshots --json 2>&1
        if ($LASTEXITCODE -eq 0) { throw 'Repository accepted an incorrect password.' }
    }
    finally { $env:RESTIC_PASSWORD = $savedPassword; $env:RESTIC_PASSWORD_FILE = $savedPasswordFile; $env:RESTIC_PASSWORD_COMMAND = $savedPasswordCommand }
    Write-Output 'PASS: healthy state, stale backup, disk pressure, failed job, unavailable destination and wrong-password rejection.'
}
finally {
    try {
        $configText | Set-Content -LiteralPath $configPath
        $attemptText | Set-Content -LiteralPath $attemptPath
        # Restore the real health result, preserving real disk warnings.
        $null = & $pwsh -NoProfile -NonInteractive -File (Join-Path $PSScriptRoot 'Test-LocalBackupHealth.ps1')
    }
    finally { $lock.Dispose() }
}
exit 0
