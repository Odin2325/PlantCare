# Fixed local Docker stack only. No credentials are printed or put in arguments.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        $dockerBin = Join-Path $env:LOCALAPPDATA 'Programs\DockerDesktop\resources\bin'
        if (-not (Test-Path (Join-Path $dockerBin 'docker.exe'))) { $dockerBin = Join-Path $env:ProgramFiles 'Docker\Docker\resources\bin' }
        $env:Path = $dockerBin + ';' + $env:Path
    }
    foreach ($name in @('DatabaseRuntime__ApiPassword', 'DatabaseRuntime__WorkerPassword')) {
        $result = docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local run --rm --no-deps -e ($name + '=short') migrate --provision-runtime-users 2>&1
        if ($LASTEXITCODE -eq 0) { throw 'Provisioning accepted an invalid runtime credential.' }
    }
    docker exec plantcare-local-api-1 dotnet PlantCare.Api.dll --check-runtime-access api
    if ($LASTEXITCODE -ne 0) { throw 'API runtime permission checks failed.' }
    docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local run --rm --no-deps worker-db-check
    if ($LASTEXITCODE -ne 0) { throw 'Worker runtime permission checks failed.' }
    $sqlRunner = @('exec', 'plantcare-local-database-1', 'sh', '-c', 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -Q "$1"', 'sql')
    # A temporary role-name read grant contains no account secrets. Verify the
    # provisioner rejects unmanaged grants, then remove only this probe grant.
    $grant = 'USE PlantCare; IF EXISTS (SELECT 1 FROM sys.database_permissions WHERE grantee_principal_id=USER_ID(''plantcare_worker'') AND major_id=OBJECT_ID(''dbo.AspNetRoles'') AND permission_name=''SELECT'') THROW 51000, ''Probe grant already exists; refusing to alter it.'', 1; GRANT SELECT ON OBJECT::dbo.AspNetRoles TO plantcare_worker;'
    docker @sqlRunner $grant | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not establish the bounded permission-conflict probe.' }
    try {
        $result = docker compose --env-file .env.local -f compose.yaml -f compose.local.yaml --project-name plantcare-local run --rm --no-deps migrate --provision-runtime-users 2>&1
        if ($LASTEXITCODE -eq 0 -or ($result -join "`n") -notmatch 'unmanaged permissions') { throw 'Provisioner did not reject an unmanaged runtime grant.' }
    }
    finally {
        docker @sqlRunner 'USE PlantCare; REVOKE SELECT ON OBJECT::dbo.AspNetRoles FROM plantcare_worker;' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not remove the temporary role-name read probe grant.' }
    }
    Write-Output 'PASS: separate runtime principals, required table permissions and denied schema/Identity-role/migration-history operations.'
}
finally { Pop-Location }
