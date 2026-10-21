# PowerShell 7, .NET 10 SDK and the fixed local Docker SQL Server at port 14330.
# Each test migrates a generated PlantCarePushTests_* database and removes only
# that database afterward. Windows SQL Server and PlantCare data are untouched.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$previousPassword = $env:PLANTCARE_PUSH_TEST_SQL_PASSWORD
Push-Location $repoRoot
try {
    $null = Invoke-WebRequest 'https://localhost:8443/health/ready' -SkipCertificateCheck -TimeoutSec 30
    $passwordLine = Get-Content -LiteralPath '.env.local' | Where-Object { $_.StartsWith('SQL_PASSWORD=') }
    if (@($passwordLine).Count -ne 1) { throw 'Expected one SQL password in the private local Docker configuration.' }
    $env:PLANTCARE_PUSH_TEST_SQL_PASSWORD = $passwordLine.Substring('SQL_PASSWORD='.Length)
    if ([string]::IsNullOrWhiteSpace($env:PLANTCARE_PUSH_TEST_SQL_PASSWORD)) { throw 'Local Docker SQL password is missing.' }
    dotnet test PlantCare.Api.IntegrationTests/PlantCare.Api.IntegrationTests.csproj --configuration Release --filter 'FullyQualifiedName~PushDeliveryTests|FullyQualifiedName~AccountDataTests' --verbosity minimal -p:UseSharedCompilation=false -m:1
    if ($LASTEXITCODE -ne 0) { throw 'SQL Server push delivery checks failed.' }
    Write-Output 'PASS: encrypted push transport, per-device persistence, bounded retries, expiry and queue progress on isolated SQL Server databases.'
    Write-Output 'PASS: private account export and transactional deletion, confirmation/password/antiforgery checks, owner isolation and session revocation on isolated SQL Server databases.'
}
finally {
    $env:PLANTCARE_PUSH_TEST_SQL_PASSWORD = $previousPassword
    Pop-Location
}
