# Destructive expiry verification ONLY in a generated disposable repository.
[CmdletBinding()]
param()
. "$PSScriptRoot/LocalBackup.Common.ps1"
$originalState=$BackupState
$tool=Join-Path $BackupState 'tools/restic.exe'
$run=[Guid]::NewGuid().ToString('N')
$fixture=Join-Path $BackupState ('scratch/retention-test-'+$run)
Protect-BackupDirectory $fixture
$repository=Join-Path $fixture 'repository'
$data=Join-Path $fixture 'data'
$null=New-Item -ItemType Directory -Path $repository,$data
$names=@('RESTIC_PASSWORD','RESTIC_PASSWORD_FILE','RESTIC_PASSWORD_COMMAND','RESTIC_REPOSITORY','RESTIC_REPOSITORY_FILE')
$saved=@{}
foreach($name in $names){$saved[$name]=[Environment]::GetEnvironmentVariable($name,'Process');[Environment]::SetEnvironmentVariable($name,$null,'Process')}
$env:RESTIC_PASSWORD=[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$BackupState=$fixture
$script:prunes=0
function Get-BackupConfiguration { return @{repository=$repository} }
function Invoke-BackupRestic([string[]]$Arguments,[string]$Repository){
    if($Repository -ne $repository -or -not $Repository.StartsWith($fixture+[IO.Path]::DirectorySeparatorChar)){throw 'Refusing non-fixture retention operation.'}
    if($Arguments[0] -eq 'prune'){$script:prunes++}
    $output=& $tool --repo $Repository --cache-dir (Join-Path $fixture 'cache') @Arguments 2>&1
    if($LASTEXITCODE){throw 'Disposable retention repository operation failed; output withheld.'}
    return $output
}
function Snapshot([string]$Scope,[int]$Age,[string]$Label){
    $Label|Set-Content (Join-Path $data 'payload.txt')
    Push-Location $data
    try{$output=Invoke-BackupRestic @('backup','.','--host',$Scope,'--tag',$Scope,'--time',([DateTime]::UtcNow.AddDays(-$Age).ToString('yyyy-MM-dd HH:mm:ss')),'--json') $repository}finally{Pop-Location}
    $summary=$output|ForEach-Object{try{$_|ConvertFrom-Json}catch{}}|Where-Object message_type -eq summary|Select-Object -Last 1
    if($summary.snapshot_id -notmatch '^[a-f0-9]{64}$'){throw 'Fixture snapshot missing.'}
    return $summary.snapshot_id
}
try {
    $null=Invoke-BackupRestic @('init') $repository
    $null=Snapshot plantcare-local 31 old-local
    $null=Snapshot plantcare-vps-staging 31 old-vps
    $local=Snapshot plantcare-local 0 recent-local
    $vps=Snapshot plantcare-vps-staging 0 recent-vps
    Write-BackupStatus 'last-success' @{status='success';snapshotId=$local;atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    Write-BackupStatus 'vps-last-success' @{status='success';snapshotId=$vps;atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    $plan=Get-CurrentBackupRetentionPlan
    if($plan.expireIds.Count -ne 2){throw 'Disposable expiry plan is incorrect.'}
    $tampered=$plan|ConvertTo-Json -Depth 6|ConvertFrom-Json
    $tampered.expireIds=@($local)
    $rejected=$false;try{Apply-BackupRetentionPlan $tampered}catch{$rejected=$true}
    if(-not $rejected){throw 'A modified expiry set was accepted.'}
    $changed=Snapshot plantcare-local 0 changed-repository
    $rejected=$false;try{Apply-BackupRetentionPlan $plan}catch{$rejected=$true}
    if(-not $rejected){throw 'A changed repository accepted the old plan.'}
    $null=Invoke-BackupRestic @('forget','--',$changed) $repository
    Apply-BackupRetentionPlan $plan
    $remaining=(Invoke-BackupRestic @('snapshots','--json') $repository)-join "`n"|ConvertFrom-Json
    if(@($remaining).Count -ne 2 -or $local -notin $remaining.id -or $vps -notin $remaining.id){throw 'Fresh snapshots were not preserved.'}
    Write-BackupStatus 'retention-prune-pending' @{pending=$true}
    Apply-BackupRetentionPlan (Get-CurrentBackupRetentionPlan)
    if($script:prunes -ne 2 -or (Get-Content (Join-Path $BackupState 'retention-prune-pending.json') -Raw|ConvertFrom-Json).pending){throw 'Interrupted prune was not resumed without new expiry.'}
    foreach($item in @(@{id=$local;label='recent-local'},@{id=$vps;label='recent-vps'})){
        $target=Join-Path $fixture $item.label
        $null=Invoke-BackupRestic @('restore',$item.id,'--target',$target,'--verify') $repository
        $payload=@(Get-ChildItem -LiteralPath $target -Recurse -File -Filter payload.txt)
        if($payload.Count -ne 1 -or (Get-Content $payload[0].FullName -Raw).Trim() -ne $item.label){throw 'A retained snapshot did not restore correctly.'}
    }
    'PASS: disposable encrypted repository expired only two old snapshots, pruned/checked data, rejected a modified plan and restored both retained backups.'
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$saved[$name],'Process')}
    $BackupState=$originalState
    if($run -notmatch '^[a-f0-9]{32}$' -or [IO.Path]::GetFullPath($fixture) -ne [IO.Path]::GetFullPath((Join-Path $originalState ('scratch/retention-test-'+$run)))){throw 'Unexpected fixture cleanup target.'}
    Remove-BackupScratch $fixture
}
