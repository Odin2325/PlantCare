#!/usr/bin/env bash
# Fixed staging host only. No secret values or SQL output are printed.
set -euo pipefail
test "$(id -u)" = 0
run=$1
candidate=$2
mode=${3:-capture}
[[ "$run" =~ ^[a-f0-9]{32}$ ]]
[[ "$candidate" =~ ^[a-f0-9]{40}$ ]]
base=/home/ubuntu/plantcare-staging
scratch="$base/backup-scratch/$run"
sqlfile="/var/opt/mssql/plantcare-vps-$run.bak"
if test "$mode" = cleanup; then
    test "$(realpath -m "$scratch")" = "$scratch"
    docker exec plantcare-staging-database-1 rm -f -- "$sqlfile"
    rm -rf -- "$scratch"
    exit 0
fi
test "$mode" = capture
test ! -e "$scratch"
test "$(df --output=avail -B1 "$base" | tail -1)" -gt 5368709120
cd "$base/releases/$candidate"
dc() { docker compose --project-name plantcare-staging --env-file ../../.env --env-file images.env -f compose.yaml -f compose.budget.yaml "$@"; }
for service in api worker database; do
    test "$(docker inspect --format '{{.State.Running}}' "plantcare-staging-$service-1")" = true
done
install -d -o ubuntu -g ubuntu -m 0700 "$base/backup-scratch" "$scratch" "$scratch/config" "$scratch/config/deploy"
stopped=false
resume() {
    code=$?
    trap - EXIT
    if test "$stopped" = true; then
        if ! dc up --no-build --pull never -d --wait --wait-timeout 300 api worker >/dev/null 2>&1; then
            printf 'VPS application restart failed after capture.\n' >&2
            exit 1
        fi
    fi
    exit "$code"
}
trap resume EXIT
umask 077
stopped=true
dc stop api worker >/dev/null 2>&1
sql() { docker exec -i plantcare-staging-database-1 sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -i /dev/stdin'; }
sql < "$base/backup-snapshot.sql" > "$scratch/database-state.txt"
printf "BACKUP DATABASE [PlantCare] TO DISK=N'%s' WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'%s' WITH CHECKSUM;\n" "$sqlfile" "$sqlfile" | sql >/dev/null
docker cp "plantcare-staging-database-1:$sqlfile" "$scratch/database.bak"
dc run --rm --no-deps --pull never --user root --entrypoint sh -v "$scratch:/capture" api -c 'test -n "$(find /var/lib/plantcare/keys -name "key-*.xml" -print -quit)" && tar -cf /capture/keys.tar -C /var/lib/plantcare/keys .' >/dev/null 2>&1
cp "$base/.env" "$scratch/config/.env"
cp compose.yaml compose.budget.yaml images.env "$scratch/config/"
cp deploy/Caddyfile deploy/catalog.example.json "$scratch/config/deploy/"
python3 - "$scratch" "$candidate" <<'PY'
import datetime,hashlib,json,pathlib,subprocess,sys
root=pathlib.Path(sys.argv[1])
def inspect(*args):
    return subprocess.check_output(['docker',*args],text=True).strip()
images={name:inspect('inspect','--format','{{.Image}}','plantcare-staging-'+name+'-1') for name in ('api','worker','database','proxy')}
images['databaseReference']=inspect('image','inspect','--format','{{index .RepoDigests 0}}',images['database'])
files=[{'name':p.relative_to(root).as_posix(),'sha256':hashlib.file_digest(p.open('rb'),'sha256').hexdigest()} for p in sorted(root.rglob('*')) if p.is_file()]
(root/'manifest.json').write_text(json.dumps({'createdAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'candidate':sys.argv[2],'images':images,'files':files},indent=2))
PY
chown -R ubuntu:ubuntu "$scratch"
find "$scratch" -type f -exec chmod 600 {} +
printf 'PASS: consistent SQL/key/config capture; application restart follows.\n'
