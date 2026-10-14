#!/usr/bin/env bash
# Fixed synthetic staging host. No schema migrations, secret changes or pruning.
set -euo pipefail
test "$(id -u)" = 0
next=$1
previous=$2
[[ "$next" =~ ^[a-f0-9]{40}$ && "$previous" =~ ^[a-f0-9]{40}$ && "$next" != "$previous" ]]
base=/home/ubuntu/plantcare-staging
cd "$base/releases/$next"
test -f "$base/releases/$previous/compose.budget.yaml"
test "$(df --output=avail -B1 "$base" | tail -1)" -gt 5368709120
sha256sum -c SHA256SUMS >/dev/null
python3 - "$next" <<'PY'
import json,sys
assert json.load(open('manifest.json'))['candidate']==sys.argv[1]
PY
docker load --input images.tar >/dev/null
python3 - <<'PY'
import json,subprocess
for image in json.load(open('manifest.json'))['images']:
    actual=subprocess.check_output(['docker','image','inspect','--format','{{.Id}}',image['tag']],text=True).strip()
    assert actual==image['id']
PY
cp "$base/releases/$previous/compose.budget.yaml" compose.budget.yaml
dc() { docker compose --project-name plantcare-staging --env-file ../../.env --env-file images.env -f compose.yaml -f compose.budget.yaml "$@"; }
dc config --quiet
rollback() {
    code=$?
    trap - EXIT
    if test "$code" != 0; then
        cd "$base/releases/$previous"
        if ! dc up --no-build --pull never --no-deps --force-recreate -d --wait --wait-timeout 180 api worker >/dev/null 2>&1; then
            printf 'Staging activation and previous-runtime recovery failed. Operator action required.\n' >&2
            exit 1
        fi
        printf 'Staging activation failed; previous runtime reapplied.\n' >&2
    fi
    exit "$code"
}
trap rollback EXIT
dc up --no-build --pull never --no-deps --force-recreate -d --wait --wait-timeout 180 api worker >/dev/null 2>&1
for i in $(seq 1 36); do
    if curl --fail --silent --max-time 10 https://staging.plantacare.org/health/ready >/dev/null && curl --fail --silent --max-time 10 https://staging.plantacare.org/health/operations >/dev/null; then
        docker exec plantcare-staging-api-1 dotnet PlantCare.Api.dll --check-runtime-access api >/dev/null 2>&1
        dc run --rm --no-deps --pull never worker-db-check >/dev/null 2>&1
        printf 'PASS: pinned API/worker activation, readiness, operations and restricted runtime access.\n'
        exit 0
    fi
    sleep 5
done
exit 1
