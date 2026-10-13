# PowerShell 7; real HTTP/SMTP/cookie checks against the fixed local Docker stack.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
# Load the WebRequestSession type explicitly in fresh/no-profile CI sessions.
Import-Module Microsoft.PowerShell.Utility -ErrorAction Stop
$origin = 'https://localhost:8443'
$runId = [Guid]::NewGuid().ToString('N')
$email = "auth-security-$runId@example.test"
$unconfirmedEmail = "auth-unconfirmed-$runId@example.test"
$unknownEmail = "auth-unknown-$runId@example.test"
$oldPassword = 'Pc9!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
$newPassword = 'Pc9!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))

function New-Session { return [Microsoft.PowerShell.Commands.WebRequestSession]::new() }
function Request {
    param([object]$Session, [string]$Path, [string]$Method = 'GET', [object]$Body, [int[]]$Expected = @(200))
    $parameters = @{ Uri = $origin + $Path; WebSession = $Session; Method = $Method; SkipCertificateCheck = $true; SkipHttpErrorCheck = $true; TimeoutSec = 30 }
    if ($null -ne $Body) { $parameters.ContentType = 'application/json'; $parameters.Body = $Body | ConvertTo-Json -Depth 10 }
    for ($attempt = 0; $attempt -lt 4; $attempt++) {
        try { $response = Invoke-WebRequest @parameters }
        catch { throw 'Local authentication request failed; URLs and credentials are withheld.' }
        if ([int]$response.StatusCode -ne 429 -or $Path -notlike '/api/auth/*' -or $attempt -eq 3) { break }
        Write-Information 'Authentication rate limit reached; waiting 20 seconds before retry.' -InformationAction Continue
        Start-Sleep -Seconds 20
    }
    if ([int]$response.StatusCode -notin $Expected) {
        throw "$Method $(($Path -split '\?', 2)[0]) returned $($response.StatusCode), expected $($Expected -join '/')."
    }
    return $response
}
function Set-Token([object]$Session) {
    $Session.Headers['X-XSRF-TOKEN'] = ((Request $Session '/api/antiforgery/token').Content | ConvertFrom-Json).requestToken
}
function Get-MailLink([string]$Recipient, [string]$Subject, [string]$Path) {
    $message = $null
    for ($attempt = 0; $attempt -lt 15; $attempt++) {
        $messages = Invoke-RestMethod ('http://localhost:8025/api/v1/search?query=' + [Uri]::EscapeDataString('to:' + $Recipient))
        $message = $messages.messages | Where-Object Subject -EQ $Subject | Select-Object -First 1
        if ($message) { break }; Start-Sleep -Seconds 1
    }
    if (-not $message) { throw 'Expected local authentication email was not captured.' }
    $mail = Invoke-RestMethod "http://localhost:8025/api/v1/message/$($message.ID)"
    $uri = [Uri][Net.WebUtility]::HtmlDecode([regex]::Match($mail.HTML, 'href="([^"]+)"').Groups[1].Value)
    if ($uri.GetLeftPart([UriPartial]::Authority) -ne $origin -or $uri.AbsolutePath -ne $Path) { throw 'Authentication email has an unexpected origin or endpoint.' }
    return $uri
}
function Reset-Body([string]$Account, [string]$Code, [string]$Password) { return @{ email=$Account; resetCode=$Code; newPassword=$Password } }

$requester = New-Session
$null = Request $requester '/health/ready'
Set-Token $requester
$null = Request $requester '/api/auth/register' 'POST' @{ email=$email; password=$oldPassword }
$null = Request $requester '/api/auth/register' 'POST' @{ email=$unconfirmedEmail; password=$oldPassword }
$confirmation = Get-MailLink $email 'Confirm your PlantCare account' '/api/auth/confirmEmail'
$null = Request $requester $confirmation.PathAndQuery
$null = Request $requester '/api/auth/login?useCookies=true' 'POST' @{ email=$unconfirmedEmail; password=$oldPassword } -Expected 401

$browserA = New-Session; $browserB = New-Session
foreach ($browser in @($browserA, $browserB)) {
    Set-Token $browser
    $login = Request $browser '/api/auth/login?useCookies=true' 'POST' @{ email=$email; password=$oldPassword }
    $cookieHeaders = @($login.Headers['Set-Cookie'] | Where-Object { $_ -match '^PlantCare.Authentication=' })
    if ($cookieHeaders.Count -ne 1 -or $cookieHeaders[0] -notmatch '(?i);\s*secure(?:;|$)' -or
        $cookieHeaders[0] -notmatch '(?i);\s*httponly(?:;|$)' -or $cookieHeaders[0] -notmatch '(?i);\s*samesite=lax(?:;|$)') { throw 'Authentication cookie security flags are missing.' }
    $null = Request $browser '/api/account/me'
    Set-Token $browser
}
Write-Output 'PASS: confirmed/unconfirmed login policy, two real browser sessions, Secure/HttpOnly/SameSite cookie flags.'

