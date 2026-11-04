"""Private Linux photo gateway. Fixed local Docker launcher; never a public service."""
import argparse
import asyncio
import contextlib
import json
import os
import re
import signal
import socket
import stat
import struct
import sys
import uuid
if sys.platform == "linux":
    import fcntl


class RuntimeState:
    """Private restart barrier. Contains generated job identity, never photo data."""
    def __init__(self, directory):
        self.directory = directory
        self.marker = os.path.join(directory, "job-pending.json")
        self.active = None
        self.lock = os.open(os.path.join(directory, "gateway.lock"),
                            os.O_RDWR | os.O_CREAT | os.O_NOFOLLOW | os.O_CLOEXEC, 0o600)
        try:
            self.validate(os.fstat(self.lock))
            fcntl.flock(self.lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
            if os.path.lexists(self.marker):
                raise ValueError("Pending job requires operator review.")
        except BaseException:
            os.close(self.lock)
            raise

    @staticmethod
    def validate(info):
        if not stat.S_ISREG(info.st_mode) or info.st_uid != os.geteuid() or stat.S_IMODE(info.st_mode) != 0o600 or info.st_nlink != 1:
            raise ValueError("Private gateway state has unsafe ownership or mode.")

    def sync_directory(self):
        fd = os.open(self.directory, os.O_RDONLY | os.O_DIRECTORY | os.O_CLOEXEC)
        try:
            os.fsync(fd)
        finally:
            os.close(fd)

    def begin(self, name, image):
        data = json.dumps({"format": 1, "name": name, "imageId": image}, sort_keys=True).encode()
        fd = os.open(self.marker, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW | os.O_CLOEXEC, 0o600)
        try:
            self.validate(os.fstat(fd))
            with os.fdopen(fd, "wb", closefd=False) as stream:
                stream.write(data); stream.flush(); os.fsync(fd)
            info = os.fstat(fd)
            self.sync_directory()
            self.active = ((info.st_dev, info.st_ino), data)
        finally:
            os.close(fd)

    def clear(self):
        if self.active is None:
            raise ValueError("No owned gateway marker to clear.")
        fd = os.open(self.marker, os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC)
        try:
            info = os.fstat(fd); self.validate(info)
            if (info.st_dev, info.st_ino) != self.active[0] or os.read(fd, 1025) != self.active[1]:
                raise ValueError("Gateway marker changed; review before restart.")
            os.unlink(self.marker)
            self.sync_directory()
            self.active = None
        finally:
            os.close(fd)

    def close(self):
        os.close(self.lock)

MAX_INPUT = 8 * 1024 * 1024
MAX_RESULT = 20 + 512 * 1024 + 64 * 1024
SAFE_ERRORS = {
    "photo-invalid", "photo-too-large", "photo-format-unsupported", "photo-animation-unsupported",
    "photo-dimensions-exceeded", "photo-output-too-large", "photo-processing-failed",
    "photo-processing-unavailable", "photo-processing-timeout", "photo-busy",
}


class Failure(Exception):
    def __init__(self, code="photo-processing-failed"):
        self.code = code if code in SAFE_ERRORS else "photo-processing-failed"


async def bounded(reader, maximum):
    result = bytearray()
    while True:
        chunk = await reader.read(min(8192, maximum + 1 - len(result)))
        if not chunk:
            return bytes(result)
        result.extend(chunk)
        if len(result) > maximum:
            raise Failure()


def validate_result(data):
    if len(data) < 20 or data[:4] != b"PCP1":
        raise Failure()
    width, height, image, thumb = struct.unpack("<IIII", data[4:20])
    if not (0 < width <= 1600 and 0 < height <= 1600 and 4 <= image <= 524288
            and 4 <= thumb <= 65536 and 20 + image + thumb == len(data)):
        raise Failure()
    for part in (data[20:20 + image], data[20 + image:]):
        if not (part.startswith(b"\xff\xd8") and part.endswith(b"\xff\xd9")):
            raise Failure()


class DockerLauncher:
    def __init__(self, image_id, binary="/usr/bin/docker", runtime_state=None):
        if not re.fullmatch(r"sha256:[a-f0-9]{64}", image_id):
            raise ValueError("A pinned local image ID is required.")
        self.image_id = image_id
        self.binary = binary
        self.poisoned = False
        self.runtime_state = runtime_state

    async def ensure_idle(self):
        # Include old unlabeled jobs and old image versions, in every state.
        # Never auto-remove a container whose interruption has not been reviewed.
        code, output = await self.control("container", "ls", "--all", "--filter",
                                          "name=^/plantcare-photo-job-", "--format", "{{.Names}}")
        if code != 0 or output.strip():
            self.poisoned = True
            raise Failure("photo-processing-unavailable")

    async def spawn(self, *arguments, stdin=None):
        return await asyncio.create_subprocess_exec(
            self.binary, "--host", "unix:///var/run/docker.sock", *arguments,
            stdin=stdin, stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.PIPE,
            env={"PATH": "/usr/bin:/bin", "HOME": "/nonexistent", "DOCKER_CONFIG": "/nonexistent"},
            limit=8192)

    async def control(self, *arguments):
        process = await self.spawn(*arguments)
        try:
            async with asyncio.timeout(5):
                output, _ = await asyncio.gather(bounded(process.stdout, 4096), bounded(process.stderr, 4096))
                await process.wait()
                return process.returncode, output
        finally:
            if process.returncode is None:
                with contextlib.suppress(ProcessLookupError):
                    process.kill()
                await asyncio.wait_for(process.wait(), 5)

    async def cleanup(self, name):
        try:
            result, _ = await self.control("rm", "--force", name)
            if result != 0:
                code, output = await self.control("container", "ls", "--all", "--filter", "name=^/" + name + "$", "--format", "{{.Names}}")
                if code != 0 or output.strip():
                    raise Failure()
        except BaseException:
            self.poisoned = True
            raise

    async def normalize(self, data, mime):
        if self.poisoned:
            raise Failure("photo-processing-unavailable")
        if len(data) > MAX_INPUT:
            raise Failure("photo-too-large")
        if mime not in ("image/jpeg", "image/png", "image/webp"):
            raise Failure("photo-format-unsupported")
        name = "plantcare-photo-job-" + uuid.uuid4().hex
        process = None
        tasks = []
        marked = False
        try:
            if self.runtime_state is not None:
                try:
                    self.runtime_state.begin(name, self.image_id)
                    marked = True
                except BaseException:
                    self.poisoned = True
                    raise
            # Finish creation before attachment. A cancellation during creation
            # must settle that bounded control call before checking job absence.
            creation = asyncio.create_task(self.control(
                "create", "--rm", "--pull", "never", "--name", name, "--label", "plantcare.photo-job=1", "--log-driver", "none", "-i",
                "--network", "none", "--read-only", "--memory", "384m", "--memory-swap", "384m",
                "--cpus", "1", "--pids-limit", "64", "--cap-drop", "ALL",
                "--security-opt", "no-new-privileges", self.image_id, mime))
            try:
                code, _ = await asyncio.shield(creation)
            except asyncio.CancelledError:
                try:
                    await creation
                except BaseException:
                    self.poisoned = True
                raise
            except BaseException:
                self.poisoned = True
                raise
            if code != 0:
                raise Failure("photo-processing-unavailable")
            process = await self.spawn("start", "--attach", "--interactive", name, stdin=asyncio.subprocess.PIPE)

            async def send():
                try:
                    process.stdin.write(data)
                    await process.stdin.drain()
                except (BrokenPipeError, ConnectionResetError):
                    pass  # A child may reject the request before consuming its body.
                finally:
                    process.stdin.close()

            tasks = [asyncio.create_task(send()), asyncio.create_task(bounded(process.stdout, MAX_RESULT)),
                     asyncio.create_task(bounded(process.stderr, 1024))]
            async with asyncio.timeout(30):
                _, output, errors = await asyncio.gather(*tasks)
                await process.wait()
            if process.returncode != 0:
                raise Failure(errors.decode("ascii", "replace").strip())
            if errors:
                raise Failure()
            validate_result(output)
            return output
        except TimeoutError:
            raise Failure("photo-processing-timeout") from None
        finally:
            for task in tasks:
                task.cancel()
            await asyncio.gather(*tasks, return_exceptions=True)
            # Killing the Docker CLI does not kill its container: remove the exact
            # generated job and confirm absence before allowing another request.
            try:
                if process is not None and process.returncode is None:
                    with contextlib.suppress(ProcessLookupError):
                        process.kill()
                    # After killing this trusted CLI, drain its remaining finite
                    # pipe buffers so asyncio can finish reaping it.
                    await asyncio.wait_for(process.communicate(), 5)
            except BaseException:
                self.poisoned = True
                raise
            finally:
                await self.cleanup(name)
                if marked and not self.poisoned:
                    try:
                        self.runtime_state.clear()
                    except BaseException:
                        self.poisoned = True
                        raise


class Gateway:
    def __init__(self, launcher, allowed_uid):
        self.launcher = launcher
        self.allowed_uid = allowed_uid
        self.busy = False
        self.connections = set()
        self.closing = False

    async def handle(self, reader, writer):
        current = asyncio.current_task()
        self.connections.add(current)
        admitted = False
        job = watch = None
        try:
            if self.closing:
                raise Failure("photo-processing-unavailable")
            credentials = writer.get_extra_info("socket").getsockopt(socket.SOL_SOCKET, socket.SO_PEERCRED, 12)
            _, uid, _ = struct.unpack("3i", credentials)
            if uid != self.allowed_uid:
                raise Failure("photo-processing-unavailable")
            if self.busy:
                raise Failure("photo-busy")
            self.busy = admitted = True
            async with asyncio.timeout(5):
                header = await reader.readexactly(9)
                size = struct.unpack("<I", header[5:])[0]
                mime = {1: "image/jpeg", 2: "image/png", 3: "image/webp"}.get(header[4])
                if header[:4] != b"PCR1" or mime is None:
                    raise Failure("photo-invalid")
                if size > MAX_INPUT:
                    raise Failure("photo-too-large")
                data = await reader.readexactly(size)
            job = asyncio.create_task(self.launcher.normalize(data, mime))
            # Clients keep the socket open while awaiting the reply. EOF or any
            # trailing input cancels the job and waits for container cleanup.
            watch = asyncio.create_task(reader.read(1))
            done, _ = await asyncio.wait({job, watch}, return_when=asyncio.FIRST_COMPLETED)
            if watch in done:
                raise Failure("photo-invalid")
            result = await job
            await self.reply(writer, False, result)
        except asyncio.CancelledError:
            raise
        except Exception as error:
            code = error.code if isinstance(error, Failure) else "photo-processing-failed"
            with contextlib.suppress(Exception):
                await self.reply(writer, True, code.encode("ascii"))
        finally:
            for task in (watch, job):
                if task is not None:
                    task.cancel()
                    with contextlib.suppress(BaseException):
                        await task
            if admitted:
                self.busy = False
            writer.close()
            with contextlib.suppress(Exception):
                await asyncio.wait_for(writer.wait_closed(), 2)
            self.connections.discard(current)

    @staticmethod
    async def reply(writer, error, data):
        writer.write(b"PCS1" + bytes([int(error)]) + struct.pack("<I", len(data)) + data)
        await asyncio.wait_for(writer.drain(), 2)


async def serve(args):
    if sys.platform != "linux" or args.allowed_uid <= 0:
        raise ValueError("A Linux gateway and an explicit non-root API UID are required.")
    path = os.path.abspath(args.socket_path)
    directory = os.path.dirname(path)
    if len(path.encode()) > 100 or not os.path.isdir(directory):
        raise ValueError("Prepare a private runtime directory before startup.")
    info = os.stat(directory)
    if info.st_uid != os.geteuid() or info.st_mode & 0o007 or info.st_mode & stat.S_IWGRP:
        raise ValueError("The socket directory must be gateway-owned and private.")
    runtime = RuntimeState(directory)
    try:
        await serve_locked(args, path, runtime)
    finally:
        runtime.close()


async def serve_locked(args, path, runtime):
    # Never unlink unknown state. Preserve RuntimeDirectory across service stops
    # so an interrupted creation cannot evade the restart barrier.
    launcher = DockerLauncher(args.image_id, runtime_state=runtime)
    await launcher.ensure_idle()
    if os.path.lexists(path):
        raise ValueError("Socket path already exists.")
    gateway = Gateway(launcher, args.allowed_uid)
    # Python 3.13 added automatic socket removal. Keep explicit ownership-based
    # removal after job cleanup, consistently with older supported Python.
    options = {"cleanup_socket": False} if sys.version_info >= (3, 13) else {}
    server = await asyncio.start_unix_server(gateway.handle, path=path, backlog=1, limit=8192, **options)
    socket_identity = os.lstat(path)
    stop = asyncio.Event()
    loop = asyncio.get_running_loop()
    for signum in (signal.SIGTERM, signal.SIGINT):
        loop.add_signal_handler(signum, stop.set)
    try:
        os.chown(path, -1, args.socket_gid)
        os.chmod(path, 0o660)
        await stop.wait()
    finally:
        gateway.closing = True
        server.close()
        # Modern asyncio waits for active transports too. Cancel/reap handlers
        # first, otherwise a paused decoder can keep shutdown waiting until the
        # service manager kills the gateway before it removes its job.
        for task in list(gateway.connections):
            task.cancel()
        await asyncio.gather(*list(gateway.connections), return_exceptions=True)
        await server.wait_closed()
        if os.path.lexists(path):
            current = os.lstat(path)
            if not stat.S_ISSOCK(current.st_mode) or (current.st_dev, current.st_ino) != (socket_identity.st_dev, socket_identity.st_ino):
                raise Failure("photo-processing-unavailable")
            os.unlink(path)
        if launcher.poisoned:
            raise Failure("photo-processing-unavailable")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--image-id", required=True)
    parser.add_argument("--socket-path", default="/run/plantcare-photo/gateway.sock")
    parser.add_argument("--allowed-uid", type=int, required=True)
    parser.add_argument("--socket-gid", type=int, required=True)
    args = parser.parse_args()
    os.umask(0o077)
    try:
        asyncio.run(serve(args))
    except Exception:
        print("photo-gateway-unavailable", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
