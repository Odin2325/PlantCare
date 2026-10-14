# Resolve approved staging confirmation/reset links without forwarding cookies.
function Resolve-StageMailLink {
    param([string]$PrivateFile,[string]$ExpectedPath)
    if($ExpectedPath -notin @('/api/auth/confirmEmail','/reset-password')){throw 'Unexpected approved email route.'}
    try {$uri=[Uri]([IO.File]::ReadAllText($PrivateFile).Trim())}
    catch {throw 'Private email link unavailable or invalid; contents withheld.'}
    $handler=[Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect=$false
    $handler.UseCookies=$false
    $client=[Net.Http.HttpClient]::new($handler)
    $client.Timeout=[TimeSpan]::FromSeconds(20)
    try {
        for($n=0;$n -lt 5;$n++) {
            if($uri.Scheme -ne 'https' -or -not $uri.IsDefaultPort -or $uri.UserInfo){throw 'Unexpected private email URL.'}
            if($uri.Host -eq 'staging.plantacare.org'){
                if($uri.AbsolutePath -ne $ExpectedPath){throw 'Unexpected staging email route.'}
                return $uri
            }
            if($uri.Host -notmatch '^[a-z0-9.-]+\.sendibt[23]\.com$'){throw 'Unexpected email redirect origin.'}
            try{$response=$client.GetAsync($uri).GetAwaiter().GetResult()}
            catch{throw 'Email redirect connection failed; private URL withheld.'}
            try {
                if([int]$response.StatusCode -notin @(301,302,303,307,308) -or -not $response.Headers.Location){throw 'Email link did not redirect as expected.'}
                try {$uri=[Uri]::new($uri,$response.Headers.Location)}
                catch {throw 'Invalid private redirect; contents withheld.'}
            } finally{$response.Dispose()}
        }
        throw 'Too many email redirects.'
    } finally{$client.Dispose();$handler.Dispose()}
}