$preferences = (Request $browserA '/api/notifications/preferences').Content | ConvertFrom-Json
$validToken = $browserA.Headers['X-XSRF-TOKEN']
$null = $browserA.Headers.Remove('X-XSRF-TOKEN')
$null = Request $browserA '/api/notifications/preferences' 'PUT' $preferences -Expected 400
$browserA.Headers['X-XSRF-TOKEN'] = 'invalid-antiforgery-token'
$null = Request $browserA '/api/notifications/preferences' 'PUT' $preferences -Expected 400
$browserA.Headers['X-XSRF-TOKEN'] = $requester.Headers['X-XSRF-TOKEN']
$null = Request $browserA '/api/notifications/preferences' 'PUT' $preferences -Expected 400
$null = $browserA.Headers.Remove('X-XSRF-TOKEN')
$browserA.Headers['Authorization'] = 'Bearer invalid-token'
$null = Request $browserA '/api/notifications/preferences' 'PUT' $preferences -Expected @(400,401)
$null = $browserA.Headers.Remove('Authorization')
$browserA.Headers['X-XSRF-TOKEN'] = $validToken
$null = Request $browserA '/api/notifications/preferences' 'PUT' $preferences

$withoutToken = New-Session
$null = Request $withoutToken '/api/auth/forgotPassword' 'POST' @{ email=$email } -Expected 400
$withoutToken.Headers['Authorization'] = 'Bearer invalid-token'
$null = Request $withoutToken '/api/auth/forgotPassword' 'POST' @{ email=$email } -Expected 400
Write-Output 'PASS: missing, invalid and wrong-identity antiforgery rejected; bogus bearer header cannot bypass protection; valid cookie requests work.'

$known = Request $requester '/api/auth/forgotPassword' 'POST' @{ email=$email }
$unknown = Request $requester '/api/auth/forgotPassword' 'POST' @{ email=$unknownEmail }
$unconfirmed = Request $requester '/api/auth/forgotPassword' 'POST' @{ email=$unconfirmedEmail }
if ($known.Content -ne $unknown.Content -or $known.Content -ne $unconfirmed.Content) { throw 'Forgot-password response distinguishes account existence or confirmation.' }
foreach ($recipient in @($unknownEmail, $unconfirmedEmail)) {
    $messages = Invoke-RestMethod ('http://localhost:8025/api/v1/search?query=' + [Uri]::EscapeDataString('to:' + $recipient))
    if (@($messages.messages | Where-Object Subject -EQ 'Reset your PlantCare password').Count) { throw 'Reset email sent to an unknown or unconfirmed account.' }
}
$resetUri = Get-MailLink $email 'Reset your PlantCare password' '/reset-password'
$query = @{}
foreach ($item in $resetUri.Query.TrimStart('?').Split('&')) {
    $parts = $item.Split('=', 2)
    $query[[Uri]::UnescapeDataString($parts[0])] = [Uri]::UnescapeDataString($parts[1])
}
if ($query['email'] -ne $email -or [string]::IsNullOrWhiteSpace($query['code'])) { throw 'Reset email query parameters are invalid.' }
$page = Request $requester $resetUri.PathAndQuery
if ($page.Content -notmatch '<app-root') { throw 'Reset link does not reach the Angular application.' }
$code = $query['code']
$null = Request $requester '/api/auth/resetPassword' 'POST' (Reset-Body $email 'invalid-code' $newPassword) -Expected 400
$null = Request $requester '/api/auth/resetPassword' 'POST' (Reset-Body $unknownEmail $code $newPassword) -Expected 400
$null = Request $requester '/api/auth/resetPassword' 'POST' (Reset-Body $email $code 'weak') -Expected 400
$null = Request $requester '/api/auth/resetPassword' 'POST' (Reset-Body $email $code $newPassword)
$null = Request $requester '/api/auth/resetPassword' 'POST' (Reset-Body $email $code $oldPassword) -Expected 400
$null = Request $browserA '/api/account/me' -Expected 401
$null = Request $browserB '/api/account/me' -Expected 401
Write-Output 'PASS: neutral reset requests, real SMTP/reset page, invalid/wrong-account/weak/reused-code rejection; reset invalidates both existing browser sessions.'

$fresh = New-Session
Set-Token $fresh
$null = Request $fresh '/api/auth/login?useCookies=true' 'POST' @{ email=$email; password=$oldPassword } -Expected 401
$null = Request $fresh '/api/auth/login?useCookies=true' 'POST' @{ email=$email; password=$newPassword }
$me = (Request $fresh '/api/account/me').Content | ConvertFrom-Json
if ($me.email -ne $email) { throw 'Reset account identity mismatch.' }
Set-Token $fresh
$null = Request $fresh '/api/auth/logout' 'POST' -Expected 204
$null = Request $fresh '/api/account/me' -Expected 401
Write-Output 'PASS: old password rejected, new password accepted, protected logout clears the browser session.'
Write-Output 'Synthetic accounts/emails remain in Docker only. Token expiry is covered separately by API tests; no host clock changes are made.'
