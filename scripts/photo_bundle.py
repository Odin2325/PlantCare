"""Export committed decoder/host-gateway preparation artifacts; never install them."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import tarfile
import tempfile
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NOTICES = ("Magick.NET-15.0.0.txt", "Magick.NET.Core-15.0.0.txt")
FILES = {"decoder-image.tar", "decoder.deps.json", "photo_gateway.py",
         "plantcare-photo-gateway.service.example", "compose.photo-processing.yaml.example",
         "policy.xml", *("notices/" + name for name in NOTICES)}


def command(*args, cwd=ROOT):
    result = subprocess.run(args, cwd=cwd, capture_output=True, timeout=900)
    if result.returncode:
        raise ValueError("Photo bundle command failed; inspect local build prerequisites.")
    return result.stdout


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def verify(path):
    path = Path(path)
    if path.is_symlink() or path.is_junction() or not path.is_dir():
        raise ValueError("Bundle must be a regular directory.")
    actual = set()
    for item in path.rglob("*"):
        if item.is_symlink() or item.is_junction() or (not item.is_dir() and not item.is_file()):
            raise ValueError("Bundle links and special files are forbidden.")
        if item.is_file():
            actual.add(item.relative_to(path).as_posix())
    if actual != FILES | {"manifest.json"}:
        raise ValueError("Unexpected or missing bundle files.")
    manifest = json.loads((path / "manifest.json").read_text(encoding="utf-8"))
    if not isinstance(manifest, dict) or not isinstance(manifest.get("files"), dict):
        raise ValueError("Invalid manifest structure.")
    if any(not isinstance(manifest.get(key), str) for key in ("candidate", "imageId")):
        raise ValueError("Invalid manifest identity.")
    if (manifest.get("format") != 1 or not re.fullmatch(r"[a-f0-9]{40}", manifest.get("candidate", ""))
            or not re.fullmatch(r"sha256:[a-f0-9]{64}", manifest.get("imageId", ""))
            or manifest.get("platform") != "linux/amd64" or manifest.get("activation") != "disabled"
            or manifest.get("licenseReview") != "incomplete" or manifest.get("files", {}).keys() != FILES):
        raise ValueError("Invalid preparation-only manifest.")
    for name, expected in manifest["files"].items():
        if not isinstance(expected, str) or not re.fullmatch(r"[a-f0-9]{64}", expected) or digest(path / name) != expected:
            raise ValueError("Bundle checksum mismatch.")
    protocol = manifest.get("gatewayStateProtocol", 0)
    if protocol not in (0, 1):
        raise ValueError("Unsupported gateway restart-state protocol.")
    # A manifest is not a signature. Compare its digest through a trusted channel
    # at transfer, then verify loaded image ID and host permissions separately.
    unit = (path / "plantcare-photo-gateway.service.example").read_text(encoding="utf-8")
    if ("--image-id " + manifest["imageId"] not in unit or "User=root" not in unit
            or "--allowed-uid 1654 --socket-gid 1654" not in unit):
        raise ValueError("Gateway identity or image binding changed.")
    if protocol == 1 and "RuntimeDirectoryPreserve=yes" not in unit:
        raise ValueError("Gateway restart state must survive service stops.")
    compose = (path / "compose.photo-processing.yaml.example").read_text(encoding="utf-8")
    for required in ('PhotoProcessing__Enabled: "false"', 'PhotoJournal__UploadsEnabled: "false"',
                     "read_only: true", "create_host_path: false"):
        if required not in compose:
            raise ValueError("Preparation override must remain disabled/read-only.")
    for name in NOTICES:
        if (path / "notices" / name).stat().st_size < 100:
            raise ValueError("Decoder attribution is missing.")
    return manifest


def export(candidate):
    if not re.fullmatch(r"HEAD|[a-fA-F0-9]{40}", candidate):
        raise ValueError("Use HEAD or a complete commit SHA.")
    sha = command("git", "rev-parse", "--verify", candidate + "^{commit}").decode().strip()
    if not re.fullmatch(r"[a-f0-9]{40}", sha):
        raise ValueError("Candidate is not a commit.")
    if command("docker", "info", "--format", "{{.OSType}}").strip() != b"linux":
        raise ValueError("Linux Docker is required.")
    base = ROOT / ".operations-local" / "photo-processing-bundles"
    base.mkdir(parents=True, exist_ok=True)
    destination = base / sha
    if destination.exists() or destination.is_symlink():
        raise ValueError("Preserve the existing bundle; choose another committed candidate.")
    with tempfile.TemporaryDirectory(prefix="scratch-", dir=base) as temporary:
        scratch = Path(temporary)
        source, payload = scratch / "source", scratch / "bundle"
        source.mkdir(); payload.mkdir()
        archive = scratch / "source.tar"
        paths = ["PlantCare.PhotoProcessing", "deploy/Dockerfile.photo-processing",
                 "scripts/photo_gateway.py", "deploy/plantcare-photo-gateway.service.example",
                 "deploy/compose.photo-processing.yaml.example"]
        command("git", "archive", "--format=tar", "--output=" + str(archive), sha, *paths)
        with tarfile.open(archive) as stream:
            stream.extractall(source, filter="data")
        tag = "plantcare-photo-processing:" + sha
        print("Building committed Linux amd64 decoder.", flush=True)
        command("docker", "build", "--platform", "linux/amd64", "--target", "runtime", "-f",
                str(source / "deploy/Dockerfile.photo-processing"), "-t", tag, str(source))
        info = json.loads(command("docker", "image", "inspect", tag))[0]
        if (info["Os"] != "linux" or info["Architecture"] != "amd64" or info["Config"]["User"] != "app"
                or info["Config"]["Entrypoint"] != ["dotnet", "PlantCare.PhotoProcessing.dll", "--normalize"]):
            raise ValueError("Unexpected decoder image contract.")
        image = info["Id"]
        probe = "plantcare-photo-bundle-" + uuid.uuid4().hex
        command("docker", "create", "--name", probe, "--network", "none", image)
        try:
            command("docker", "cp", probe + ":/app/ThirdPartyNotices/.", str(payload / "notices"))
            command("docker", "cp", probe + ":/app/policy/policy.xml", str(payload / "policy.xml"))
            command("docker", "cp", probe + ":/app/PlantCare.PhotoProcessing.deps.json", str(payload / "decoder.deps.json"))
        finally:
            command("docker", "rm", probe)
        for name in NOTICES:
            if digest(payload / "notices" / name) != digest(source / "PlantCare.PhotoProcessing/ThirdPartyNotices" / name):
                raise ValueError("Image notices differ from committed source.")
        if digest(payload / "policy.xml") != digest(source / "PlantCare.PhotoProcessing/policy/policy.xml"):
            raise ValueError("Image policy differs from committed source.")
        shutil.copyfile(source / "scripts/photo_gateway.py", payload / "photo_gateway.py")
        shutil.copyfile(source / "deploy/compose.photo-processing.yaml.example", payload / "compose.photo-processing.yaml.example")
        unit = (source / "deploy/plantcare-photo-gateway.service.example").read_text(encoding="utf-8")
        if unit.count("sha256:REPLACE_WITH_ACCEPTED_LOCAL_IMAGE_ID") != 1:
            raise ValueError("Gateway template placeholder changed.")
        (payload / "plantcare-photo-gateway.service.example").write_text(
            unit.replace("sha256:REPLACE_WITH_ACCEPTED_LOCAL_IMAGE_ID", image), encoding="utf-8", newline="\n")
        print("Exporting separate decoder archive and verified image notices.", flush=True)
        command("docker", "image", "save", "--output", str(payload / "decoder-image.tar"), tag)
        manifest = {"format": 1, "candidate": sha, "imageId": image, "platform": "linux/amd64",
                    "gatewayStateProtocol": 1,
                    "activation": "disabled", "licenseReview": "incomplete",
                    "files": {name: digest(payload / name) for name in sorted(FILES)}}
        (payload / "manifest.json").write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        verify(payload)
        payload.rename(destination)
    print("PASS: committed photo preparation bundle verified; nothing installed or enabled.")
    print("Manifest SHA256: " + digest(destination / "manifest.json"))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    actions = parser.add_subparsers(dest="action", required=True)
    actions.add_parser("export").add_argument("--candidate", default="HEAD")
    actions.add_parser("verify").add_argument("--bundle", type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.action == "export":
            export(args.candidate)
        else:
            verify(args.bundle)
            print("PASS: photo preparation bundle files and hashes verified; no load or installation.")
    except (ValueError, OSError, subprocess.SubprocessError, tarfile.TarError):
        parser.exit(1, "Photo bundle rejected; check committed inputs, file hashes and local Docker prerequisites.\n")
