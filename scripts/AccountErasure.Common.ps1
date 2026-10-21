function Export-CurrentAccountErasureLedger {
    param([ValidateSet('Local','Vps')][string]$Source,[Parameter(Mandatory)][string]$Destination)
    if(Test-Path -LiteralPath $Destination){throw 'Current erasure ledger destination must be new.'}
    $run=[Guid]::NewGuid().ToString('N')
    $containerFile='/tmp/plantcare-erasures-'+$run+'.json'
    if($Source -eq 'Local'){
        try {
            $null=Invoke-BackupDocker @('exec','plantcare-local-api-1','dotnet','PlantCare.Api.dll','--export-account-erasures','plantcare-local',$containerFile)
            $null=Invoke-BackupDocker @('cp',('plantcare-local-api-1:'+$containerFile),$Destination)
        } finally { $null=Invoke-BackupDocker @('exec','plantcare-local-api-1','rm','-f','--',$containerFile) }
        $expected='plantcare-local'
    } else {
        $vps=Get-VpsBackupConfiguration
        $remote='/home/ubuntu/plantcare-staging/erasure-capture-'+$run
        try {
            $null=Invoke-VpsSsh $vps ("set -e; mkdir -m 700 '$remote'; sudo -n docker exec plantcare-staging-api-1 dotnet PlantCare.Api.dll --export-account-erasures plantcare-vps-staging '$containerFile'; sudo -n docker cp 'plantcare-staging-api-1:$containerFile' '$remote/ledger.json'; sudo -n chown ubuntu:ubuntu '$remote/ledger.json'; chmod 600 '$remote/ledger.json'")
            Invoke-VpsScp $vps ($vps.user+'@'+$vps.host+':'+$remote+'/ledger.json') $Destination
        } finally {
            $null=Invoke-VpsSsh $vps ("sudo -n docker exec plantcare-staging-api-1 rm -f -- '$containerFile'; rm -f -- '$remote/ledger.json'; rmdir '$remote'")
        }
        $expected='plantcare-vps-staging'
    }
    $ledger=Get-Content -LiteralPath $Destination -Raw|ConvertFrom-Json
    if($ledger.version -ne 1 -or $ledger.source -ne $expected -or ([DateTimeOffset]::UtcNow-[DateTimeOffset]$ledger.capturedAtUtc).TotalMinutes -gt 15){throw 'Current erasure authority is invalid or stale; do not start recovered services.'}
}
