"""Disposable local HTTPS -> API -> isolated decoder -> SQL journal acceptance."""
import argparse
import json
import hashlib
import os
import re
import secrets
import shutil
import subprocess
import tempfile
import time
import uuid
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from photo_bundle import ROOT, verify
from photo_capacity import png_fixture
from photo_snapshot_policy import prepare, checkpoint, reject_log_chain_retirement
from photo_mixed_workload import mixed_checkpoint
from photo_large_workload import large_checkpoint, padded_png
from photo_encrypted_recovery import encrypted_recovery
from photo_restored_app import restored_app


def docker(*args, check=True, timeout=300, data=None):
    result = subprocess.run(["docker", *args], input=data, capture_output=True, timeout=timeout)
    if check and result.returncode:
        raise RuntimeError("Disposable photo HTTP Docker operation failed; raw output withheld.")
    return result


def image(tag):
    info = json.loads(docker("image", "inspect", tag).stdout)[0]
    if info["Os"] != "linux" or info["Architecture"] != "amd64":
        raise ValueError("Local Linux amd64 images are required.")
    return info["Id"]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api", required=True, help="Existing local API image; never pulled/built here")
    parser.add_argument("--bundle", required=True, type=Path)
    parser.add_argument("--gateway", default="plantcare-photo-gateway-tests:local")
    parser.add_argument("--browser", default="plantcare-browser-tests:local")
    parser.add_argument("--capacity", action="store_true", help="Use a detailed generated sample and record local cgroup accounting")
    parser.add_argument("--budget", action="store_true", help="Test the preparation-only memory budget; requires --capacity")
    parser.add_argument("--snapshot-policy", action="store_true", help="Rehearse the snapshot-only policy in this disposable SQL server")
    parser.add_argument("--mixed-worker", help="Existing local worker image for concurrent reminder/photo/SQL-backup acceptance; requires --capacity --budget")
    parser.add_argument("--large-account", action="store_true", help="Test input boundaries and near-quota private export; requires --capacity --budget")
    parser.add_argument("--encrypted-recovery", action="store_true", help="Capture/check/restore only this generated near-quota fixture in a new private restic repository")
    parser.add_argument("--restored-app", action="store_true", help="Verify restored keys, restricted API, private photos and erasure replay; requires --encrypted-recovery")
    args = parser.parse_args()
    if args.budget and not args.capacity:
        parser.error("--budget requires --capacity")
    if args.mixed_worker and (not args.budget or args.snapshot_policy):
        parser.error("--mixed-worker requires --capacity --budget and cannot combine with --snapshot-policy")
    if args.large_account and (not args.budget or args.snapshot_policy or args.mixed_worker):
        parser.error("--large-account requires --capacity --budget and its own isolated run")
    if args.encrypted_recovery and not args.large_account:
        parser.error("--encrypted-recovery requires --large-account")
    if args.restored_app and not args.encrypted_recovery:
        parser.error("--restored-app requires --encrypted-recovery")
    restic = ROOT / ".operations-local/tools/restic.exe"
    if args.encrypted_recovery and (os.name != "nt" or not restic.is_file() or restic.is_symlink()):
        parser.error("Encrypted fixture recovery needs Windows and the existing local restic tool")
    manifest = verify(args.bundle)
    if image(manifest["imageId"]) != manifest["imageId"]:
        raise ValueError("Exact bundle decoder must already be loaded locally.")
    ids = {key: image(tag) for key, tag in {
        "api": args.api, "gateway": args.gateway, "browser": args.browser,
        "database": "mcr.microsoft.com/mssql/server:2022-latest", "proxy": "caddy:2-alpine",
        "mailpit": "axllent/mailpit:v1.27"}.items()}
    if args.mixed_worker:
        ids["worker"] = image(args.mixed_worker)
    project = "plantcare-photo-http-" + uuid.uuid4().hex
    base = ROOT / ".operations-local" / "photo-http"
    base.mkdir(parents=True, exist_ok=True)
    scratch = Path(tempfile.mkdtemp(prefix=project + "-", dir=base))
    if os.name != "nt":
        scratch.chmod(0o700)
    step, started, passed = "prepare", False, False
    compose = ["compose", "--project-name", project, "--file", str(scratch / "compose.json")]
    resources = None
    policy_results = None
    mixed_results = None
    large_results = None
    try:
        # Generated-only secrets in the private ignored directory, never argv or
        # output. No saved app environment, account, key or backup is read.
        passwords = {key: "Pc9!" + secrets.token_hex(24) for key in ("sa", "api", "worker")}
        connection = "Server=database,1433;Database=PlantCare;User Id={};Password={};Encrypt=True;TrustServerCertificate=True"
        production_caddy = (ROOT / "deploy/Caddyfile").read_text(encoding="utf-8")
        (scratch / "Caddyfile").write_text(production_caddy.replace(" {", " {\n    tls internal", 1), encoding="utf-8")
        common = {"pull_policy": "never", "logging": {"driver": "none"}, "restart": "no"}
        def service(key, **values):
            return {**common, "image": ids[key], **values}
        def volume(name, target, read_only=False):
            return {"type": "volume", "source": name, "target": target, "read_only": read_only}
        api_environment = {
            "ASPNETCORE_ENVIRONMENT": "Production", "ASPNETCORE_URLS": "http://+:8080",
            "ConnectionStrings__PlantCareDatabase": connection.format("plantcare_api", passwords["api"]),
            "Database__ApplyMigrationsOnStartup": "false", "Authentication__RequireConfirmedEmail": "true",
            "ReverseProxy__TrustForwardedHeaders": "true", "Application__ClientBaseUrl": "https://plantcare-photo.test:8443",
            "DataProtection__KeysPath": "/var/lib/plantcare/keys", "Logging__LogLevel__Default": "Warning",
            "Email__Host": "mailpit", "Email__Port": "1025", "Email__UseSsl": "false",
            "Email__Username": "", "Email__Password": "", "Email__FromAddress": "photos@example.test",
            "Email__ClientBaseUrl": "https://plantcare-photo.test:8443", "Email__FromName": "Photo acceptance",
            "PhotoProcessing__Enabled": "true", "PhotoJournal__UploadsEnabled": "true",
            "PhotoProcessing__SocketPath": "/run/plantcare-photo/gateway.sock", "PhotoProcessing__GatewayUid": "0",
            "GoogleCalendar__Enabled": "false", "MicrosoftCalendar__Enabled": "false",
            "WebPush__PublicKey": "", "WebPush__PrivateKey": "", "WebPush__Subject": ""}
        config = {"networks": {"default": {"internal": True}},
                  "volumes": {name: {} for name in ("sql", "keys", "ipc", "caddy-data", "caddy-config")}, "services": {
            "database": service("database", environment={"ACCEPT_EULA": "Y", "MSSQL_PID": "Express",
                "MSSQL_SA_PASSWORD": passwords["sa"]}, volumes=[volume("sql", "/var/opt/mssql")], mem_limit="2g",
                healthcheck={"test": ["CMD-SHELL", '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" -b -o /dev/null'],
                             "interval": "3s", "timeout": "5s", "retries": 60}),
            "migrate": service("api", profiles=["operations"], environment={
                "ASPNETCORE_ENVIRONMENT": "Production", "Logging__LogLevel__Default": "Warning",
                "ConnectionStrings__PlantCareDatabase": connection.format("sa", passwords["sa"]),
                "DatabaseRuntime__ApiPassword": passwords["api"], "DatabaseRuntime__WorkerPassword": passwords["worker"]},
                volumes=[{"type": "bind", "source": str(ROOT / "deploy"), "target": "/operator", "read_only": True}]),
            "gateway": service("gateway", user="0:1654", network_mode="none", read_only=True, cap_drop=["ALL"],
                security_opt=["no-new-privileges"], volumes=[volume("ipc", "/run/plantcare-photo"),
                    {"type": "bind", "source": "/var/run/docker.sock", "target": "/var/run/docker.sock"}],
                command=["--image-id", manifest["imageId"], "--allowed-uid", "1654", "--socket-gid", "1654"],
                healthcheck={"test": ["CMD", "python3", "-c", "import os; raise SystemExit(not os.path.exists('/run/plantcare-photo/gateway.sock'))"],
                             "interval": "2s", "timeout": "3s", "retries": 30}),
            "api": service("api", environment=api_environment,
                volumes=[volume("keys", "/var/lib/plantcare/keys"), volume("ipc", "/run/plantcare-photo", True)],
                healthcheck={"test": ["CMD", "curl", "--fail", "--silent", "http://localhost:8080/health/ready"],
                             "interval": "3s", "timeout": "5s", "retries": 40}),
            "proxy": service("proxy", environment={"PLANTCARE_DOMAIN": "plantcare-photo.test:8443"},
                networks={"default": {"aliases": ["plantcare-photo.test"]}},
                volumes=[{"type": "bind", "source": str(scratch / "Caddyfile"), "target": "/etc/caddy/Caddyfile", "read_only": True},
                         volume("caddy-data", "/data"), volume("caddy-config", "/config")]),
            "mailpit": service("mailpit"),
            "browser": service("browser", profiles=["checks"], entrypoint=["node", "/checks/photo-http.cjs"], shm_size="512m",
                volumes=[{"type": "bind", "source": str(ROOT / "tests/photo-http"), "target": "/checks", "read_only": True},
                         {"type": "bind", "source": str(scratch / "output"), "target": "/output"}])}}
        (scratch / "output").mkdir()
        if args.mixed_worker:
            config["volumes"]["worker-health"] = {}
            config["services"]["worker"] = service("worker", environment={
                "DOTNET_ENVIRONMENT": "Production", "Logging__LogLevel__Default": "Warning",
                "ConnectionStrings__PlantCareDatabase": connection.format("plantcare_worker", passwords["worker"]),
                "Notifications__PollIntervalSeconds": "5", "Notifications__BatchSize": "25",
                "ExternalCalendarSync__Enabled": "false", "GoogleCalendar__Enabled": "false",
                "MicrosoftCalendar__Enabled": "false", "WebPush__PublicKey": "", "WebPush__PrivateKey": "",
                "WebPush__Subject": "", "DataProtection__KeysPath": "/var/lib/plantcare/keys",
                "Operations__HeartbeatPath": "/var/lib/plantcare/worker-health/notifications.json"},
                volumes=[volume("keys", "/var/lib/plantcare/keys"), volume("worker-health", "/var/lib/plantcare/worker-health")])
            config["services"]["browser"]["environment"] = {"PHOTO_HTTP_MIXED": "1"}
        if args.capacity:
            (scratch / "fixtures").mkdir()
            (scratch / "fixtures/capacity.png").write_bytes(png_fixture(1024, 768, detailed=True))
            config["services"]["browser"].setdefault("environment", {})["PHOTO_HTTP_PNG"] = "/fixtures/capacity.png"
            config["services"]["browser"]["volumes"].append({
                "type": "bind", "source": str(scratch / "fixtures"), "target": "/fixtures", "read_only": True})
        if args.large_account:
            config["services"]["browser"]["environment"]["PHOTO_HTTP_LARGE"] = "1"
            for name, data in {
                "limit.png": padded_png((scratch / "fixtures/capacity.png").read_bytes(), 8 * 1024 * 1024),
                "over-limit.png": padded_png((scratch / "fixtures/capacity.png").read_bytes(), 8 * 1024 * 1024 + 1),
                "20mp.png": png_fixture(5000, 4000), "21mp.png": png_fixture(6000, 3500)}.items():
                (scratch / "fixtures" / name).write_bytes(data)
        if args.restored_app:
            config["services"]["browser"]["environment"]["PHOTO_HTTP_RESTORED_APP"] = "1"
        if args.budget:
            budget = json.loads((ROOT / "deploy/compose.photo-budget.json.example").read_text(encoding="utf-8"))["services"]
            for service in ("api", "database", "proxy") + (("worker",) if args.mixed_worker else ()):
                for key in ("mem_limit", "memswap_limit"):
                    config["services"][service][key] = budget[service][key]
                if service == "database":
                    config["services"][service]["environment"].update(budget[service]["environment"])
            config["services"]["gateway"].update(mem_limit="128m", memswap_limit="128m")
        if args.snapshot_policy:
            config["services"]["browser"].setdefault("environment", {})["PHOTO_HTTP_SNAPSHOT"] = "1"
        (scratch / "compose.json").write_text(json.dumps(config), encoding="utf-8")
        if os.name != "nt":
            (scratch / "compose.json").chmod(0o600)
        step = "start-isolated-prerequisites"
        started = True
        docker(*compose, "up", "-d", "--no-build", "--wait", "--wait-timeout", "240", "database", "gateway", "mailpit")
        if docker("network", "inspect", project + "_default", "--format", "{{.Internal}}").stdout.strip() != b"true":
            raise ValueError("External routing must remain blocked.")
        step = "migrate-restrict-and-seed"
        for operation in (["--migrate-database"], ["--provision-runtime-users"], ["--import-catalog", "/operator/catalog.example.json"]):
            docker(*compose, "run", "--rm", "--no-deps", "migrate", *operation)
        def sql(query, check=True):
            return docker("exec", "-i", project + "-database-1", "sh", "-c",
                          'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -h -1 -W -w 4096 -d PlantCare -i /dev/stdin',
                          data=("SET NOCOUNT ON;\n" + query).encode(), check=check)
        if args.snapshot_policy:
            step = "snapshot-policy-guards"
            policy = prepare(sql, ROOT)
            print("PASS: missing approval/baseline refused; acknowledged snapshot-only policy is idempotent in disposable SQL.", flush=True)
        docker(*compose, "up", "-d", "--no-build", "--wait", "--wait-timeout", "180", "api", "proxy")
        api = project + "-api-1"
        mounts = json.loads(docker("inspect", "--format", "{{json .Mounts}}", api).stdout)
        if (not any(m["Destination"] == "/run/plantcare-photo" and not m["RW"] for m in mounts)
                or any("docker.sock" in m["Destination"] for m in mounts)):
            raise ValueError("API mount boundary changed.")
        for flag in ("-u", "-g"):
            if docker("exec", api, "id", flag).stdout.strip() != b"1654":
                raise ValueError("Unexpected API process identity.")
        docker("exec", api, "dotnet", "PlantCare.Api.dll", "--check-runtime-access", "api")
        def memory(container, name):
            value = docker("exec", container, "cat", "/sys/fs/cgroup/memory." + name).stdout.strip()
            if not re.fullmatch(rb"[1-9][0-9]*", value):
                raise ValueError("Kernel memory accounting unavailable.")
            return int(value)
        if args.capacity:
            resources = {"beforeBytes": {service: memory(project + "-" + service + "-1", "current")
                                         for service in ("api", "database", "gateway")}}
            if args.budget:
                resources["limitBytes"] = {service: int(docker("exec", project + "-" + service + "-1", "cat", "/sys/fs/cgroup/memory.max").stdout.strip())
                                           for service in ("api", "database", "gateway", "proxy")}
        print("PASS: disposable internal HTTPS stack, restricted API and read-only IPC mount.", flush=True)
        step = "real-browser-journal"
        if args.snapshot_policy or args.mixed_worker or args.large_account:
            process = subprocess.Popen(["docker", *compose, "run", "--rm", "--no-deps", "browser"], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            try:
                if args.large_account:
                    step = "large-input-account-export"
                    large_results = {}
                    def recovery(sample, progress):
                        def recover_app(payload, app_progress):
                            return restored_app(sql, docker, compose, project, scratch, process, payload, sample,
                                                config, api_environment, passwords, app_progress)
                        return encrypted_recovery(sql, docker, project, scratch, restic, sample, ids, manifest["imageId"], progress,
                                                  recover_app if args.restored_app else None)
                    large_results.update(large_checkpoint(sql, docker, project, scratch, process, large_results,
                                                         recovery if args.encrypted_recovery else None))
                    print("PASS: input byte/pixel boundaries, near-quota private export and storage rejection/reuse.", flush=True)
                elif args.mixed_worker:
                    step = "mixed-worker-photo-backups"
                    mixed_results = {}
                    mixed_results.update(mixed_checkpoint(sql, docker, compose, project, scratch, process, mixed_results))
                    print("PASS: concurrent photo operations, restricted reminder worker and checksum SQL backups; no duplicate reminders or OOM events.", flush=True)
                else:
                    step = "snapshot-browser-checkpoint"
                    ready_path = scratch / "output/snapshot-ready.json"
                    deadline = time.monotonic() + 180
                    while not ready_path.exists():
                        if process.poll() is not None or time.monotonic() > deadline:
                            raise RuntimeError("Browser did not reach the synthetic photo snapshot checkpoint.")
                        time.sleep(.1)
                    step = "snapshot-photo-restore"
                    try:
                        policy_results = checkpoint(sql, json.loads(ready_path.read_text(encoding="utf-8")))
                    finally:
                        (scratch / "output/snapshot-continue.json").write_text('{"continue":true}', encoding="utf-8")
                out, err = process.communicate(timeout=180)
                result = subprocess.CompletedProcess(process.args, process.returncode, out, err)
            finally:
                if process.poll() is None:
                    process.kill()
                    process.communicate(timeout=20)
        else:
            result = docker(*compose, "run", "--rm", "--no-deps", "browser", check=False)
        # Print only known success markers. Never forward browser failure details,
        # generated credentials, confirmation links, cookies or request bodies.
        for line in result.stdout.decode(errors="replace").splitlines():
            if line.startswith("PASS: ") or re.fullmatch(r"Photo HTTP browser failure at [a-z-]+; sensitive details withheld\.", line):
                print(line, flush=True)
        if result.returncode:
            raise RuntimeError("Real browser photo journal acceptance failed.")
        if args.capacity:
            resources["afterBytes"] = {service: memory(project + "-" + service + "-1", "current")
                                       for service in ("api", "database", "gateway")}
            resources["peakBytes"] = {service: memory(project + "-" + service + "-1", "peak")
                                      for service in ("api", "database", "gateway")}
            resources["sample"] = json.loads((scratch / "output/photo-sample.json").read_text(encoding="utf-8"))
        step = "verify-normalized-export"
        with zipfile.ZipFile(scratch / "output/photos.zip") as archive:
            export = json.loads(archive.read("manifest.json"))
            if len(export["images"]) != 1:
                raise ValueError("Unexpected private photo export contents.")
            entry = export["images"][0]
            if not re.fullmatch(r"photos/[a-f0-9]{32}\.jpg", entry["file"]):
                raise ValueError("Unexpected export filename.")
            if set(archive.namelist()) != {"manifest.json", entry["file"]}:
                raise ValueError("Unexpected export entries.")
            data = archive.read(entry["file"])
            if (not data.startswith(b"\xff\xd8") or not data.endswith(b"\xff\xd9")
                    or hashlib.sha256(data).hexdigest().lower() != entry["sha256"].lower()):
                raise ValueError("Normalized export image/hash mismatch.")
        print("PASS: private ZIP contains only the normalized JPEG and matching manifest hash.", flush=True)
        step = "verify-media-erasure"
        query = "SET NOCOUNT ON; USE PlantCare; IF EXISTS(SELECT 1 FROM PlantPhotoEntries) OR EXISTS(SELECT 1 FROM PlantPhotoMedia) THROW 51000,'Photo rows remain',1;"
        docker("exec", project + "-database-1", "sh", "-c",
               '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "$1"', "sh", query)
        if args.snapshot_policy:
            step = "existing-log-chain-refusal"
            reject_log_chain_retirement(sql, policy)
            print("PASS: logical log reuse and photo-bearing snapshot restore; existing log-chain retirement refused.", flush=True)
        remaining = docker("ps", "-aq", "--filter", "ancestor=" + manifest["imageId"]).stdout.strip()
        if remaining:
            raise ValueError("A decoder job remains; inspect before further processing.")
        passed = True
        if args.capacity:
            (ROOT / ".operations-local/photo-http-capacity.json").write_text(json.dumps({
                "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "images": ids,
                "decoderImage": manifest["imageId"], "budgetEnabled": args.budget, "resources": resources,
                "limitations": "One generated 1024x768 sample in the isolated sequential HTTP lifecycle on local Docker. Peak includes startup and all account/export/erasure work. Not a sustained-load, maximum-body, camera or VPS capacity claim. SQL backup/physical file overhead is not measured."}, indent=2) + "\n", encoding="utf-8")
            print("PASS: detailed HTTP sample and local API/SQL/gateway kernel memory accounting recorded.", flush=True)
        print("PASS: real SQL photo metadata/media erased; no decoder job remains.", flush=True)
    finally:
        cleanup_ok = True
        if started:
            if not re.fullmatch(r"plantcare-photo-http-[a-f0-9]{32}", project):
                raise ValueError("Unexpected project; cleanup refused.")
            cleanup_ok = docker(*compose, "down", "--volumes", "--remove-orphans", check=False).returncode == 0
        (ROOT / ".operations-local/photo-http-last.json").write_text(json.dumps({
            "passed": passed and cleanup_ok, "step": step, "cleanup": cleanup_ok,
            "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "images": ids,
            "apiImage": ids["api"], "decoderImage": manifest["imageId"], "bundleCandidate": manifest["candidate"]}), encoding="utf-8")
        if args.snapshot_policy:
            (ROOT / ".operations-local/photo-snapshot-policy.json").write_text(json.dumps({
                "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "passed": passed and cleanup_ok,
                "cleanup": cleanup_ok, "step": step, "results": policy_results,
                "images": ids, "decoderImage": manifest["imageId"],
                "limitations": "Disposable local SQL only; policy not selected/applied on normal local/staging databases. SQL media restore only, no restored app start or point-in-time recovery. No encrypted repository capture/restore."}, indent=2) + "\n", encoding="utf-8")
        if args.mixed_worker:
            (ROOT / ".operations-local/photo-mixed-workload.json").write_text(json.dumps({
                "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "passed": passed and cleanup_ok,
                "cleanup": cleanup_ok, "step": step, "results": mixed_results, "images": ids,
                "decoderImage": manifest["imageId"],
                "limitations": "Generated local Docker sample, 100 reminders, four extra photo uploads/exports and overlapping SQL COPY_ONLY backups. No external push/mail, encrypted repository work, maximum quota export, sustained load, systemd or VPS acceptance."}, indent=2) + "\n", encoding="utf-8")
        if args.large_account:
            (ROOT / ".operations-local/photo-large-account.json").write_text(json.dumps({
                "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "passed": passed and cleanup_ok,
                "cleanup": cleanup_ok, "step": step, "results": large_results, "images": ids,
                "decoderImage": manifest["imageId"],
                "limitations": "Generated local fixtures and cloned genuine normalized media in disposable SQL. Near-quota sample, not every image mix, real camera, multi-user/service saturation, encrypted backup or VPS acceptance."}, indent=2) + "\n", encoding="utf-8")
        if args.encrypted_recovery:
            (ROOT / ".operations-local/photo-encrypted-recovery.json").write_text(json.dumps({
                "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "passed": passed and cleanup_ok,
                "cleanup": cleanup_ok, "step": step, "results": (large_results or {}).get("recovery"),
                "images": ids, "decoderImage": manifest["imageId"],
                "limitations": "New disposable local repository and random test key only; repeated generated media can deduplicate heavily. "
                    + ("Restored API/key/erasure acceptance is recorded separately. " if args.restored_app else "SQL/key payload restore and CHECKDB, without restored app startup. ")
                    + "No external-drive/network transfer, independent production recovery or retention sizing."}, indent=2) + "\n", encoding="utf-8")
        if args.restored_app:
            recovery_result = (large_results or {}).get("recovery", {})
            (ROOT / ".operations-local/photo-restored-app.json").write_text(json.dumps({
                "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "passed": passed and cleanup_ok,
                "cleanup": cleanup_ok, "step": step, "results": recovery_result.get("app"), "images": ids,
                "limitations": "Generated local accounts, same disposable SQL server and existing server logins. Real restored keys/API and erasure replay, not a fresh-master/replacement-host test or external notification delivery."}, indent=2) + "\n", encoding="utf-8")
        if cleanup_ok and scratch.resolve().parent == base.resolve() and scratch.name.startswith(project + "-"):
            shutil.rmtree(scratch)
        if not cleanup_ok:
            raise RuntimeError("Disposable photo HTTP cleanup failed; private configuration retained.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, RuntimeError, OSError, subprocess.SubprocessError):
        raise SystemExit("Photo HTTP acceptance failed; inspect the non-secret step in .operations-local/photo-http-last.json.")
