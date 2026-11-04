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
import uuid
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from photo_bundle import ROOT, verify


def docker(*args, check=True, timeout=300):
    result = subprocess.run(["docker", *args], capture_output=True, timeout=timeout)
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
    args = parser.parse_args()
    manifest = verify(args.bundle)
    if image(manifest["imageId"]) != manifest["imageId"]:
        raise ValueError("Exact bundle decoder must already be loaded locally.")
    ids = {key: image(tag) for key, tag in {
        "api": args.api, "gateway": args.gateway, "browser": args.browser,
        "database": "mcr.microsoft.com/mssql/server:2022-latest", "proxy": "caddy:2-alpine",
        "mailpit": "axllent/mailpit:v1.27"}.items()}
    project = "plantcare-photo-http-" + uuid.uuid4().hex
    base = ROOT / ".operations-local" / "photo-http"
    base.mkdir(parents=True, exist_ok=True)
    scratch = Path(tempfile.mkdtemp(prefix=project + "-", dir=base))
    if os.name != "nt":
        scratch.chmod(0o700)
    step, started, passed = "prepare", False, False
    compose = ["compose", "--project-name", project, "--file", str(scratch / "compose.json")]
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
        print("PASS: disposable internal HTTPS stack, restricted API and read-only IPC mount.", flush=True)
        step = "real-browser-journal"
        result = docker(*compose, "run", "--rm", "--no-deps", "browser", check=False)
        # Print only known success markers. Never forward browser failure details,
        # generated credentials, confirmation links, cookies or request bodies.
        for line in result.stdout.decode(errors="replace").splitlines():
            if line.startswith("PASS: ") or re.fullmatch(r"Photo HTTP browser failure at [a-z-]+; sensitive details withheld\.", line):
                print(line, flush=True)
        if result.returncode:
            raise RuntimeError("Real browser photo journal acceptance failed.")
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
        remaining = docker("ps", "-aq", "--filter", "ancestor=" + manifest["imageId"]).stdout.strip()
        if remaining:
            raise ValueError("A decoder job remains; inspect before further processing.")
        passed = True
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
        if cleanup_ok and scratch.resolve().parent == base.resolve() and scratch.name.startswith(project + "-"):
            shutil.rmtree(scratch)
        if not cleanup_ok:
            raise RuntimeError("Disposable photo HTTP cleanup failed; private configuration retained.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, RuntimeError, OSError, subprocess.SubprocessError):
        raise SystemExit("Photo HTTP acceptance failed; inspect the non-secret step in .operations-local/photo-http-last.json.")
