#!/usr/bin/env bash
# Configuration-only maintenance of the fixed staging project. No volume removal,
# image upgrade, schema migration, Docker daemon change or raw-log export.
set -euo pipefail
test "$(id -u)" = 0
candidate=$1
old_hash=$2
new_hash=$3
mode=${4:-validate}
[[ "$mode" == validate || "$mode" == apply ]]
[[ "$candidate" =~ ^[a-f0-9]{40}$ && "$old_hash" =~ ^[a-f0-9]{64}$ && "$new_hash" =~ ^[a-f0-9]{64}$ ]]
base=/home/ubuntu/plantcare-staging
cd "$base/releases/$candidate"
test "$(sha256sum compose.budget.yaml | cut -d' ' -f1)" = "$old_hash"
test "$(sha256sum "$base/compose.budget.logging.yaml" | cut -d' ' -f1)" = "$new_hash"
sha256sum -c SHA256SUMS >/dev/null
test ! -e compose.budget.pre-log-rotation.yaml
test "$(df --output=avail -B1 "$base" | tail -1)" -gt 5368709120
export PLANTCARE_LOG_CANDIDATE="$candidate"
# Parse resolved configuration only in memory; never print/save environment data.
python3 - "$base/compose.budget.logging.yaml" <<'PY'
import json, os, subprocess, sys
candidate=os.environ['PLANTCARE_LOG_CANDIDATE']
base=['docker','compose','--project-name','plantcare-staging','--env-file','../../.env','--env-file','images.env','-f','compose.yaml']
def config(override):
    return json.loads(subprocess.check_output(base+['-f',override,'--profile','operations','config','--format','json'],stderr=subprocess.DEVNULL))
before=config('compose.budget.yaml'); after=config(sys.argv[1])
expected={'driver':'json-file','options':{'max-size':'10m','max-file':'3'}}
# Compose preserves the YAML extension defining the shared logging anchor.
for document in (before,after):
    if 'x-bounded-logging' in document:
        assert document.pop('x-bounded-logging')==expected
assert before['services'].keys()==after['services'].keys()
for name, service in after['services'].items():
    assert service['logging']==expected, 'Unexpected logging policy'
    service.pop('logging')
    before['services'][name].pop('logging',None)
assert before==after, 'Refusing a change outside logging'
manifest=json.load(open('manifest.json'))
assert manifest['candidate']==candidate and manifest.get('accountErasureProtocol')==1
for name in ('database','api','worker','proxy'):
    current=json.loads(subprocess.check_output(['docker','inspect','plantcare-staging-'+name+'-1']))[0]
    assert current['State']['Running']
    if name in ('api','database'): assert current['State']['Health']['Status']=='healthy'
    assert current['Image']==subprocess.check_output(['docker','image','inspect','--format','{{.Id}}',before['services'][name]['image']],text=True).strip(), 'Local image tag drift'
    if name in ('api','worker'):
        expected_image=next(i['id'] for i in manifest['images'] if i['tag']==before['services'][name]['image'])
        assert current['Image']==expected_image, 'Application image drift'
print('PASS: only logging changes; healthy services and pinned images verified.')
PY
if test "$mode" = validate; then
    printf 'PASS: validation only; no running configuration changed.\n'
    exit 0
fi
cp -p compose.budget.yaml compose.budget.pre-log-rotation.yaml
dc() { docker compose --project-name plantcare-staging --env-file ../../.env --env-file images.env -f compose.yaml -f compose.budget.yaml "$@"; }
rollback() {
    code=$?
    trap - EXIT
    if test "$code" != 0; then
        cp -p compose.budget.pre-log-rotation.yaml compose.budget.yaml
        if dc up --no-build --pull never --no-deps -d --wait --wait-timeout 180 database api worker proxy >/dev/null 2>&1; then
            printf 'Logging maintenance failed; previous configuration reapplied.\n' >&2
        else
            printf 'Logging maintenance and recovery failed; operator action required.\n' >&2
        fi
    fi
    exit "$code"
}
trap rollback EXIT
cp "$base/compose.budget.logging.yaml" compose.budget.yaml
chmod 644 compose.budget.yaml
dc config --quiet
# Stop writers first; keep all named database/key/health/proxy volumes. Existing
# container stdout/stderr history is removed as Docker replaces each container.
dc stop worker api >/dev/null 2>&1
dc up --no-build --pull never --no-deps --force-recreate -d --wait --wait-timeout 180 database >/dev/null 2>&1
dc up --no-build --pull never --no-deps --force-recreate -d --wait --wait-timeout 180 api worker >/dev/null 2>&1
dc up --no-build --pull never --no-deps --force-recreate -d --wait --wait-timeout 120 proxy >/dev/null 2>&1
python3 - <<'PY'
import json,subprocess
for name in ('database','api','worker','proxy'):
    c=json.loads(subprocess.check_output(['docker','inspect','plantcare-staging-'+name+'-1']))[0]
    assert c['State']['Running']
    if name in ('database','api'): assert c['State']['Health']['Status']=='healthy'
    assert c['HostConfig']['LogConfig']=={'Type':'json-file','Config':{'max-size':'10m','max-file':'3'}}
print('PASS: all four running containers use 10m/3-file rotation.')
PY
curl --fail --silent --retry 6 --retry-delay 5 --retry-all-errors --max-time 10 https://staging.plantacare.org/health/ready >/dev/null
curl --fail --silent --retry 6 --retry-delay 5 --retry-all-errors --max-time 10 https://staging.plantacare.org/health/operations >/dev/null
docker exec plantcare-staging-api-1 dotnet PlantCare.Api.dll --check-runtime-access api >/dev/null 2>&1
dc run -T --interactive=false --rm --no-deps --pull never worker-db-check >/dev/null 2>&1
printf 'PASS: readiness, operations and restricted runtime access after logging maintenance.\n'
