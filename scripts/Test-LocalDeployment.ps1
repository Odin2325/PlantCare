# Requires PowerShell 7 and the compose.local.yaml stack. Creates one synthetic
# account per run; confirmation/reset links and passwords are never printed.
[CmdletBinding()]
param([switch]$IncludeCareJourney, [switch]$IncludeRecovery)

$ErrorActionPreference = 'Stop'
Import-Module Microsoft.PowerShell.Utility -ErrorAction Stop
if ($IncludeRecovery -and -not $IncludeCareJourney) { throw '-IncludeRecovery requires -IncludeCareJourney.' }
$baseUrl = 'https://localhost:8443'
$mailUrl = 'http://localhost:8025'
$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$email = 'smoke-' + [Guid]::NewGuid().ToString('N') + '@example.test'
$password = 'Pc9!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))

function Invoke-LocalRequest {
    param([string]$Path, [string]$Method = 'GET', [object]$Body, [int]$Expected = 200)
    $parameters = @{
        Uri = $baseUrl + $Path
        Method = $Method
        WebSession = $session
        SkipCertificateCheck = $true # Only this fixed localhost test origin.
        SkipHttpErrorCheck = $true
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 10
    }
    $response = Invoke-WebRequest @parameters
    if ([int]$response.StatusCode -ne $Expected) {
        $endpoint = ($Path -split '\?', 2)[0]
        throw "$Method $endpoint returned $($response.StatusCode), expected $Expected."
    }
    return $response
}

function Set-LocalAntiforgery {
    $response = Invoke-LocalRequest '/api/antiforgery/token'
    $session.Headers['X-XSRF-TOKEN'] = ($response.Content | ConvertFrom-Json).requestToken
}

$null = Invoke-LocalRequest '/health/live'
$null = Invoke-LocalRequest '/health/ready'
$page = Invoke-LocalRequest '/'
if ($page.Content -notmatch '<app-root') { throw 'Angular application shell was not served.' }
$null = Invoke-LocalRequest '/api/unknown-smoke-route' -Expected 404
Write-Output 'PASS: HTTPS, liveness/readiness, Angular shell, API 404.'

Set-LocalAntiforgery
$null = Invoke-LocalRequest '/api/auth/register' 'POST' @{ email = $email; password = $password }
$null = Invoke-LocalRequest '/api/auth/login?useCookies=true' 'POST' @{ email = $email; password = $password } -Expected 401

$message = $null
for ($attempt = 0; $attempt -lt 10; $attempt++) {
    $messages = Invoke-RestMethod "$mailUrl/api/v1/search?query=to%3A$email"
    $message = $messages.messages | Select-Object -First 1
    if ($message) { break }
    Start-Sleep -Seconds 1
}
if (-not $message) { throw 'Confirmation email was not captured by Mailpit.' }
$mail = Invoke-RestMethod "$mailUrl/api/v1/message/$($message.ID)"
$link = [regex]::Match($mail.HTML, 'href="([^"]+)"').Groups[1].Value
$link = [Net.WebUtility]::HtmlDecode($link)
$confirmationUri = [Uri]$link
if ($confirmationUri.GetLeftPart([UriPartial]::Authority) -ne $baseUrl -or
    $confirmationUri.AbsolutePath -ne '/api/auth/confirmEmail') {
    throw 'Confirmation email points to an unexpected origin or endpoint.'
}
$null = Invoke-LocalRequest $confirmationUri.PathAndQuery
Write-Output 'PASS: registration, SMTP capture, unconfirmed login rejection, confirmation link.'

