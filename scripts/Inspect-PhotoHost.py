"""Read-only Linux host preparation facts; never install/start/change a service."""
import argparse
import json
import os
import re
import stat
import subprocess
from datetime import datetime, timezone


def inspect(decoder, api):
    def run(*args):
        try:
            result = subprocess.run(args, capture_output=True, text=True, timeout=20)
            return result.stdout.strip() if result.returncode == 0 else None
        except (OSError, subprocess.SubprocessError): return None

    def docker(*args):
        return run(*(("sudo", "-n") if os.geteuid() else ()), "docker", *args)

    systemd = run("systemctl", "--version")
    report = {"checkedAtUtc": datetime.now(timezone.utc).isoformat(), "readOnly": True,
              "architecture": run("uname", "-m"), "python": run("python3", "--version"),
              "systemd": systemd.splitlines()[0] if systemd else None,
              "dockerServer": docker("version", "--format", "{{.Server.Version}}"),
              "decoderImage": decoder, "decoderLoaded": docker("image", "inspect", decoder, "--format", "{{.Id}}") == decoder}
    for label, path in (("gatewaySource", "/opt/plantcare/scripts/photo_gateway.py"),
                        ("gatewayUnit", "/etc/systemd/system/plantcare-photo-gateway.service"),
                        ("gatewayRuntime", "/run/plantcare-photo"), ("gatewaySocket", "/run/plantcare-photo/gateway.sock")):
        try:
            info = os.lstat(path)
            report[label] = {"uid": info.st_uid, "gid": info.st_gid, "mode": oct(stat.S_IMODE(info.st_mode)),
                             "symlink": stat.S_ISLNK(info.st_mode)}
        except FileNotFoundError: report[label] = {"present": False}
        except OSError: report[label] = {"inspection": "unavailable"}
    data = docker("info", "--format", "{{json .SecurityOptions}}")
    security = json.loads(data) if data else []
    report["rootlessDocker"] = any("rootless" in item for item in security)
    report["dockerUserNamespaceOption"] = any("userns" in item for item in security)
    data = docker("inspect", api)
    if data:
        container = json.loads(data)[0]
        mounts = container["Mounts"]
        sockets = [item for item in mounts if item["Destination"] == "/run/plantcare-photo"]
        report["api"] = {"image": container["Image"], "configuredUser": container["Config"]["User"],
                         "uid": docker("exec", api, "id", "-u"), "gid": docker("exec", api, "id", "-g"),
                         "userNamespaceMode": container["HostConfig"]["UsernsMode"],
                         "memoryLimitBytes": container["HostConfig"]["Memory"],
                         "dockerSocketMounted": any("docker.sock" in item["Destination"] for item in mounts),
                         "photoSocketMounted": bool(sockets),
                         "photoSocketMountReadOnly": bool(sockets) and all(not item["RW"] for item in sockets)}
    else: report["api"] = {"inspection": "unavailable"}
    report["activationAccepted"] = False
    report["scope"] = "Versions, current metadata and mounts only. Does not certify installed hashes, live socket peer mapping, systemd/reboot behavior, resource headroom, notices or recovery policy."
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--decoder-image", required=True)
    parser.add_argument("--api-container", default="plantcare-staging-api-1")
    args = parser.parse_args()
    if not re.fullmatch(r"sha256:[a-f0-9]{64}", args.decoder_image) or not re.fullmatch(r"[a-zA-Z0-9][a-zA-Z0-9_.-]{0,127}", args.api_container):
        parser.error("Use a pinned decoder image ID and a literal container name")
    print(json.dumps(inspect(args.decoder_image, args.api_container), sort_keys=True))
