# PowerShell 7; fixed local Docker acceptance stack only. Uses synthetic accounts.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$origin = 'https://localhost:8443'
$runId = [Guid]::NewGuid().ToString('N')

function Request {
    param([object]$User, [string]$Path, [string]$Method = 'GET', [object]$Body, [int]$Expected = 200)
    $parameters = @{ Uri = $origin + $Path; WebSession = $User.Session; Method = $Method; SkipCertificateCheck = $true; SkipHttpErrorCheck = $true; TimeoutSec = 30 }
    if ($null -ne $Body) { $parameters.ContentType = 'application/json'; $parameters.Body = $Body | ConvertTo-Json -Depth 10 }
    for ($attempt = 0; $attempt -lt 4; $attempt++) {
        $response = Invoke-WebRequest @parameters
        if ([int]$response.StatusCode -ne 429 -or $Path -notlike '/api/auth/*' -or $attempt -eq 3) { break }
        # Three registrations, confirmations, logins and logouts exceed the real
        # ten-per-minute IP limit. Respect it without weakening production policy.
        Write-Information 'Authentication rate limit reached; waiting 20 seconds before retry.' -InformationAction Continue
        Start-Sleep -Seconds 20
    }
    if ([int]$response.StatusCode -ne $Expected) {
        # Never include confirmation links or bearer feed tokens in failure output.
        $safePath = ($Path -split '\?', 2)[0] -replace '/subscription/[^/]+\.ics', '/subscription/[redacted].ics'
        throw "$Method $safePath returned $($response.StatusCode), expected $Expected."
    }
    return $response
}
function Json {
    param([object]$Response)
    return $Response.Content | ConvertFrom-Json
}
function New-TestUser {
    param([string]$Label)
    $user = [pscustomobject]@{ Session = [Microsoft.PowerShell.Commands.WebRequestSession]::new(); Email = "isolation-$runId-$Label@example.test"; Id = $null }
    $password = 'Pc9!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $user.Session.Headers['X-XSRF-TOKEN'] = (Json (Request $user '/api/antiforgery/token')).requestToken
    $null = Request $user '/api/auth/register' 'POST' @{ email = $user.Email; password = $password }
    $message = $null
    for ($attempt = 0; $attempt -lt 15; $attempt++) {
        $messages = Invoke-RestMethod ('http://localhost:8025/api/v1/search?query=' + [Uri]::EscapeDataString('to:' + $user.Email))
        $message = $messages.messages | Select-Object -First 1
        if ($message) { break }; Start-Sleep -Seconds 1
    }
    if (-not $message) { throw 'Synthetic confirmation email was not captured.' }
    $mail = Invoke-RestMethod "http://localhost:8025/api/v1/message/$($message.ID)"
    $uri = [Uri][Net.WebUtility]::HtmlDecode([regex]::Match($mail.HTML, 'href="([^"]+)"').Groups[1].Value)
    if ($uri.GetLeftPart([UriPartial]::Authority) -ne $origin -or $uri.AbsolutePath -ne '/api/auth/confirmEmail') { throw 'Unexpected confirmation origin/endpoint.' }
    $null = Request $user $uri.PathAndQuery
    $null = Request $user '/api/auth/login?useCookies=true' 'POST' @{ email = $user.Email; password = $password }
    $me = Json (Request $user '/api/account/me')
    if ($me.email -ne $user.Email) { throw 'Authenticated account mismatch.' }
    $user.Id = $me.id
    $user.Session.Headers['X-XSRF-TOKEN'] = (Json (Request $user '/api/antiforgery/token')).requestToken
    return $user
}
function Assert-OnlyPlant {
    param([object]$Entries, [string]$ExpectedId, [string]$IdProperty = 'userPlantId')
    $items = @($Entries)
    if (-not $items.Count -or @($items | Where-Object { $_.$IdProperty -ne $ExpectedId }).Count) { throw 'An account-scoped list omitted its own plant or included another account.' }
}
function Wait-Reminder {
    param([object]$User, [string]$PlantId)
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $items = @(Json (Request $User '/api/notifications'))
        $matching = @($items | Where-Object userPlantId -EQ $PlantId)
        if ($matching.Count -eq 1) { return $matching[0] }
        if ($matching.Count -gt 1) { throw 'Duplicate reminder in test fixture.' }
        Start-Sleep -Seconds 1
    }
    throw 'Worker reminder fixture was not created.'
}

