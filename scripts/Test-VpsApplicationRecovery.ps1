# Restore the encrypted VPS snapshot only into a new local Docker project.
# No source data changes/deployment, external sends or schema rollback. Apply
# additive migrations and the freshly exported live erasure ledger only to target.
[CmdletBinding()]
param([ValidatePattern('^[a-f0-9]{40}$')][string]$RuntimeCandidate)
. "$PSScriptRoot/VpsBackup.Common.ps1"
$lock=Enter-BackupLock
$run=[Guid]::NewGuid().ToString('N')
$project='plantcare-app-recovery-'+$run.Substring(0,12)
$scratch=Join-Path $BackupState "scratch/app-recovery-$run"
$authority=Join-Path $BackupState "scratch/erasure-authority-$run"
$target=@()
$started=$false
$passed=$false
$origin='https://localhost:19443'
$session=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
function Request([string]$Path,[string]$Method='GET',[object]$Body,[int]$Expected=200) {
    if($Path -notmatch '^/[a-zA-Z0-9/?=&-]+$' -or ($Method -notin @('GET','POST') -and -not($Method -eq 'PUT' -and $Path -match '^/api/my-plants/[a-f0-9-]{36}/growing-plan$'))){throw 'Unexpected isolated HTTP request.'}
    # Curl runs inside the internal network. Cookies stay in the disposable
    # client container; private request files stay in restricted scratch.
    $curl=@(('url="'+$origin+$Path+'"'),'insecure','silent','max-time=15',('request="'+$Method+'"'),'cookie="/tmp/cookies"','cookie-jar="/tmp/cookies"','output="/tmp/response"','write-out="%{http_code}"')
    if($origin -eq 'https://localhost:19443'){$curl+='connect-to="localhost:19443:proxy:19443"'}
    if($origin -eq 'https://localhost:19443' -and $session.Headers['X-XSRF-TOKEN']){
        $token=$session.Headers['X-XSRF-TOKEN']
        if($token -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Unexpected antiforgery token format.'}
        $curl+='header="X-XSRF-TOKEN: '+$token+'"'
    }
    if($null -ne $Body){
        $Body|ConvertTo-Json -Depth 8|Set-Content (Join-Path $requestDir 'body.json')
        $curl+=@('header="Content-Type: application/json"','data-binary="@/requests/body.json"')
    }
    $curl|Set-Content (Join-Path $requestDir 'request.conf')
    $status=(Invoke-BackupDocker @('exec',"$project-client-1",'curl','--config','/requests/request.conf')).ToString().Trim()
    if($status -ne "$Expected"){throw "Recovery $Method $Path returned HTTP $status, expected $Expected; content withheld."}
    if($Path -notlike '/health/*'){
        $content=Invoke-BackupDocker @('exec',"$project-client-1",'cat','/tmp/response')
        if($content){return ($content -join "`n")|ConvertFrom-Json}
    }
}
function Set-Xsrf {$session.Headers['X-XSRF-TOKEN']=(Request '/api/antiforgery/token').requestToken}
function MailRequest([string]$Path) {$origin='http://mailpit:8025';Request $Path}
function Wait-Reminder([string]$PlantId) {
    for($n=0;$n -lt 20;$n++){
        $items=@(Request '/api/notifications'|Where-Object userPlantId -eq $PlantId)
        if($items.Count){return $items}
        Start-Sleep -Seconds 2
    }
    throw 'Restored worker did not generate a reminder.'
}
try {
    # Recover only the saved synthetic account's copied Identity record.
    # No public login, public reset or source database changes are performed.
    $account=Get-Content (Join-Path $BackupState 'staging-test-account.json') -Raw|ConvertFrom-Json
    $repo=Get-BackupConfiguration
    $last=Get-Content (Join-Path $BackupState 'vps-last-success.json') -Raw|ConvertFrom-Json
    if($last.snapshotId -notmatch '^[a-f0-9]{64}$'){throw 'Invalid VPS snapshot.'}
    $snapshots=Invoke-BackupRestic @('snapshots','--json','--host','plantcare-vps-staging') $repo.repository|ConvertFrom-Json
    if(-not($snapshots|Where-Object id -eq $last.snapshotId)){throw 'Snapshot is not a VPS backup.'}
    Protect-BackupDirectory $scratch
    Protect-BackupDirectory $authority
    Export-CurrentAccountErasureLedger -Source Vps -Destination (Join-Path $authority 'preflight-erasures.json')
    $null=Invoke-BackupRestic @('restore',$last.snapshotId,'--target',$scratch,'--verify') $repo.repository
    $manifest=Get-Content (Join-Path $scratch 'manifest.json') -Raw|ConvertFrom-Json
    foreach($file in $manifest.files){
        $path=[IO.Path]::GetFullPath((Join-Path $scratch $file.name))
        if(-not $path.StartsWith($scratch+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest escapes private scratch.'}
        if((Get-FileHash $path -Algorithm SHA256).Hash -ne $file.sha256){throw 'Restored file hash mismatch.'}
    }
    $testedCandidate=$manifest.candidate
    if(-not $RuntimeCandidate){$RuntimeCandidate=(Get-VpsBackupConfiguration).candidate}
    $composeFile=Join-Path $scratch 'config/compose.yaml'
    $imageEnvironment=Join-Path $scratch 'config/images.env'
    if($RuntimeCandidate){
        # Validate a new prebuilt runtime against the existing encrypted data
        # before any VPS activation. Snapshot files/hashes remain unmodified.
        $bundleRoot=Join-Path $BackupState "deployment-bundles/$RuntimeCandidate"
        $bundle=Get-Content (Join-Path $bundleRoot 'manifest.json') -Raw|ConvertFrom-Json
        if($bundle.candidate -ne $RuntimeCandidate -or $bundle.accountErasureProtocol -ne 1){throw 'Recovery requires a matching erasure-aware runtime bundle.'}
        if([int]$bundle.reminderSnoozeProtocol -lt [int]$manifest.reminderSnoozeProtocol){throw 'The selected runtime cannot safely process snoozed reminders from this snapshot.'}
        foreach($line in Get-Content (Join-Path $bundleRoot 'SHA256SUMS')){
            if($line -notmatch '^([a-f0-9]{64})  (images.tar|compose.yaml|environment.example|images.env|manifest.json|deploy/Caddyfile|deploy/catalog.example.json|deploy/catalog.houseplants.json|deploy/catalog-images.json)$'){throw 'Unexpected prebuilt checksum entry.'}
            if((Get-FileHash (Join-Path $bundleRoot $Matches[2]) -Algorithm SHA256).Hash -ne $Matches[1]){throw 'Prebuilt payload hash mismatch.'}
        }
        foreach($name in @('api','worker')){
            $image=@($bundle.images|Where-Object tag -eq "plantcare-${name}:$RuntimeCandidate")
            if($image.Count -ne 1){throw 'Prebuilt test runtime image missing.'}
            $manifest.images.$name=$image[0].id
        }
        $testedCandidate=$RuntimeCandidate
        $composeFile=Join-Path $bundleRoot 'compose.yaml'
        $imageEnvironment=Join-Path $bundleRoot 'images.env'
    }
    foreach($name in @('api','worker')){
        $id=$manifest.images.$name
        if($id -notmatch '^sha256:[a-f0-9]{64}$'){throw 'Invalid application image identity.'}
        $null=Invoke-BackupDocker @('image','inspect',$id,'--format','{{.Id}}')
    }
    $sqlImage=$manifest.images.databaseReference
    if($sqlImage -notmatch '^mcr\.microsoft\.com/mssql/server@sha256:[a-f0-9]{64}$'){throw 'Invalid SQL image reference.'}
    $null=Invoke-BackupDocker @('pull',$sqlImage)
    $proxyImage=(Invoke-BackupDocker @('image','inspect','caddy:2-alpine','--format','{{.Id}}')).ToString().Trim()
    if($proxyImage -notmatch '^sha256:[a-f0-9]{64}$'){throw 'Local verification proxy image unavailable.'}
    $mailImage=(Invoke-BackupDocker @('image','inspect','axllent/mailpit:v1.27','--format','{{.Id}}')).ToString().Trim()
    if($mailImage -notmatch '^sha256:[a-f0-9]{64}$'){throw 'Local mail capture image unavailable.'}
    $caddy=Join-Path $scratch 'Caddyfile.verify'
    "localhost:19443 {`n tls internal`n reverse_proxy api:8080`n}"|Set-Content $caddy
    $caddyMount=$caddy.Replace('\','/')
    $requestDir=Join-Path $scratch 'requests'
    Protect-BackupDirectory $requestDir
    $requestMount=$requestDir.Replace('\','/')
    $override=Join-Path $scratch 'compose.app-verify.yaml'
    @"
networks:
  default:
    internal: true
services:
  database:
    image: $sqlImage
    pull_policy: never
    ports: !override []
  api:
    image: $($manifest.images.api)
    pull_policy: never
    ports: !override []
    environment:
      Application__ClientBaseUrl: https://localhost:19443
      Email__ClientBaseUrl: https://localhost:19443
      Email__Host: mailpit
      Email__Port: "1025"
      Email__UseSsl: "false"
      Email__Username: ""
      Email__Password: ""
      GoogleCalendar__Enabled: "false"
      MicrosoftCalendar__Enabled: "false"
  migrate:
    image: $($manifest.images.api)
    pull_policy: never
  worker-db-check:
    image: $($manifest.images.api)
    pull_policy: never
  worker:
    image: $($manifest.images.worker)
    pull_policy: never
    ports: !override []
    environment:
      Notifications__PollIntervalSeconds: "5"
      ExternalCalendarSync__Enabled: "false"
      GoogleCalendar__Enabled: "false"
      MicrosoftCalendar__Enabled: "false"
  proxy:
    image: $proxyImage
    pull_policy: never
    environment:
      PLANTCARE_DOMAIN: localhost:19443
    ports: !override []
    volumes: !override
      - "$($caddyMount):/etc/caddy/Caddyfile:ro"
      - caddy-data:/data
      - caddy-config:/config
  client:
    image: $($manifest.images.api)
    pull_policy: never
    entrypoint: ["sh", "-c", "sleep infinity"]
    environment: {}
    volumes:
      - "$($requestMount):/requests:ro"
  mailpit:
    image: $mailImage
    pull_policy: never
    ports: []
"@|Set-Content $override
    $target=@('compose','--env-file',(Join-Path $scratch 'config/.env'),'--env-file',$imageEnvironment,'-f',$composeFile,'-f',(Join-Path $scratch 'config/compose.budget.yaml'),'-f',$override,'--project-name',$project)
    $started=$true
    $null=Invoke-BackupDocker ($target+@('up','-d','--no-build','--wait','--wait-timeout','300','database'))
    $internal=(Invoke-BackupDocker @('network','inspect',"${project}_default",'--format','{{.Internal}}')).ToString().Trim()
    if($internal -ne 'true'){throw 'Recovery network must block external routing.'}
    $db="$project-database-1"
    $null=Invoke-BackupDocker @('cp',(Join-Path $scratch 'database.bak'),"${db}:/var/opt/mssql/recovery.bak")
    $null=Invoke-BackupSql $db "IF DB_ID(N'PlantCare') IS NOT NULL THROW 51000, 'Target is not empty.', 1; RESTORE DATABASE [PlantCare] FROM DISK=N'/var/opt/mssql/recovery.bak' WITH CHECKSUM,RECOVERY; DBCC CHECKDB ([PlantCare]) WITH NO_INFOMSGS;"
    if(Compare-Object @(Get-Content (Join-Path $scratch 'database-state.txt')) @(Invoke-BackupSql $db $BackupSnapshotSql)){throw 'Restored database table comparison failed.'}
    $null=Invoke-BackupDocker ($target+@('run','--rm','--no-deps','--user','root','--entrypoint','sh','-v',"${scratch}:/capture:ro",'api','-c','tar -xf /capture/keys.tar -C /var/lib/plantcare/keys && chown -R app:app /var/lib/plantcare/keys && chmod 700 /var/lib/plantcare/keys'))
    $keyCommand=@('run','--rm','--no-deps','--entrypoint','sh','api','-c','cd /var/lib/plantcare/keys && sha256sum key-*.xml')
    $keyHashes=@(Invoke-BackupDocker ($target+$keyCommand)|Where-Object {"$_" -match '^[a-f0-9]{64}\s+key-'})
    if(-not $keyHashes.Count){throw 'Restored keys unavailable.'}
    $null=Invoke-BackupDocker ($target+@('run','--rm','--no-deps','migrate','--migrate-database'))
    Export-CurrentAccountErasureLedger -Source Vps -Destination (Join-Path $authority 'current-erasures.json')
    $null=Invoke-BackupDocker ($target+@('run','--rm','--no-deps','--user','root','-e','Recovery__AllowAccountErasureApply=true','-e','Recovery__AccountErasureSource=plantcare-vps-staging','-v',"${authority}:/erasure-authority:ro",'migrate','--apply-account-erasures','/erasure-authority/current-erasures.json'))
    $null=Invoke-BackupDocker ($target+@('run','--rm','--no-deps','migrate','--provision-runtime-users'))
    if($RuntimeCandidate -and $bundle.catalogDataset -eq 'houseplants-en-v1'){
        $null=Invoke-BackupDocker ($target+@('run','--rm','--no-deps','migrate','--upsert-catalog','/operator/catalog.houseplants.json'))
    }
    $null=Invoke-BackupDocker ($target+@('up','-d','--no-build','--wait','--wait-timeout','300','api','worker','proxy','client','mailpit'))
    $ready=$false
    for($n=0;$n -lt 15;$n++){
        try{$null=Request '/health/ready';$ready=$true;break}catch{Start-Sleep -Seconds 2}
    }
    if(-not $ready){throw 'Isolated HTTPS readiness did not become available.'}
    'PASS: database/key/config restore and runtime login remapping.'
    # The saved password is stale: test the restored account's standard reset
    # using ONLY internal captured mail. No messages leave this Docker network.
    Set-Xsrf
    $null=Request '/api/auth/forgotPassword' 'POST' @{email=$account.email}
    $mail=$null
    for($n=0;$n -lt 10;$n++){
        $messages=MailRequest '/api/v1/messages'
        $mail=$messages.messages|Where-Object Subject -eq 'Reset your PlantCare password'|Select-Object -First 1
        if($mail){break};Start-Sleep -Seconds 1
    }
    if(-not $mail){throw 'Restored account reset email was not captured internally.'}
    $message=MailRequest ('/api/v1/message/'+$mail.ID)
    $uri=[Uri][Net.WebUtility]::HtmlDecode([regex]::Match($message.HTML,'href="([^"]+)"').Groups[1].Value)
    if($uri.GetLeftPart([UriPartial]::Authority) -ne $origin -or $uri.AbsolutePath -ne '/reset-password'){throw 'Unexpected internal reset route.'}
    $query=@{}
    foreach($part in $uri.Query.TrimStart('?').Split('&')){$pair=$part.Split('=',2);$query[$pair[0]]=[Uri]::UnescapeDataString($pair[1])}
    if($query.email -ne $account.email -or -not $query.code){throw 'Unexpected internal reset recipient/code.'}
    $newPassword='Pc9!'+[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    $reset=@{email=$account.email;resetCode=$query.code;newPassword=$newPassword}
    $null=Request '/api/auth/resetPassword' 'POST' $reset
    $null=Request '/api/auth/resetPassword' 'POST' $reset -Expected 400
    $null=Request '/api/auth/login?useCookies=true' 'POST' @{email=$account.email;password=$newPassword}
    if((Request '/api/account/me').email -ne $account.email){throw 'Restored account mismatch.'}
    Set-Xsrf
    $species=@(Request '/api/plant-species')|Where-Object commonName -eq 'Demo houseplant'|Select-Object -First 1
    if(-not $species){throw 'Recovered synthetic catalog unavailable.'}
    $plant=Request '/api/my-plants' 'POST' @{plantSpeciesId=$species.id;nickname='Isolated recovery probe';wateringIntervalDays=7;lastWateredAtUtc=[DateTimeOffset]::UtcNow.AddDays(-8).ToString('O')} -Expected 201
    $reminder=@(Wait-Reminder $plant.id)
    if($RuntimeCandidate){$null=Request '/health/operations'}
    if($reminder.Count -ne 1){throw 'Recovered worker produced duplicate reminders.'}
    $null=Invoke-BackupDocker ($target+@('restart','worker'))
    Start-Sleep -Seconds 12
    $again=@(Request '/api/notifications'|Where-Object userPlantId -eq $plant.id)
    if($again.Count -ne 1 -or $again[0].id -ne $reminder[0].id){throw 'Reminder changed after worker restart.'}
    $complete=Request "/api/my-plants/$($plant.id)/care/Watering/complete" 'POST' @{completedAtUtc=[DateTimeOffset]::UtcNow.AddSeconds(-1).ToString('O');notes='Isolated restored-copy care test'}
    $history=@(Request "/api/my-plants/$($plant.id)/care/history")
    if($history.Count -ne 1 -or $history[0].id -ne $complete.event.id){throw 'Recovered care history mismatch.'}
    # Newer runtimes expose optional planting dates; older supported bundles do
    # not. Keep this probe capability-based so historical recovery remains usable.
    $supportsGrowingPlan = $plant.PSObject.Properties.Name -contains 'plannedSowingOn'
    if($supportsGrowingPlan){
        $planned=Request "/api/my-plants/$($plant.id)/growing-plan" 'PUT' @{plannedSowingOn='2027-03-01';plannedPlantingOutOn='2027-05-15';expectedHarvestOn='2027-08-01'}
        $reloaded=Request "/api/my-plants/$($plant.id)"
        if($reloaded.plannedSowingOn -ne '2027-03-01' -or $reloaded.expectedHarvestOn -ne '2027-08-01'){throw 'Restored planting plan did not persist.'}
        if((Request "/api/my-plants/$($plant.id)/care/history").id -ne $complete.event.id){throw 'Planting plan changed care history.'}
        'PASS: restored runtime saves planting dates without changing care history.'
    }
    'PASS: fresh sign-in, restored worker/restart deduplication and care history.'
    # Failed activation only: intentionally non-starting API, no migrations/data rewrite.
    $failure=Join-Path $scratch 'compose.failed-activation.yaml'
    @'
services:
  api:
    entrypoint: ["/bin/sh", "-c", "exit 42"]
    restart: "no"
'@|Set-Content $failure
    $rejected=$false
    try{$null=Invoke-BackupDocker ($target+@('-f',$failure,'up','-d','--no-deps','--force-recreate','--wait','--wait-timeout','30','api'))}catch{$rejected=$true}
    if(-not $rejected){throw 'Failed activation probe unexpectedly succeeded.'}
    $exitCode=(Invoke-BackupDocker @('inspect',"$project-api-1",'--format','{{.State.ExitCode}}')).ToString().Trim()
    if($exitCode -ne '42'){throw 'Failed activation did not reach the intended exit-42 probe.'}
    $null=Invoke-BackupDocker ($target+@('up','-d','--no-deps','--force-recreate','--wait','--wait-timeout','120','api'))
    $activeImage=(Invoke-BackupDocker @('inspect',"$project-api-1",'--format','{{.Image}}')).ToString().Trim()
    if($activeImage -ne $manifest.images.api){throw 'Rollback did not restore the pinned application image.'}
    if($supportsGrowingPlan -and (Request "/api/my-plants/$($plant.id)").expectedHarvestOn -ne '2027-08-01'){throw 'Failed activation lost the planting plan.'}

    $null=Request '/health/ready'
    if((Request '/api/account/me').email -ne $account.email){throw 'Session did not survive activation rollback.'}
    $after=@(Request "/api/my-plants/$($plant.id)/care/history")
    if($after.Count -ne 1 -or $after[0].id -ne $history[0].id){throw 'Rollback lost care history.'}
    $keysAfter=@(Invoke-BackupDocker ($target+$keyCommand)|Where-Object {"$_" -match '^[a-f0-9]{64}\s+key-'})
    if(Compare-Object $keyHashes $keysAfter){throw 'Key hashes changed during recovery/rollback.'}
    $null=Request '/api/auth/logout' 'POST' -Expected 204
    $null=Request '/api/account/me' -Expected 401
    $passed=$true
    Write-BackupStatus 'vps-last-app-recovery' @{status='success';atUtc=[DateTimeOffset]::UtcNow.ToString('O');snapshotId=$last.snapshotId;candidate=$testedCandidate;growingPlanVerified=$supportsGrowingPlan;snapshotCandidate=$manifest.candidate;checks=@('sql-checkdb-table-comparison','key-config-hashes','fresh-source-erasure-authority','target-migration-erasure-reconciliation-before-start','runtime-login-remapping','internal-mail-password-recovery','reset-reuse-rejected','fresh-login','worker-restart-deduplication','care-history','failed-activation-rollback','logout');savedPassword='not-accepted';sourceCookie='not-run';externalIntegrations='not-run';replacementVps='not-run'}
    'PASS: failed API activation rejected; pinned runtime restored with session/history/keys intact; logout enforced.'
} finally {
    try {
        if($started){
            if($project -notmatch '^plantcare-app-recovery-[a-f0-9]{12}$'){throw 'Unexpected recovery project; cleanup refused.'}
            $null=Invoke-BackupDocker ($target+@('down','--volumes'))
        }
        Remove-BackupScratch $scratch
        Remove-BackupScratch $authority
        if(-not $passed){Write-BackupStatus 'vps-last-app-recovery' @{status='failed';atUtc=[DateTimeOffset]::UtcNow.ToString('O');message='Isolated application recovery failed; details withheld.'}}
        $account=$null;$session=$null;$newPassword=$null;$reset=$null;$message=$null;$query=$null
    } finally {$lock.Dispose()}
}
