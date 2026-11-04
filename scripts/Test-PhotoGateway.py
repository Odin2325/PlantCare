"""Synthetic end-to-end private IPC acceptance; never touches app data or staging."""
import argparse
import importlib.util
import subprocess
import time
import uuid
from pathlib import Path

spec = importlib.util.spec_from_file_location("sandbox_fixture", Path(__file__).with_name("Test-PhotoProcessingSandbox.py"))
fixture = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fixture)


def docker(*args, data=None, check=True):
    result = subprocess.run(["docker", *args], input=data, capture_output=True, timeout=60)
    if check and result.returncode:
        raise AssertionError("Disposable photo-gateway Docker operation failed.")
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--decoder", default="plantcare-photo-processing:local")
    parser.add_argument("--gateway", default="plantcare-photo-gateway-tests:local")
    parser.add_argument("--client", default="plantcare-photo-client-tests:local")
    args = parser.parse_args()
    image = docker("image", "inspect", args.decoder, "--format", "{{.Id}}").stdout.decode().strip()
    suffix = uuid.uuid4().hex
    name = "plantcare-photo-gateway-test-" + suffix
    volume = "plantcare-photo-ipc-test-" + suffix
    docker("volume", "create", volume)
    try:
        # Only the trusted gateway receives the daemon socket. This is a local
        # test launcher, not a configuration to copy into the API service.
        docker("run", "--detach", "--pull", "never", "--name", name,
               "--network", "none", "--read-only", "--log-driver", "none",
               "--cap-drop", "ALL", "--security-opt", "no-new-privileges",
               "--mount", "type=bind,src=/var/run/docker.sock,dst=/var/run/docker.sock",
               "--mount", "type=volume,src=" + volume + ",dst=/run/plantcare-photo",
               args.gateway, "--image-id", image, "--allowed-uid", "1654", "--socket-gid", "1654")
        for _ in range(40):
            ready = docker("exec", name, "python3", "-c", "import os; raise SystemExit(not os.path.exists('/run/plantcare-photo/gateway.sock'))", check=False)
            if ready.returncode == 0:
                break
            time.sleep(.1)
        else:
            raise AssertionError("Private test gateway did not start.")

        docker("exec", name, "python3", "-c", "import os,stat; p='/run/plantcare-photo'; d=os.stat(p); s=os.stat(p+'/gateway.sock'); assert (d.st_uid,d.st_gid,stat.S_IMODE(d.st_mode))==(0,1654,0o750); assert (s.st_uid,s.st_gid,stat.S_IMODE(s.st_mode))==(0,1654,0o660)")
        print("PASS: root-owned private directory and socket use API group 1654", flush=True)

        def client(data, uid="0", cancel=False, process_uid="1654"):
            client_name = "plantcare-photo-client-test-" + uuid.uuid4().hex
            try:
                return docker("run", "--rm", "--pull", "never", "--name", client_name,
                              "--network", "none", "--read-only", "--log-driver", "none",
                              "--user", process_uid + ":1654", "--cap-drop", "ALL", "--security-opt", "no-new-privileges",
                              "--mount", "type=volume,src=" + volume + ",dst=/run/plantcare-photo,readonly",
                              "-i", args.client, "/run/plantcare-photo/gateway.sock", uid, *(["cancel"] if cancel else []), data=data, check=False)
            finally:
                docker("rm", "--force", client_name, check=False)

        for _ in range(2):
            result = client(fixture.png())
            assert result.returncode == 0 and result.stdout.strip() == b"8x4" and not result.stderr, "private client/gateway/decoder round trip failed"
        print("PASS: API-side .NET client -> authenticated Unix gateway -> fresh restricted decoder", flush=True)
        invalid = client(b"not a photo")
        assert invalid.returncode != 0 and invalid.stderr.strip() == b"photo-invalid" and not invalid.stdout
        wrong = client(fixture.png(), uid="42")
        assert wrong.returncode != 0 and wrong.stderr.strip() == b"photo-processing-unavailable" and not wrong.stdout
        wrong_peer = client(fixture.png(), process_uid="1655")
        assert wrong_peer.returncode != 0 and wrong_peer.stderr.strip() == b"photo-processing-unavailable" and not wrong_peer.stdout
        read_only = docker("run", "--rm", "--pull", "never", "--network", "none", "--read-only",
                           "--user", "0:1654", "--cap-drop", "ALL", "--security-opt", "no-new-privileges",
                           "--mount", "type=volume,src=" + volume + ",dst=/run/plantcare-photo,readonly",
                           "--entrypoint", "python3", args.gateway, "-c",
                           "import errno,os\ntry: os.open('/run/plantcare-photo/probe', os.O_WRONLY|os.O_CREAT,0o600)\nexcept OSError as e: assert e.errno==errno.EROFS\nelse: raise AssertionError('socket mount is writable')", check=False)
        assert read_only.returncode == 0, "shared client socket mount must enforce read-only even for its directory owner"
        print("PASS: invalid photos, wrong peer identities and client-directory writes fail closed", flush=True)
        canceled = client(fixture.png(), cancel=True)
        assert canceled.returncode == 3 and canceled.stderr.strip() == b"photo-test-canceled"
        for _ in range(10):
            recovered = client(fixture.png())
            if recovered.returncode == 0:
                break
            assert recovered.stderr.strip() == b"photo-busy", "gateway failed after cancellation"
            time.sleep(.1)
        else:
            raise AssertionError("Gateway did not release admission after canceled job cleanup.")
        remaining = docker("container", "ls", "--all", "--filter", "ancestor=" + image, "--format", "{{.Names}}").stdout.splitlines()
        assert not any(item.startswith(b"plantcare-photo-job-") for item in remaining), "a photo job remains after cleanup"
        print("PASS: cancellation cleans the job before reuse; no decoder job remains", flush=True)
    finally:
        docker("stop", "--time", "20", name, check=False)
        docker("rm", "--force", name, check=False)
        # The generated volume contains only this test's Unix socket, no photos.
        docker("volume", "rm", volume, check=False)


if __name__ == "__main__":
    main()
