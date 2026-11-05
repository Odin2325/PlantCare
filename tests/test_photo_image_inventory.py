"""Notice-archive integrity checks without Docker, packages or host access."""
import hashlib
import io
import json
from pathlib import Path
import sys
import tarfile
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from photo_image_inventory import verify_inventory


class ImageNoticeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.data = {"usr/share/dotnet/LICENSE.txt": b"fixture framework terms",
                     "app/ThirdPartyNotices/Magick.NET-15.0.0.txt": b"fixture native terms"}
        self.report = {"schemaVersion": 1, "bundleCandidate": "a" * 40, "imageId": "sha256:" + "b" * 64,
                       "licenseReview": "incomplete", "noticeFiles": [
                           {"path": name, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}
                           for name, data in self.data.items()]}
        self.pack(list(self.data))

    def tearDown(self): self.temp.cleanup()

    def pack(self, names, link=False):
        with tarfile.open(self.root / "image-notices.tar", "w") as archive:
            for name in names:
                member = tarfile.TarInfo(name)
                if link:
                    member.type = tarfile.SYMTYPE; member.linkname = "/outside-fixture"
                    archive.addfile(member)
                else:
                    member.size = len(self.data[name]); archive.addfile(member, io.BytesIO(self.data[name]))
        self.report["noticeArchiveSha256"] = hashlib.sha256((self.root / "image-notices.tar").read_bytes()).hexdigest()
        (self.root / "inventory.json").write_text(json.dumps(self.report), encoding="utf-8")

    def test_valid_archive_is_read_only_verified(self):
        self.assertEqual("incomplete", verify_inventory(self.root)["licenseReview"])
        self.assertEqual({"inventory.json", "image-notices.tar"}, {path.name for path in self.root.iterdir()})

    def test_changed_payload_is_rejected(self):
        path = self.root / "image-notices.tar"
        data = bytearray(path.read_bytes()); data[512] ^= 1; path.write_bytes(data)
        with self.assertRaises(ValueError): verify_inventory(self.root)

    def test_duplicate_is_rejected_even_with_rewritten_archive_digest(self):
        self.pack([*self.data, next(iter(self.data))])
        with self.assertRaises(ValueError): verify_inventory(self.root)

    def test_missing_notice_is_rejected_even_with_rewritten_archive_digest(self):
        self.pack([next(iter(self.data))])
        with self.assertRaises(ValueError): verify_inventory(self.root)

    def test_archive_link_is_rejected_without_following_it(self):
        self.pack(list(self.data), link=True)
        with self.assertRaises(ValueError): verify_inventory(self.root)


if __name__ == "__main__": unittest.main()
