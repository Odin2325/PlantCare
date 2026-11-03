"""Focused, synthetic-only Linux acceptance. Build the photo image first."""
import argparse
import struct
import subprocess
import uuid
import zlib


def png():
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 8, 4, 8, 2, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress((b"\0" + b"\0\x80\0" * 8) * 4)) + chunk(b"IEND", b""))


def run(image, data, mime="image/png", omit=None):
    name = "plantcare-photo-test-" + uuid.uuid4().hex
    controls = {
        "network": ["--network", "none"], "readonly": ["--read-only"],
        "memory": ["--memory", "384m", "--memory-swap", "384m"],
        "cpu": ["--cpus", "1"], "pids": ["--pids-limit", "64"],
        "capabilities": ["--cap-drop", "ALL"],
        "privileges": ["--security-opt", "no-new-privileges"],
    }
    command = ["docker", "run", "--rm", "--pull", "never", "--name", name, "--log-driver", "none", "-i"]
    for key, values in controls.items():
        if key != omit:
            command.extend(values)
    command.extend([image, mime])
    try:
        return subprocess.run(command, input=data, capture_output=True, timeout=25, check=False)
    finally:
        # Only the uniquely named disposable container made by this invocation.
        subprocess.run(["docker", "rm", "--force", name], capture_output=True, timeout=10, check=False)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--image", default="plantcare-photo-processing:local")
    args = parser.parse_args()
    result = run(args.image, png())
    assert result.returncode == 0, "sandboxed PNG normalization failed: " + result.stderr.decode("ascii", "replace")
    assert result.stderr == b""
    assert result.stdout[:4] == b"PCP1"
    width, height, full, thumb = struct.unpack("<IIII", result.stdout[4:20])
    assert (width, height) == (8, 4)
    assert 4 <= full <= 524288 and 4 <= thumb <= 65536
    assert len(result.stdout) == 20 + full + thumb
    for data in (result.stdout[20:20 + full], result.stdout[20 + full:]):
        assert data.startswith(b"\xff\xd8") and data.endswith(b"\xff\xd9")
    print("PASS: real Linux decoder, bounded private binary response")
    jpeg = run(args.image, result.stdout[20:20 + full], mime="image/jpeg")
    assert jpeg.returncode == 0 and jpeg.stdout[:4] == b"PCP1" and not jpeg.stderr
    print("PASS: normalized JPEG passes the strict worker policy")
    for omitted in ("network", "readonly", "memory", "cpu", "pids", "capabilities", "privileges"):
        failure = run(args.image, png(), omit=omitted)
        assert failure.returncode != 0 and not failure.stdout, "missing sandbox control was accepted: " + omitted
        assert failure.stderr.strip() == b"photo-sandbox-unavailable", "sandbox error was not sanitized"
    print("PASS: each missing sandbox control fails closed")
    invalid = run(args.image, b"not a photograph")
    assert invalid.returncode != 0 and invalid.stderr.strip() == b"photo-invalid" and not invalid.stdout
    print("PASS: child validation failure stays sanitized")


if __name__ == "__main__":
    main()
