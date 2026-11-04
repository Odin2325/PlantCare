"""Generated-only local gateway stop/kill/restart acceptance; never a host repair tool."""
import argparse
import concurrent.futures
import json
import re
import subprocess
import time
import uuid
from photo_capacity import png_fixture
STEP = "configuration"


def docker(*args, data=None, check=True, timeout=60):
    result = subprocess.run(["docker", *args], input=data, capture_output=True, timeout=timeout)
    if check and result.returncode:
        raise RuntimeError("Disposable interruption operation failed; raw output withheld.")
    return result


def main():
    global STEP
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--decoder", required=True)
    parser.add_argument("--gateway", default="plantcare-photo-gateway-tests:local")
    parser.add_argument("--client", default="plantcare-photo-client-tests:local")
    args = parser.parse_args()
    ids = {key: docker("image", "inspect", tag, "--format", "{{.Id}}").stdout.decode().strip()
           for key, tag in (("decoder", args.decoder), ("gateway", args.gateway), ("client", args.client))}
    if any(not re.fullmatch(r"sha256:[a-f0-9]{64}", value) for value in ids.values()):
        raise ValueError("Pinned local images required.")
    label = docker("image", "inspect", ids["decoder"], "--format", '{{index .Config.Labels "org.plantcare.test-only"}}').stdout.strip()
    if label != b"interruption-delay":
        raise ValueError("Use the test-only delayed decoder fixture for deterministic interruption.")
    token = uuid.uuid4().hex
    gateway, duplicate = "plantcare-photo-fault-" + token, "plantcare-photo-fault-second-" + token
    volume = "plantcare-photo-fault-ipc-" + token
    owned_jobs = set()
    docker("volume", "create", volume)

    def helper(code, *values, writable=False):
        return docker("run", "--rm", "--pull", "never", "--network", "none", "--read-only", "--log-driver", "none",
                      "--user", "0:1654", "--cap-drop", "ALL", "--security-opt", "no-new-privileges",
                      "--mount", "type=volume,src=" + volume + ",dst=/run/plantcare-photo" + ("" if writable else ",readonly"),
                      "--entrypoint", "python3", ids["gateway"], "-c", code, *values)

    def running():
        return docker("inspect", "--format", "{{.State.Running}}", gateway).stdout.strip() == b"true"

    def ready():
        for _ in range(60):
            if not running(): raise ValueError("Owned gateway stopped before readiness.")
            check = docker("exec", gateway, "python3", "-c",
                           "import os; raise SystemExit(not os.path.exists('/run/plantcare-photo/gateway.sock'))", check=False)
            if check.returncode == 0: return
            time.sleep(.05)
        raise ValueError("Owned gateway did not become ready.")

    def start_refused(name):
        docker("start", name)
        code = docker("wait", name, timeout=15).stdout.strip()
        if code != b"1": raise ValueError("Interrupted/duplicate gateway restart was not refused.")

    def client(data):
        name = "plantcare-photo-fault-client-" + uuid.uuid4().hex
        try:
            return docker("run", "--rm", "--pull", "never", "--name", name, "--network", "none", "--read-only", "--log-driver", "none",
                          "--user", "1654:1654", "--cap-drop", "ALL", "--security-opt", "no-new-privileges",
                          "--mount", "type=volume,src=" + volume + ",dst=/run/plantcare-photo,readonly", "-i", ids["client"],
                          "/run/plantcare-photo/gateway.sock", "0", data=data, check=False)
        finally:
            docker("rm", "--force", name, check=False)

    def paused_job(future):
        deadline = time.monotonic() + 20
        while time.monotonic() < deadline:
            if future.done(): raise ValueError("Generated decoder finished before fault injection; retry deliberately.")
            result = docker("exec", gateway, "python3", "-c",
                            "from pathlib import Path; p=Path('/run/plantcare-photo/job-pending.json'); print(p.read_text() if p.exists() else '{}')")
            state = json.loads(result.stdout)
            if state:
                name = state.get("name", "")
                if state.get("format") != 1 or state.get("imageId") != ids["decoder"] or not re.fullmatch(r"plantcare-photo-job-[a-f0-9]{32}", name):
                    raise ValueError("Unexpected owned interruption marker.")
                owned_jobs.add(name)
                info = docker("inspect", name, check=False)
                if info.returncode == 0:
                    description = json.loads(info.stdout)[0]
                    if description["Image"] != ids["decoder"] or description["Config"].get("Labels", {}).get("plantcare.photo-job") != "1":
                        raise ValueError("Unexpected owned decoder identity.")
                    if docker("pause", name, check=False).returncode == 0:
                        return name
            time.sleep(.02)
        raise ValueError("No generated active job was paused.")

    def success():
        result = client(png_fixture(8, 4))
        if result.returncode or result.stdout.strip() != b"8x4" or result.stderr:
            raise ValueError("Owned gateway did not recover normal IPC.")

    def absent(name):
        result = docker("container", "ls", "--all", "--filter", "name=^/" + name + "$", "--format", "{{.Names}}")
        return not result.stdout.strip()

    def recover_owned_fixture(job):
        # Simulate a reviewed recovery only for this test's recorded UUID job.
        if running() or job not in owned_jobs: raise ValueError("Fixture recovery ownership is unproven.")
        docker("rm", "--force", job, check=False)
        if not absent(job): raise ValueError("Owned paused job still exists.")
        helper("""import json,os,stat,sys
p='/run/plantcare-photo/job-pending.json'
value=json.load(open(p))
assert value=={'format':1,'name':sys.argv[1],'imageId':sys.argv[2]}
assert stat.S_IMODE(os.stat(p).st_mode)==0o600 and os.stat(p).st_uid==0
os.unlink(p)
socket='/run/plantcare-photo/gateway.sock'
if os.path.lexists(socket):
 assert stat.S_ISSOCK(os.lstat(socket).st_mode)
 os.unlink(socket)
""", job, ids["decoder"], writable=True)

    try:
        launch = ["create", "--pull", "never", "--network", "none", "--read-only", "--log-driver", "none", "--user", "0:1654",
                  "--cap-drop", "ALL", "--security-opt", "no-new-privileges",
                  "--mount", "type=bind,src=/var/run/docker.sock,dst=/var/run/docker.sock",
                  "--mount", "type=volume,src=" + volume + ",dst=/run/plantcare-photo"]
        command = [ids["gateway"], "--image-id", ids["decoder"], "--allowed-uid", "1654", "--socket-gid", "1654"]
        docker(*launch, "--name", gateway, *command)
        docker("start", gateway); ready(); success()
        docker(*launch, "--name", duplicate, *command)
        start_refused(duplicate); success()
        print("PASS: duplicate gateway cannot take the private lock; existing gateway remains usable.", flush=True)

        data = png_fixture(5000, 4000)
        with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
            STEP = "graceful-pause"
            future = pool.submit(client, data); job = paused_job(future)
            STEP = "graceful-stop"
            docker("stop", "--time", "20", gateway)
            STEP = "graceful-client-and-job"
            if docker("inspect", "--format", "{{.State.ExitCode}}", gateway).stdout.strip() != b"0":
                raise ValueError("Graceful gateway reported unsuccessful shutdown.")
            if not absent(job): raise ValueError("Graceful shutdown left the paused owned job behind.")
            if future.result(timeout=30).returncode == 0: raise ValueError("Interruption fixture completed before shutdown.")
            STEP = "graceful-state"
            helper("from pathlib import Path; assert not Path('/run/plantcare-photo/job-pending.json').exists(); assert not Path('/run/plantcare-photo/gateway.sock').exists()")
            STEP = "graceful-restart"
            docker("start", gateway); ready(); success()
            print("PASS: SIGTERM cleans a paused real decoder, pending marker and socket before restart.", flush=True)

            STEP = "forced-pause"
            future = pool.submit(client, data); job = paused_job(future)
            docker("kill", "--signal", "KILL", gateway)
            if future.result(timeout=30).returncode == 0:
                raise ValueError("Forced gateway loss incorrectly confirmed a save.")
            # Remove only the known socket after its owned gateway is dead, to
            # prove the pending marker blocks restart independently of the socket.
            helper("import os,stat; p='/run/plantcare-photo/gateway.sock'; assert stat.S_ISSOCK(os.lstat(p).st_mode); os.unlink(p)", writable=True)
            start_refused(gateway)
            if docker("inspect", "--format", "{{.State.Paused}}", job).stdout.strip() != b"true":
                raise ValueError("Forced-loss fixture no longer represents an unfinished job.")
            print("PASS: SIGKILL preserves pending state and refuses restart without touching the unfinished decoder.", flush=True)
            recover_owned_fixture(job)
            docker("start", gateway); ready(); success()
            print("PASS: reviewed cleanup of only the recorded UUID job permits restart and normal decoding.", flush=True)

        docker("stop", "--time", "20", gateway)
        legacy = "plantcare-photo-job-" + uuid.uuid4().hex
        owned_jobs.add(legacy)
        docker("create", "--name", legacy, "--network", "none", "--read-only", ids["decoder"])
        start_refused(gateway)
        if docker("inspect", "--format", "{{.State.Status}}", legacy).stdout.strip() != b"created":
            raise ValueError("Startup modified the legacy orphan fixture.")
        docker("rm", legacy)
        docker("start", gateway); ready(); success()
        print("PASS: startup refuses a legacy unlabeled created job; no automatic orphan deletion.", flush=True)
    finally:
        docker("stop", "--time", "20", gateway, check=False)
        for name in (gateway, duplicate): docker("rm", "--force", name, check=False)
        for name in owned_jobs:
            docker("rm", "--force", name, check=False)
            if not absent(name): raise RuntimeError("Owned fault job cleanup failed.")
        docker("volume", "rm", volume)


if __name__ == "__main__":
    try:
        main()
    except (ValueError, RuntimeError, OSError, subprocess.SubprocessError, AssertionError) as error:
        # Only our own fixed messages; never subprocess/browser output or input.
        reason = str(error) if type(error) in (ValueError, RuntimeError) else "operation did not complete"
        raise SystemExit("Local photo interruption failed at " + STEP + ": " + reason)
