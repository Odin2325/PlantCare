# Requires PowerShell 7 and the compose.local.yaml stack. Creates one synthetic
# account per run; confirmation/reset links and passwords are never printed.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
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
$null = Invoke-LocalRequest '/api/auth/logout' 'POST' -Expected 204
$null = Invoke-LocalRequest '/api/account/me' -Expected 401
Write-Output 'PASS: real cookie login, current account, antiforgery-protected logout, access revoked.'
