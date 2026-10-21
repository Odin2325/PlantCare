# Pure planning is separate from repository mutations for review and regression tests.
function New-BackupRetentionPlan([object[]]$Snapshots,[string[]]$ProtectedIds,[DateTimeOffset]$Now=[DateTimeOffset]::UtcNow) {
    $cutoff=$Now.AddDays(-30)
    $seen=[Collections.Generic.HashSet[string]]::new()
    $state=@()
    foreach($s in $Snapshots){
        if($s.id -notmatch '^[a-f0-9]{64}$' -or -not $seen.Add($s.id)){throw 'Invalid or duplicate snapshot identity.'}
        if($s.hostname -notin @('plantcare-local','plantcare-vps-staging') -or @($s.tags).Count -ne 1 -or $s.tags[0] -ne $s.hostname){throw 'Unmanaged snapshot group; automatic expiry refused.'}
        $time=[DateTimeOffset]$s.time
        if($time -gt $Now.AddMinutes(5)){throw 'Future snapshot timestamp; expiry refused.'}
        $state+=@{id=$s.id;hostname=$s.hostname;time=$time.ToUniversalTime().ToString('O')}
    }
    foreach($hostName in @('plantcare-local','plantcare-vps-staging')){
        $group=@($state|Where-Object hostname -eq $hostName|Sort-Object time)
        if(-not $group.Count -or [DateTimeOffset]$group[-1].time -lt $Now.AddHours(-30)){throw 'Both backup groups must have a fresh snapshot before expiry.'}
    }
    foreach($id in $ProtectedIds){if($id -notin $seen){throw 'Verified latest snapshot missing; expiry refused.'}}
    $expired=@($state|Where-Object { [DateTimeOffset]$_.time -lt $cutoff }|ForEach-Object id|Sort-Object)
    if(@($expired|Where-Object {$_ -in $ProtectedIds}).Count){throw 'A verified latest snapshot would expire; expiry refused.'}
    $canonical=($state|Sort-Object id|ForEach-Object { $_.id+'|'+$_.hostname+'|'+$_.time }) -join "`n"
    $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($canonical))).ToLowerInvariant()
    return @{version=1;days=30;createdAtUtc=$Now.ToString('O');cutoffUtc=$cutoff.ToString('O');repositoryStateHash=$hash;expireIds=$expired;protectedIds=@($ProtectedIds|Sort-Object);totalSnapshots=$state.Count}
}

function Get-CurrentBackupRetentionPlan {
    $config=Get-BackupConfiguration
    $snapshots=(Invoke-BackupRestic @('snapshots','--json') $config.repository)-join "`n"|ConvertFrom-Json
    $ids=@()
    foreach($name in @('last-success','vps-last-success')){
        $last=Get-Content (Join-Path $BackupState ($name+'.json')) -Raw|ConvertFrom-Json
        if($last.status -ne 'success' -or ([DateTimeOffset]::UtcNow-[DateTimeOffset]$last.atUtc).TotalHours -gt 30){throw 'Verified successful backups must be fresh before expiry.'}
        $ids+=$last.snapshotId
    }
    New-BackupRetentionPlan @($snapshots) $ids
}

function Apply-BackupRetentionPlan([object]$Plan) {
    $age=([DateTimeOffset]::UtcNow-[DateTimeOffset]$Plan.createdAtUtc).TotalMinutes
    if($Plan.version -ne 1 -or $Plan.days -ne 30 -or $age -lt 0 -or $age -gt 15){throw 'Retention plan is invalid or stale.'}
    $current=Get-CurrentBackupRetentionPlan
    if($current.repositoryStateHash -ne $Plan.repositoryStateHash -or (Compare-Object @($current.protectedIds) @($Plan.protectedIds))){throw 'Repository or latest verified snapshots changed; review a new plan.'}
    # Recompute against the reviewed cutoff rather than trusting a list from disk.
    $config=Get-BackupConfiguration
    $snapshots=(Invoke-BackupRestic @('snapshots','--json') $config.repository)-join "`n"|ConvertFrom-Json
    $expected=@($snapshots|Where-Object { [DateTimeOffset]$_.time -lt [DateTimeOffset]$Plan.cutoffUtc }|ForEach-Object id|Sort-Object)
    if(($expected -join ',') -ne (@($Plan.expireIds) -join ',') -or ([DateTimeOffset]$Plan.cutoffUtc) -ne ([DateTimeOffset]$Plan.createdAtUtc).AddDays(-30)){throw 'Retention plan expiry set is not valid.'}
    $pendingFile=Join-Path $BackupState 'retention-prune-pending.json'
    $pending=(Test-Path $pendingFile) -and (Get-Content $pendingFile -Raw|ConvertFrom-Json).pending -eq $true
    if($expected.Count -or $pending){
        $null=Invoke-BackupRestic @('check') $config.repository
        Write-BackupStatus 'retention-prune-pending' @{pending=$true;atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
        if($expected.Count){$null=Invoke-BackupRestic (@('forget','--')+$expected) $config.repository}
        $null=Invoke-BackupRestic @('prune','--max-unused','0') $config.repository
        $null=Invoke-BackupRestic @('check','--read-data') $config.repository
        Write-BackupStatus 'retention-prune-pending' @{pending=$false;atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    }
    Write-BackupStatus 'retention-last-success' @{status='success';atUtc=[DateTimeOffset]::UtcNow.ToString('O');days=30;expiredCount=$expected.Count;remainingCount=($snapshots.Count-$expected.Count)}
    Write-BackupStatus 'retention-last-attempt' @{status='success';atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
}

function Invoke-ConfiguredBackupRetention {
    $path=Join-Path $BackupState 'retention-policy.json'
    if(-not(Test-Path $path)){return}
    $policy=Get-Content $path -Raw|ConvertFrom-Json
    if($policy.enabled -ne $true){return}
    if($policy.days -ne 30){throw 'Unexpected retention configuration.'}
    try { Apply-BackupRetentionPlan (Get-CurrentBackupRetentionPlan) }
    catch { Write-BackupStatus 'retention-last-attempt' @{status='failed';atUtc=[DateTimeOffset]::UtcNow.ToString('O');problem='backup-retention-failed'};Write-Warning 'Backup captured; retention deferred. Inspect backup retention health.';return }
    Write-BackupStatus 'retention-last-attempt' @{status='success';atUtc=[DateTimeOffset]::UtcNow.ToString('O')}
}
