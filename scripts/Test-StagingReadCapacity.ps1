# Bounded public catalog reads only. No login, writes or external email/push.
[CmdletBinding()]
param([ValidateRange(20,500)][int]$Requests=200,[ValidateRange(1,5)][int]$Concurrency=5)
. "$PSScriptRoot/VpsBackup.Common.ps1"
$lock=Enter-BackupLock
$client=[Net.Http.HttpClient]::new()
$client.Timeout=[TimeSpan]::FromSeconds(10)
$cancellation=[Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(90))
function Read-Metric([object]$Vps,[string]$Name,[string]$Command) {
    for($attempt=0;$attempt -lt 2;$attempt++){
        try{return Invoke-VpsSsh $Vps $Command}catch{if($attempt -eq 1){throw "Read-only capacity metric unavailable: $Name."};Start-Sleep -Seconds 1}
    }
}
function Metrics {
    $vps=Get-VpsBackupConfiguration
    $memoryLine=@(Read-Metric $vps 'memory' 'cat /proc/meminfo'|Where-Object {$_ -match '^MemAvailable:'})[0]
    $memory=[regex]::Match($memoryLine,'\d+').Value
    $disk=(Read-Metric $vps 'disk' 'df -B1 --output=avail /home/ubuntu/plantcare-staging | tail -1').ToString().Trim()
    $states=@(Read-Metric $vps 'container-state' "sudo -n docker inspect --format '{{.Name}} {{.RestartCount}} {{.State.OOMKilled}} {{.State.Running}}' plantcare-staging-api-1 plantcare-staging-worker-1 plantcare-staging-database-1 plantcare-staging-proxy-1")
    $stats=@(Read-Metric $vps 'container-stats' "sudo -n docker stats --no-stream --format '{{.Name}} {{.MemUsage}} {{.CPUPerc}}' plantcare-staging-api-1 plantcare-staging-worker-1 plantcare-staging-database-1 plantcare-staging-proxy-1")
    return @{availableMemoryKiB=[long]$memory;freeDiskBytes=[long]$disk;containerStates=$states;containerStats=$stats}
}
try {
    $before=Metrics
    if($before.availableMemoryKiB -lt 512*1024 -or $before.freeDiskBytes -lt 5GB){throw 'Insufficient host headroom for a capacity sample.'}
    $results=@(1..$Requests|ForEach-Object -Parallel {
        $http=$using:client;$cancel=$using:cancellation
        $timer=[Diagnostics.Stopwatch]::StartNew()
        $status=0
        try {
            $response=$http.GetAsync('https://staging.plantacare.org/api/plant-species',$cancel.Token).GetAwaiter().GetResult()
            try{$status=[int]$response.StatusCode;$null=$response.Content.ReadAsByteArrayAsync($cancel.Token).GetAwaiter().GetResult()}finally{$response.Dispose()}
        } catch {$status=0}
        $timer.Stop()
        if($status -ne 200){$cancel.Cancel()}
        [pscustomobject]@{status=$status;milliseconds=$timer.Elapsed.TotalMilliseconds}
        Start-Sleep -Milliseconds 250
    } -ThrottleLimit $Concurrency)
    $after=Metrics
    $latencies=@($results.milliseconds|Sort-Object)
    $errors=@($results|Where-Object status -ne 200).Count
    $unchanged= -not (Compare-Object $before.containerStates $after.containerStates)
    $allRunning=$after.containerStates.Count -eq 4 -and @($after.containerStates|Where-Object {$_ -notmatch '^/plantcare-staging-[a-z]+-1 \d+ false true$'}).Count -eq 0
    $passed=$errors -eq 0 -and $unchanged -and $allRunning -and $after.availableMemoryKiB -ge 512*1024 -and $after.freeDiskBytes -ge 5GB
    $result=@{passed=$passed;atUtc=[DateTimeOffset]::UtcNow.ToString('O');requests=$Requests;concurrency=$Concurrency;perLanePauseMs=250;errors=$errors;p50ms=[Math]::Round($latencies[[int][Math]::Ceiling($latencies.Count*0.5)-1],1);p95ms=[Math]::Round($latencies[[int][Math]::Ceiling($latencies.Count*0.95)-1],1);maximumMs=[Math]::Round($latencies[-1],1);before=$before;after=$after;scope='anonymous catalog read sample only'}
    Write-BackupStatus 'staging-capacity' $result
    "Catalog sample: $Requests requests, concurrency $Concurrency, errors $errors, p95 $($result.p95ms) ms."
    if(-not $passed){throw 'Bounded capacity sample failed; inspect private non-secret metrics.'}
} finally {$client.Dispose();$cancellation.Dispose();$lock.Dispose()}
