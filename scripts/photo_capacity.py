"""Local generated-image capacity samples, not VPS certification or a load test."""
import argparse
import hashlib
import json
import random
import re
import struct
import subprocess
import time
import uuid
import zlib
from datetime import datetime, timezone
if __package__:
    from .photo_bundle import ROOT, verify
else:
    from photo_bundle import ROOT, verify


def png_fixture(width, height, detailed=False):
    if not 1 <= width <= 10000 or not 1 <= height <= 10000:
        raise ValueError("Unsupported generated fixture dimensions.")
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xffffffff)
    compressor = zlib.compressobj(6)
    compressed = []
    rng = random.Random(20261104)
    rows = {}
    for y in range(height):
        if detailed:
            row = rng.randbytes(width * 3)
        else:
            key = y % 64
            if key not in rows:
                tile = bytes(value for x in range(256) for value in
                             ((x * 7 + key) % 192, 48 + (x + key * 3) % 192, (x * 3 + key * 5) % 128))
                rows[key] = (tile * ((width + 255) // 256))[:width * 3]
            row = rows[key]
        compressed.append(compressor.compress(b"\0" + row))
    compressed.append(compressor.flush())
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
            + chunk(b"IDAT", b"".join(compressed)) + chunk(b"IEND", b""))


def docker(*args, data=None, check=True, timeout=45):
    result = subprocess.run(["docker", *args], input=data, capture_output=True, timeout=timeout)
    if check and result.returncode:
        raise RuntimeError("Generated capacity Docker operation failed; raw output withheld.")
    return result


# The wrapper remains inside the same restricted cgroup. It reads kernel peak
# accounting after the supervisor/child exit, avoiding sampled-stats undercounts.
MEASURE = r'''dotnet PlantCare.PhotoProcessing.dll --normalize image/png
status=$?
read peak < /sys/fs/cgroup/memory.peak
cpu=0
while read key value rest; do if [ "$key" = usage_usec ]; then cpu=$value; fi; done < /sys/fs/cgroup/cpu.stat
oom=0
while read key value rest; do if [ "$key" = oom_kill ]; then oom=$value; fi; done < /sys/fs/cgroup/memory.events
printf '\nPCMETRICS %s %s %s %s\n' "$status" "$peak" "$cpu" "$oom" >&2
exit "$status"
'''


def measure(image, data):
    if len(data) > 8 * 1024 * 1024:
        raise ValueError("Generated input exceeds the current intake limit.")
    name = "plantcare-photo-capacity-" + uuid.uuid4().hex
    start = time.perf_counter()
    try:
        result = docker("run", "--name", name, "--pull", "never", "--network", "none", "--read-only",
                        "--log-driver", "none", "--memory", "384m", "--memory-swap", "384m", "--cpus", "1",
                        "--pids-limit", "64", "--cap-drop", "ALL", "--security-opt", "no-new-privileges",
                        "--entrypoint", "/bin/sh", "-i", image, "-c", MEASURE, data=data, check=False)
        elapsed = time.perf_counter() - start
        metrics = re.search(rb"\nPCMETRICS (\d+) (\d+) (\d+) (\d+)\n", result.stderr)
        if not metrics:
            raise ValueError("Kernel peak accounting missing; do not substitute sampled or zero memory.")
        status, peak, cpu, oom = map(int, metrics.groups())
        if status != result.returncode or peak <= 0 or peak > 384 * 1024 * 1024 or oom:
            raise ValueError("Capacity job failed its kernel accounting/resource contract.")
        row = {"inputBytes": len(data), "memoryPeakBytes": peak, "cpuSeconds": cpu / 1_000_000,
               "dockerWallSeconds": round(elapsed, 3), "oomKills": oom, "status": "accepted"}
        if status:
            error = result.stderr[:metrics.start()].strip()
            allowed = {b"photo-dimensions-exceeded", b"photo-output-too-large", b"photo-invalid"}
            if error not in allowed or result.stdout:
                raise ValueError("Unexpected capacity rejection; raw errors withheld.")
            row.update(status="rejected", code=error.decode())
        else:
            if result.stderr[:metrics.start()].strip() or len(result.stdout) < 20 or result.stdout[:4] != b"PCP1":
                raise ValueError("Unexpected measured decoder response.")
            width, height, full, thumb = struct.unpack("<IIII", result.stdout[4:20])
            if not (1 <= width <= 1600 and 1 <= height <= 1600 and 4 <= full <= 512 * 1024
                    and 4 <= thumb <= 64 * 1024 and len(result.stdout) == 20 + full + thumb):
                raise ValueError("Measured decoder exceeded its output contract.")
            row.update(width=width, height=height, imageBytes=full, thumbnailBytes=thumb,
                       pairBytes=full + thumb, normalizedSha256=hashlib.sha256(result.stdout[20:20 + full]).hexdigest())
        return row
    finally:
        removed = docker("rm", "--force", name, check=False)
        if removed.returncode:
            remaining = docker("ps", "-aq", "--filter", "name=^/" + name + "$", check=False)
            if remaining.returncode or remaining.stdout.strip():
                raise RuntimeError("Owned capacity job cleanup could not be confirmed.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--bundle", required=True)
    args = parser.parse_args()
    bundle = verify(args.bundle)
    image = bundle["imageId"]
    if docker("image", "inspect", image, "--format", "{{.Os}}/{{.Architecture}}").stdout.strip() != b"linux/amd64":
        raise ValueError("Expected accepted local decoder image.")
    samples = []
    cases = [("detail-0.8mp", 1024, 768, True), ("pattern-12mp", 4000, 3000, False),
             ("pattern-20mp", 5000, 4000, False), ("over-limit-21mp", 5000, 4200, False)]
    for label, width, height, detailed in cases:
        result = measure(image, png_fixture(width, height, detailed))
        if width * height > 20_000_000:
            if result.get("code") != "photo-dimensions-exceeded":
                raise ValueError("Over-limit dimensions were not rejected safely.")
        elif result["status"] != "accepted":
            raise ValueError("A supported generated sample was rejected; retain limits and investigate.")
        samples.append({"sample": label, "sourceWidth": width, "sourceHeight": height, **result})
        print("PASS: " + label + "; " + result["status"] + "; peak " + str(round(result["memoryPeakBytes"] / 1048576, 1)) + " MiB", flush=True)
    pair_max = (512 + 64) * 1024
    report = {"checkedAtUtc": datetime.now(timezone.utc).isoformat(), "decoderImage": image,
              "bundleCandidate": bundle["candidate"], "samples": samples,
              "limits": {"decoderMemoryBytes": 384 * 1048576, "decoderCpus": 1, "inputBytes": 8 * 1048576,
                         "pairBytes": pair_max, "accountBytes": 20 * 1048576, "serviceBytes": 512 * 1048576},
              "projections": {"maximumSizedPairsPerAccount": 20 * 1048576 // pair_max,
                              "maximumSizedPairsService": 512 * 1048576 // pair_max,
                              "thirtyFullyChangedMediaSetsBytes": 30 * 512 * 1048576},
              "limitations": "Single sequential synthetic PNG samples on local Docker; not VPS load/backup certification. Wall time includes Docker startup; kernel peak includes measurement wrapper. Media projections exclude SQL/index/log/key/backup overhead and deduplication."}
    (ROOT / ".operations-local/photo-capacity.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("PASS: local kernel resource samples and bounded media-growth projection recorded; no activation.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, RuntimeError, OSError, subprocess.SubprocessError):
        raise SystemExit("Photo capacity sample failed; preserve current limits and inspect the last safe progress marker.")
