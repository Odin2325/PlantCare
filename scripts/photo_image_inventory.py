"""Read exact decoder image files/notices without starting a container or installing it."""
import argparse
import hashlib
import io
import json
import re
import subprocess
import tarfile
import uuid
from pathlib import Path, PurePosixPath
from photo_bundle import ROOT, verify


def docker(*args):
    result = subprocess.run(["docker", *args], capture_output=True, timeout=60)
    if result.returncode:
        raise ValueError("Exact image inventory operation failed; output withheld.")
    return result.stdout


def notice_path(name):
    path = PurePosixPath(name)
    if path.is_absolute() or any(part in ("..", ".", "") for part in name.split("/")):
        raise ValueError("Unexpected archive path.")
    return (name in ("usr/share/dotnet/LICENSE.txt", "usr/share/dotnet/ThirdPartyNotices.txt")
            or name.startswith("app/ThirdPartyNotices/")
            or name.startswith("usr/share/common-licenses/")
            or (name.startswith("usr/share/doc/") and re.match(r"(?i)^(copyright|licen[sc]e|notice)(\..*)?$", path.name)))


def verify_inventory(directory):
    directory = Path(directory)
    report_path, archive_path = directory / "inventory.json", directory / "image-notices.tar"
    if (directory.is_symlink() or directory.is_junction() or any(path.is_symlink() or path.is_junction() for path in (report_path, archive_path))
            or report_path.stat().st_size > 2 * 1024 * 1024 or archive_path.stat().st_size > 32 * 1024 * 1024):
        raise ValueError("Inventory paths or size are unsafe.")
    report = json.loads(report_path.read_text(encoding="utf-8"))
    if (report.get("schemaVersion") != 1 or report.get("licenseReview") != "incomplete"
            or not re.fullmatch(r"sha256:[a-f0-9]{64}", report.get("imageId", ""))
            or not re.fullmatch(r"[a-f0-9]{40}", report.get("bundleCandidate", ""))):
        raise ValueError("Inventory identity/status changed.")
    with archive_path.open("rb") as stream: digest = hashlib.file_digest(stream, "sha256").hexdigest()
    if digest != report.get("noticeArchiveSha256"): raise ValueError("Notice archive checksum mismatch.")
    expected = {item["path"]: item for item in report["noticeFiles"]}
    if not expected or len(expected) != len(report["noticeFiles"]): raise ValueError("Ambiguous notice index.")
    seen = set(); total = 0
    with tarfile.open(archive_path, "r:") as archive:
        for member in archive:
            if not member.isfile() or member.name in seen or not notice_path(member.name) or member.name not in expected:
                raise ValueError("Unexpected notice archive entry.")
            total += member.size
            if member.size > 8 * 1024 * 1024 or total > 24 * 1024 * 1024:
                raise ValueError("Notice archive exceeds review bounds.")
            record = expected[member.name]
            if member.size != record["bytes"] or hashlib.file_digest(archive.extractfile(member), "sha256").hexdigest() != record["sha256"]:
                raise ValueError("Notice file checksum mismatch.")
            seen.add(member.name)
    if seen != expected.keys(): raise ValueError("Notice archive is incomplete.")
    return report