$anonymous = [pscustomobject]@{ Session = [Microsoft.PowerShell.Commands.WebRequestSession]::new() }
$null = Request $anonymous '/health/ready'
foreach ($path in @('/api/account/me','/api/my-plants','/api/notifications','/api/calendar/shares','/api/calendar/subscription')) { $null = Request $anonymous $path -Expected 401 }
$a = New-TestUser 'a'; $b = New-TestUser 'b'; $c = New-TestUser 'observer'
if ($a.Id -eq $b.Id -or $a.Id -eq $c.Id -or $b.Id -eq $c.Id) { throw 'Test accounts are not distinct.' }
Write-Output 'PASS: three distinct confirmed ordinary accounts with separate cookies/antiforgery; anonymous protected reads rejected.'

$docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
if (-not $docker) {
    $docker = Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin\docker.exe'
    if (-not (Test-Path $docker)) { $docker = Join-Path $env:ProgramFiles 'Docker\Docker\resources\bin\docker.exe' }
}
$fixturePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'deploy/catalog.example.json'
$operatorOutput = & $docker cp $fixturePath plantcare-local-api-1:/tmp/plantcare-isolation-catalog.json 2>&1
if ($LASTEXITCODE -ne 0) { throw 'Catalog fixture copy failed.' }
$operatorOutput = & $docker exec plantcare-local-api-1 dotnet PlantCare.Api.dll --import-catalog /tmp/plantcare-isolation-catalog.json 2>&1
if ($LASTEXITCODE -ne 0) { throw 'Catalog fixture import failed.' }
$species = @(Json (Request $a '/api/plant-species') | Where-Object commonName -EQ 'Demo houseplant')
if ($species.Count -ne 1) { throw 'Demo catalog fixture mismatch.' }
foreach ($user in @($a, $b, $c)) {
    $null = Request $user '/api/plant-species' 'POST' ((Get-Content -Raw $fixturePath | ConvertFrom-Json)[0]) -Expected 403
    if ((Json (Request $user '/api/push/config')).isEnabled) { throw 'This synthetic push fixture requires local push delivery to be disabled.' }
}
$plants = @()
foreach ($user in @($a, $b)) {
    $plants += Json (Request $user '/api/my-plants' 'POST' @{
        plantSpeciesId = $species[0].id; nickname = "Isolation-$($user.Id)"; location = 'Synthetic shelf'; tags = @('private')
        wateringIntervalDays = 7; lastWateredAtUtc = [DateTimeOffset]::UtcNow.AddDays(-8).ToString('O')
        userId = $c.Id # Extra client-supplied ownership must never override the cookie identity.
    } -Expected 201)
}
$plantA = $plants[0]; $plantB = $plants[1]
$reminderA = Wait-Reminder $a $plantA.id; $reminderB = Wait-Reminder $b $plantB.id
$completedAt = [DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('O')
$completionA = Json (Request $a "/api/my-plants/$($plantA.id)/care/Watering/complete" 'POST' @{ completedAtUtc = $completedAt; notes = 'Owner A history' })
$completionB = Json (Request $b "/api/my-plants/$($plantB.id)/care/Watering/complete" 'POST' @{ completedAtUtc = $completedAt; notes = 'Owner B history' })
$from = [Uri]::EscapeDataString([DateTimeOffset]::UtcNow.AddDays(-15).ToString('O'))
$to = [Uri]::EscapeDataString([DateTimeOffset]::UtcNow.AddDays(30).ToString('O'))
$range = "fromUtc=$from&toUtc=$to"

# Exercise both directions, with valid payloads and valid attacker antiforgery tokens.
foreach ($pair in @(@{ owner=$a; attacker=$b; plant=$plantA; event=$completionA.event; reminder=$reminderA }, @{ owner=$b; attacker=$a; plant=$plantB; event=$completionB.event; reminder=$reminderB })) {
    $own = $pair.owner; $other = $pair.attacker; $plant = $pair.plant; $path = "/api/my-plants/$($plant.id)"
    if ((Json (Request $own "/api/account/me?userId=$($other.Id)")).id -ne $own.Id) { throw 'Client-supplied user ID changed account identity.' }
    Assert-OnlyPlant (Json (Request $own "/api/my-plants?userId=$($other.Id)")) $plant.id 'id'
    Assert-OnlyPlant (Json (Request $own "/api/dashboard/care?daysAhead=30&userId=$($other.Id)")) $plant.id
    Assert-OnlyPlant (Json (Request $own "/api/calendar?$range&userId=$($other.Id)")) $plant.id
    Assert-OnlyPlant (Json (Request $own "/api/notifications?userId=$($other.Id)")) $plant.id
    $null = Request $other $path -Expected 404
    $null = Request $other "$path/care/history" -Expected 404
    $null = Request $other $path 'PUT' @{ nickname='Unauthorized edit'; tags=@('changed') } -Expected 404
    $null = Request $other $path 'DELETE' -Expected 404
    $null = Request $other "$path/restore" 'POST' -Expected 404
    $null = Request $other "$path/care/Watering/complete" 'POST' @{ completedAtUtc=$completedAt } -Expected 404
    $null = Request $other "$path/care/Watering/schedule" 'PUT' @{ intervalDays=2; isEnabled=$false; scheduleMode='Interval' } -Expected 404
    $null = Request $other "$path/care/Misting/schedule" 'POST' @{ intervalDays=3 } -Expected 404
    $null = Request $other "$path/care/Watering/schedule" 'DELETE' -Expected 404
    $null = Request $other "$path/care/history/$($pair.event.id)" 'PUT' @{ completedAtUtc=$completedAt; notes='Unauthorized history' } -Expected 404
    $null = Request $other "$path/care/history/$($pair.event.id)" 'DELETE' -Expected 404
    $attackerPlant = if ($other.Id -eq $a.Id) { $plantA } else { $plantB }
    $null = Request $other "/api/my-plants/$($attackerPlant.id)/care/history/$($pair.event.id)" 'PUT' @{ completedAtUtc=$completedAt; notes='Mixed-ID edit' } -Expected 404
    $null = Request $other "/api/my-plants/$($attackerPlant.id)/care/history/$($pair.event.id)" 'DELETE' -Expected 404
    $null = Request $other "/api/notifications/$($pair.reminder.id)/read" 'POST' -Expected 404
    $after = Json (Request $own $path)
    $history = @(Json (Request $own "$path/care/history"))
    $watering = @($after.careSchedules | Where-Object actionType -EQ 'Watering')
    $notification = @(Json (Request $own '/api/notifications') | Where-Object id -EQ $pair.reminder.id)
    if ($after.nickname -ne $plant.nickname -or 'private' -notin $after.tags -or $history.Count -ne 1 -or
        $history[0].id -ne $pair.event.id -or $history[0].notes -ne $pair.event.notes -or
        $watering.Count -ne 1 -or $watering[0].intervalDays -ne 7 -or -not $watering[0].isEnabled -or
        $after.careSchedules.Count -ne 1 -or $notification.Count -ne 1 -or $notification[0].readAtUtc) { throw 'An unauthorized operation changed owner state.' }
}
Write-Output 'PASS: bidirectional SQL ownership checks for collections, dashboard, calendar, plants/tags, schedules, history, mixed plant/event IDs, notifications; owner state preserved.'

$preferenceA = Json (Request $a '/api/notifications/preferences')
$null = Request $b "/api/notifications/preferences?userId=$($a.Id)" 'PUT' @{
    userId=$a.Id; inAppEnabled=$false; pushEnabled=$false; wateringEnabled=$false; fertilizingEnabled=$false
    mistingEnabled=$false; pruningEnabled=$false; repottingEnabled=$false; reminderLeadTimeHours=24
}
$afterPreferenceA = Json (Request $a '/api/notifications/preferences')
$afterPreferenceB = Json (Request $b '/api/notifications/preferences')
if (($preferenceA | ConvertTo-Json -Compress) -ne ($afterPreferenceA | ConvertTo-Json -Compress) -or $afterPreferenceB.inAppEnabled -or $afterPreferenceB.reminderLeadTimeHours -ne 24) { throw 'Preference updates crossed account boundaries.' }
$endpoint = "https://push.example.test/isolation/$runId"
$push = Json (Request $a '/api/push/subscriptions' 'POST' @{ endpoint=$endpoint; p256dh='synthetic-key'; auth='synthetic-auth' })
$null = Request $b "/api/push/subscriptions/$($push.id)" 'DELETE' -Expected 404
$null = Request $b '/api/push/subscriptions' 'POST' @{ endpoint=$endpoint; p256dh='other-key'; auth='other-auth' } -Expected 400
$null = Request $a "/api/push/subscriptions/$($push.id)" 'DELETE' -Expected 204
Write-Output 'PASS: preference ownership ignores supplied user IDs; push endpoint takeover and cross-user deletion rejected.'

$share = Json (Request $a '/api/calendar/shares' 'POST' @{ recipientEmail=$b.Email } -Expected 201)
Assert-OnlyPlant (Json (Request $b "/api/calendar/shares/$($share.id)/entries?$range")) $plantA.id
$null = Request $b "/api/my-plants/$($plantA.id)" -Expected 404
$null = Request $c "/api/calendar/shares/$($share.id)/entries?$range" -Expected 404
if (@(Json (Request $c '/api/calendar/shares')).Count) { throw 'Unrelated user can list calendar shares.' }
$null = Request $b "/api/calendar/shares/$($share.id)" 'DELETE' -Expected 404
$null = Request $c "/api/calendar/shares/$($share.id)" 'DELETE' -Expected 404
$null = Request $a "/api/calendar/shares/$($share.id)" 'DELETE' -Expected 204
$null = Request $b "/api/calendar/shares/$($share.id)/entries?$range" -Expected 404
Write-Output 'PASS: calendar recipient sees only explicitly shared entries; private plant access remains denied; observer denied; owner revocation takes effect.'

$feedA = Json (Request $a '/api/calendar/subscription' 'POST')
$feedB = Json (Request $b '/api/calendar/subscription' 'POST')
function Feed-Path([string]$Url) {
    $uri = [Uri]$Url
    if ($uri.GetLeftPart([UriPartial]::Authority) -ne $origin) { throw 'Unexpected feed origin.' }
    return $uri.PathAndQuery
}
$feedPathA = Feed-Path $feedA.subscriptionUrl; $feedPathB = Feed-Path $feedB.subscriptionUrl
$ical = (Request $anonymous $feedPathA).Content
if ($ical -notmatch 'BEGIN:VCALENDAR' -or -not $ical.Contains($plantA.nickname) -or $ical.Contains($plantB.nickname)) { throw 'Bearer feed exposed incorrect account entries.' }
$icalB = (Request $anonymous $feedPathB).Content
if ($icalB -notmatch 'BEGIN:VCALENDAR' -or -not $icalB.Contains($plantB.nickname) -or $icalB.Contains($plantA.nickname)) { throw 'Second bearer feed exposed incorrect account entries.' }
$replacement = Json (Request $a '/api/calendar/subscription' 'POST')
$null = Request $anonymous $feedPathA -Expected 404
$null = Request $anonymous (Feed-Path $replacement.subscriptionUrl)
$null = Request $a '/api/calendar/subscription' 'DELETE' -Expected 204
$null = Request $anonymous (Feed-Path $replacement.subscriptionUrl) -Expected 404
$null = Request $anonymous $feedPathB
$null = Request $b '/api/calendar/subscription' 'DELETE' -Expected 204
Write-Output 'PASS: bearer feed contains only its owner calendar; rotation/revocation invalidates old tokens without revoking another user feed.'

foreach ($pair in @(@{user=$a;plant=$plantA}, @{user=$b;plant=$plantB})) { $null = Request $pair.user "/api/my-plants/$($pair.plant.id)" 'DELETE' -Expected 204 }
$null = Request $b "/api/my-plants/$($plantA.id)/restore" 'POST' -Expected 404
Assert-OnlyPlant (Json (Request $a '/api/my-plants/archived')) $plantA.id 'id'
Assert-OnlyPlant (Json (Request $b '/api/my-plants/archived')) $plantB.id 'id'
foreach ($user in @($a, $b, $c)) {
    $null = Request $user '/api/auth/logout' 'POST' -Expected 204
    $null = Request $user '/api/account/me' -Expected 401
}
Write-Output 'PASS: archive/restore isolation and all sessions logged out. Synthetic accounts/archived plants remain in Docker SQL; Windows localhost untouched.'
Write-Output 'NOT RUN: real Google/Microsoft credential isolation and browser push delivery; local providers remain disabled.'
