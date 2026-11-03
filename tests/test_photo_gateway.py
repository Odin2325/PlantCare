import asyncio
import contextlib
import os
import socket
import struct
import sys
import tempfile
import unittest
from scripts.photo_gateway import Gateway, DockerLauncher, Failure, bounded


def photo():
    return b"PCP1" + struct.pack("<IIII", 8, 4, 4, 4) + b"\xff\xd8\xff\xd9" * 2


class FakeLauncher:
    def __init__(self):
        self.calls = 0
        self.started = asyncio.Event()
        self.release = asyncio.Event()
        self.cleaned = asyncio.Event()
        self.block = False

    async def normalize(self, data, mime):
        self.calls += 1
        self.started.set()
        try:
            if self.block:
                await self.release.wait()
            return photo()
        finally:
            self.cleaned.set()


@unittest.skipUnless(sys.platform == "linux", "SO_PEERCRED acceptance runs on Linux")
class GatewayTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="pc-photo-test-")
        self.path = os.path.join(self.directory.name, "gateway.sock")
        self.launcher = FakeLauncher()
        self.gateway = Gateway(self.launcher, os.geteuid())
        self.server = await asyncio.start_unix_server(self.gateway.handle, self.path)

    async def asyncTearDown(self):
        self.server.close()
        await self.server.wait_closed()
        for task in list(self.gateway.connections):
            task.cancel()
        await asyncio.gather(*list(self.gateway.connections), return_exceptions=True)
        self.directory.cleanup()

    async def connect(self, body=b"abc", header=None):
        reader, writer = await asyncio.open_unix_connection(self.path)
        writer.write(header if header is not None else b"PCR1\2" + struct.pack("<I", len(body)) + body)
        await writer.drain()
        return reader, writer

    async def response(self, reader):
        header = await asyncio.wait_for(reader.readexactly(9), 2)
        self.assertEqual(b"PCS1", header[:4])
        size = struct.unpack("<I", header[5:])[0]
        self.assertLessEqual(size, 20 + 576 * 1024)
        return header[4], await reader.readexactly(size)

    async def test_success_and_reuse(self):
        for _ in range(2):
            reader, writer = await self.connect()
            self.assertEqual((0, photo()), await self.response(reader))
            self.assertEqual(b"", await reader.read())
            writer.close(); await writer.wait_closed()
        self.assertEqual(2, self.launcher.calls)

    async def test_wrong_peer_identity_never_launches_decoder(self):
        self.gateway.allowed_uid = os.geteuid() + 1
        reader, writer = await asyncio.open_unix_connection(self.path)
        self.assertEqual((1, b"photo-processing-unavailable"), await self.response(reader))
        writer.close(); await writer.wait_closed()
        self.assertEqual(0, self.launcher.calls)

    async def test_oversize_and_invalid_format_never_launch(self):
        for header, code in ((b"PCR1\2" + struct.pack("<I", 8 * 1024 * 1024 + 1), b"photo-too-large"),
                             (b"PCR1\7" + struct.pack("<I", 0), b"photo-invalid")):
            reader, writer = await self.connect(header=header)
            self.assertEqual((1, code), await self.response(reader))
            writer.close(); await writer.wait_closed()
        self.assertEqual(0, self.launcher.calls)

    async def test_busy_does_not_queue_and_disconnect_cleans_before_reuse(self):
        self.launcher.block = True
        reader, writer = await self.connect()
        await self.launcher.started.wait()
        other_reader, other_writer = await asyncio.open_unix_connection(self.path)
        self.assertEqual((1, b"photo-busy"), await self.response(other_reader))
        other_writer.close(); await other_writer.wait_closed()
        self.assertEqual(1, self.launcher.calls)
        writer.close(); await writer.wait_closed()
        await asyncio.wait_for(self.launcher.cleaned.wait(), 2)
        for _ in range(100):
            if not self.gateway.busy:
                break
            await asyncio.sleep(.01)
        self.assertFalse(self.gateway.busy)
        self.launcher.block = False
        again, again_writer = await self.connect()
        self.assertEqual((0, photo()), await self.response(again))
        again_writer.close(); await again_writer.wait_closed()

    async def test_trailing_data_cancels_job(self):
        self.launcher.block = True
        reader, writer = await self.connect()
        await self.launcher.started.wait()
        writer.write(b"extra"); await writer.drain()
        self.assertEqual((1, b"photo-invalid"), await self.response(reader))
        await asyncio.wait_for(self.launcher.cleaned.wait(), 2)
        writer.close(); await writer.wait_closed()


class LimitsTests(unittest.IsolatedAsyncioTestCase):
    async def test_reader_stops_at_one_excess_byte(self):
        reader = asyncio.StreamReader()
        reader.feed_data(b"a" * 100); reader.feed_eof()
        with self.assertRaises(Failure):
            await bounded(reader, 4)

    async def test_cleanup_failure_poison_is_durable(self):
        launcher = DockerLauncher("sha256:" + "a" * 64)
        async def fail(*args):
            return 1, b""
        launcher.control = fail
        with self.assertRaises(Failure):
            await launcher.cleanup("plantcare-photo-job-synthetic")
        self.assertTrue(launcher.poisoned)
        with self.assertRaises(Failure) as error:
            await launcher.normalize(b"", "image/png")
        self.assertEqual("photo-processing-unavailable", error.exception.code)

    async def test_unpinned_image_is_rejected(self):
        for image in ("latest", "plantcare-photo:local", "sha256:bad", "; shell"):
            with self.assertRaises(ValueError):
                DockerLauncher(image)

    async def test_cancellation_waits_for_creation_then_cleans_before_return(self):
        launcher = DockerLauncher("sha256:" + "a" * 64)
        started, release, removed = asyncio.Event(), asyncio.Event(), asyncio.Event()
        async def control(*args):
            if args[0] == "create":
                started.set()
                await release.wait()
                return 0, b"synthetic-id"
            if args[0] == "rm":
                removed.set()
                return 0, b""
            self.fail("Unexpected control operation")
        launcher.control = control
        job = asyncio.create_task(launcher.normalize(b"synthetic", "image/png"))
        await started.wait()
        job.cancel()
        await asyncio.sleep(.01)
        self.assertFalse(job.done())
        release.set()
        with self.assertRaises(asyncio.CancelledError):
            await job
        self.assertTrue(removed.is_set())
        self.assertFalse(launcher.poisoned)


if __name__ == "__main__":
    unittest.main()