def inventory(bundle):
    manifest = verify(bundle)
    info = json.loads(docker("image", "inspect", manifest["imageId"]))[0]
    if info["Id"] != manifest["imageId"] or info["Os"] != "linux" or info["Architecture"] != "amd64":
        raise ValueError("Exact accepted Linux amd64 image required.")
    base = ROOT / ".operations-local/license-review/photo-images"
    base.mkdir(parents=True, exist_ok=True)
    destination = base / manifest["candidate"]
    if destination.exists(): raise ValueError("Preserve the existing image inventory.")
    name = "plantcare-photo-notices-" + uuid.uuid4().hex
    notices, links, assets, framework_versions = {}, [], [], set()
    status = os_release = None
    created = False
    try:
        docker("create", "--pull", "never", "--name", name, "--network", "none", "--read-only", "--entrypoint", "/bin/true", manifest["imageId"])
        created = True
        process = subprocess.Popen(["docker", "export", name], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        try:
            with tarfile.open(fileobj=process.stdout, mode="r|") as archive:
                for member in archive:
                    path = member.name.removeprefix("./")
                    is_notice = notice_path(path)
                    if path.startswith("usr/share/dotnet/shared/Microsoft.NETCore.App/"):
                        framework_versions.add(path.split("/")[5])
                    if is_notice and (member.issym() or member.islnk()):
                        links.append({"path": path, "target": member.linkname, "kind": "symlink" if member.issym() else "hardlink"})
                    if not member.isfile(): continue
                    native = path.startswith("app/") and ("/native/" in path or "Magick.Native" in PurePosixPath(path).name)
                    if native:
                        stream = archive.extractfile(member); digest = hashlib.file_digest(stream, "sha256").hexdigest()
                        assets.append({"path": path, "bytes": member.size, "sha256": digest})
                    elif is_notice or path in ("var/lib/dpkg/status", "etc/os-release", "usr/lib/os-release"):
                        if member.size > 8 * 1024 * 1024:
                            raise ValueError("Oversized metadata requires separate inspection.")
                        data = archive.extractfile(member).read()
                        if is_notice:
                            if path in notices: raise ValueError("Duplicate notice path.")
                            notices[path] = data
                            if sum(map(len, notices.values())) > 24 * 1024 * 1024:
                                raise ValueError("Notice inventory exceeds its review bound.")
                        elif path == "var/lib/dpkg/status": status = data.decode("utf-8")
                        else: os_release = data.decode("utf-8")
            _, error = process.communicate(timeout=60)
            if process.returncode: raise ValueError("Stopped-container export failed.")
        finally:
            if process.poll() is None:
                process.kill(); process.communicate(timeout=15)
        if not status or not os_release or not assets or not framework_versions:
            raise ValueError(f"Image inventory incomplete: frameworks={len(framework_versions)}, packages={bool(status)}, OS={bool(os_release)}, native={len(assets)}.")
        for notice in ("Magick.NET-15.0.0.txt", "Magick.NET.Core-15.0.0.txt"):
            path = "app/ThirdPartyNotices/" + notice
            if notices.get(path) != (bundle / "notices" / notice).read_bytes():
                raise ValueError("Image native notices differ from the accepted bundle.")
        if not all(path in notices for path in ("usr/share/dotnet/LICENSE.txt", "usr/share/dotnet/ThirdPartyNotices.txt")):
            raise ValueError("Framework notice evidence missing.")
        packages = []
        for paragraph in status.split("\n\n"):
            fields = dict(line.split(": ", 1) for line in paragraph.splitlines() if ": " in line and not line.startswith(" "))
            if fields.get("Status") == "install ok installed":
                packages.append({"package": fields["Package"], "version": fields["Version"], "architecture": fields.get("Architecture")})
        headings = re.findall(r"^\[ (.+?) \](?: copyright:)?\r?$", notices["app/ThirdPartyNotices/Magick.NET-15.0.0.txt"].decode("utf-8-sig"), re.M)
        destination.mkdir()
        tar_path = destination / "image-notices.tar"
        with tarfile.open(tar_path, "w") as archive:
            for path, data in sorted(notices.items()):
                member = tarfile.TarInfo(path); member.size = len(data); member.mode = 0o644
                archive.addfile(member, io.BytesIO(data))
        report = {"schemaVersion": 1, "bundleCandidate": manifest["candidate"], "imageId": manifest["imageId"],
                  "platform": "linux/amd64", "containerStarted": False,
                  "osRelease": os_release, "frameworkVersions": sorted(framework_versions),
                  "installedOsPackages": sorted(packages, key=lambda row: row["package"]),
                  "shippedNativeAssets": sorted(assets, key=lambda row: row["path"]),
                  "noticeFiles": [{"path": path, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()} for path, data in sorted(notices.items())],
                  "noticeLinks": sorted(links, key=lambda row: row["path"]), "nativeNoticeSectionHeadings": headings,
                  "noticeArchiveSha256": hashlib.sha256(tar_path.read_bytes()).hexdigest(),
                  "licenseReview": "incomplete",
                  "scope": "Exact stopped-container filesystem: native RID assets, OS package records, framework and packaged vendor notice bytes. Headings are not proof of linked components or license obligations. No code executed, package installed, host modified, signature verified or legal clearance granted."}
        (destination / "inventory.json").write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        verify_inventory(destination)
        print("PASS: exact image native/OS/framework notice inventory captured; container never started; terms review remains open.")
    finally:
        if created: docker("rm", name)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    actions = parser.add_mutually_exclusive_group(required=True)
    actions.add_argument("--bundle", type=lambda value: ROOT / value)
    actions.add_argument("--verify", type=lambda value: ROOT / value)
    args = parser.parse_args()
    try:
        if args.bundle: inventory(args.bundle)
        else:
            verify_inventory(args.verify)
            print("PASS: image notice archive/index hashes and members verified; no Docker or host action.")
    except (ValueError, OSError, subprocess.SubprocessError, tarfile.TarError):
        raise SystemExit("Photo image inventory failed; raw image/tool output withheld.")
