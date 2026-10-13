# Read-only public deployment checks. Requires PowerShell 7; no accounts created.
[CmdletBinding()]
param(
    [ValidatePattern('^(?=.{1,253}$)(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,63}$')]
    [string]$Hostname = 'staging.plantacare.org',
    [Parameter(Mandatory)]
    [string]$ExpectedIPv4
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 is required.' }
$address = $null
if (-not [Net.IPAddress]::TryParse($ExpectedIPv4, [ref]$address) -or
    $address.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork) {
    throw 'ExpectedIPv4 must be the hosting server IPv4 address.'
}

try { $addresses = @([Net.Dns]::GetHostAddresses($Hostname)) }
catch { throw 'DNS lookup failed. Check the staging record and allow DNS caches to expire.' }
$ipv4 = @($addresses | Where-Object AddressFamily -eq InterNetwork | ForEach-Object ToString | Sort-Object -Unique)
if ($ipv4.Count -ne 1 -or $ipv4[0] -ne $address.ToString()) {
    throw 'DNS does not resolve exclusively to the expected server IPv4. Review conflicting records or propagation.'
}
if (@($addresses | Where-Object AddressFamily -eq InterNetworkV6).Count -gt 0) {
    throw 'IPv6 is present. This IPv4-only first-deployment check requires removing unverified AAAA records first.'
}
Write-Output 'PASS: staging DNS resolves to the expected server.'

# Keep default certificate/hostname validation. Do not follow redirects or accept
# cookies; never print remote bodies, headers, tokens or exception payloads.
$handler = [Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$handler.UseCookies = $false
$client = [Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(20)
function Read-Endpoint {
    param([string]$Url, [int[]]$Expected)
    try { $response = $client.GetAsync($Url).GetAwaiter().GetResult() }
    catch { throw 'Endpoint connection or TLS validation failed. Check DNS, firewall, services and the public certificate.' }
    if ([int]$response.StatusCode -notin $Expected) {
        $status = [int]$response.StatusCode
        $response.Dispose()
        throw "Endpoint returned unexpected HTTP status $status."
    }
    return $response
}
try {
    $response = Read-Endpoint "http://$Hostname/" @(301, 302, 307, 308)
    try {
        $location = $response.Headers.Location
        if ($null -eq $location -or -not $location.IsAbsoluteUri -or
            $location.Scheme -ne 'https' -or $location.Host -ne $Hostname -or
            -not $location.IsDefaultPort -or $location.AbsolutePath -ne '/' -or
            $location.Query -or $location.Fragment -or $location.UserInfo) {
            throw 'HTTP must redirect directly to the same staging hostname over standard HTTPS.'
        }
    } finally { $response.Dispose() }
    Write-Output 'PASS: HTTP redirects to the staging HTTPS origin.'

    foreach ($path in @('/health/live', '/health/ready', '/', '/login', '/api/unknown-staging-check')) {
        $expected = if ($path -eq '/api/unknown-staging-check') { 404 } else { 200 }
        $response = Read-Endpoint "https://$Hostname$path" @($expected)
        try {
            if ($path -in @('/', '/login')) {
                $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if ($body -notmatch '<app-root(?:\s|>)' -or
                    $response.Content.Headers.ContentType.MediaType -ne 'text/html') {
                    throw 'Expected the PlantCare Angular application shell.'
                }
            }
        } finally { $response.Dispose() }
    }
    Write-Output 'PASS: trusted HTTPS, liveness/readiness, SPA deep link and API 404.'
    Write-Output 'Next: perform real-email, worker, device/push and recovery acceptance. These checks do not cover them.'
} finally {
    $client.Dispose()
    $handler.Dispose()
}