$null = Invoke-LocalRequest '/api/auth/login?useCookies=true' 'POST' @{ email = $email; password = $password }
$me = Invoke-LocalRequest '/api/account/me'
if (($me.Content | ConvertFrom-Json).email -ne $email) { throw 'Authenticated account mismatch.' }
Set-LocalAntiforgery
if ($IncludeCareJourney) {
    $docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
    if (-not $docker) {
        $docker = Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin\docker.exe'
        if (-not (Test-Path $docker)) { $docker = Join-Path $env:ProgramFiles 'Docker\Docker\resources\bin\docker.exe' }
    }
    $fixturePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'deploy\catalog.example.json'
    $speciesRequest = (Get-Content -Raw $fixturePath | ConvertFrom-Json)[0]
    $null = Invoke-LocalRequest '/api/plant-species' 'POST' $speciesRequest -Expected 403
    $operatorOutput = & $docker exec plantcare-local-api-1 dotnet PlantCare.Api.dll --bootstrap-admin $email 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Local administrator provisioning failed.' }
    $operatorOutput = & $docker cp $fixturePath 'plantcare-local-api-1:/tmp/plantcare-catalog.json' 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Local catalog fixture copy failed.' }
    for ($import = 0; $import -lt 2; $import++) {
        $operatorOutput = & $docker exec plantcare-local-api-1 dotnet PlantCare.Api.dll --import-catalog /tmp/plantcare-catalog.json 2>&1
        if ($LASTEXITCODE -ne 0) { throw 'Local catalog import failed.' }
    }
    $null = Invoke-LocalRequest '/api/auth/login?useCookies=true' 'POST' @{ email = $email; password = $password }
    Set-LocalAntiforgery
    $catalog = (Invoke-LocalRequest '/api/plant-species').Content | ConvertFrom-Json
    $species = @($catalog | Where-Object commonName -EQ $speciesRequest.commonName)
    if ($species.Count -ne 1) { throw 'Repeated import did not produce exactly one demo species.' }
    $null = Invoke-LocalRequest "/api/plant-species/$($species[0].id)" 'PUT' $speciesRequest
    Write-Output 'PASS: ordinary-user catalog denial, explicit Admin assignment, repeatable catalog import, Admin catalog update.'

    $plant = (Invoke-LocalRequest '/api/my-plants' 'POST' @{
        plantSpeciesId = $species[0].id; nickname = 'Smoke care plant'; location = 'Test shelf'
        wateringIntervalDays = 7; lastWateredAtUtc = [DateTimeOffset]::UtcNow.AddDays(-8).ToString('O')
        tags = @('smoke', 'indoor')
    } -Expected 201).Content | ConvertFrom-Json
    $plantPath = '/api/my-plants/' + $plant.id
    if ('smoke' -notin $plant.tags) { throw 'Plant tags were not saved.' }
    $dashboard = (Invoke-LocalRequest '/api/dashboard/care?daysAhead=30').Content | ConvertFrom-Json
    if (-not ($dashboard | Where-Object userPlantId -EQ $plant.id)) { throw 'Overdue plant missing from dashboard.' }
    $reminder = $null
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        $notifications = (Invoke-LocalRequest '/api/notifications').Content | ConvertFrom-Json
        $reminder = $notifications | Where-Object userPlantId -EQ $plant.id | Select-Object -First 1
        if ($reminder) { break }
        Start-Sleep -Seconds 1
    }
    if (-not $reminder) { throw 'Worker did not generate the overdue reminder.' }
    $null = Invoke-LocalRequest "/api/notifications/$($reminder.id)/read" 'POST' -Expected 204
    $completedAt = [DateTimeOffset]::UtcNow.AddSeconds(-1)
    $completion = (Invoke-LocalRequest "$plantPath/care/Watering/complete" 'POST' @{
        completedAtUtc = $completedAt.ToString('O'); notes = 'Real SQL Server smoke test'
    }).Content | ConvertFrom-Json
    if ([Math]::Abs((([DateTimeOffset]$completion.schedule.nextDueAtUtc) - $completedAt.AddDays(7)).TotalSeconds) -gt 1) {
        throw 'Completion did not advance the schedule by seven days.'
    }
    $history = @((Invoke-LocalRequest "$plantPath/care/history").Content | ConvertFrom-Json)
    if ($history.Count -ne 1 -or $history[0].id -ne $completion.event.id) { throw 'Care history mismatch.' }
    $from = [Uri]::EscapeDataString($completedAt.AddDays(-1).ToString('O'))
    $to = [Uri]::EscapeDataString($completedAt.AddDays(15).ToString('O'))
    $calendar = (Invoke-LocalRequest "/api/calendar?fromUtc=$from&toUtc=$to").Content | ConvertFrom-Json
    if (-not ($calendar | Where-Object { $_.userPlantId -eq $plant.id -and $_.kind -eq 'Completed' }) -or
        -not ($calendar | Where-Object { $_.userPlantId -eq $plant.id -and $_.kind -eq 'Scheduled' })) {
        throw 'Calendar did not include completed and scheduled care.'
    }
    if ($IncludeRecovery) {
        & (Join-Path $PSScriptRoot 'Invoke-LocalRecoveryDrill.ps1') -Session $session -PlantId $plant.id -EventId $completion.event.id -SpeciesId $species[0].id -Email $email
    }
    $null = Invoke-LocalRequest $plantPath 'DELETE' -Expected 204
    $null = Invoke-LocalRequest "$plantPath/care/Watering/complete" 'POST' @{ completedAtUtc = $completedAt.ToString('O') } -Expected 404
    Write-Output 'PASS: SQL Server plant/tags, overdue dashboard, worker reminder, completion, next due date, history, calendar, archive.'
}
$null = Invoke-LocalRequest '/api/auth/logout' 'POST' -Expected 204
$null = Invoke-LocalRequest '/api/account/me' -Expected 401
Write-Output 'PASS: real cookie login, current account, antiforgery-protected logout, access revoked.'
