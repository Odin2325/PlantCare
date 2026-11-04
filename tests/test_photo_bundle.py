import json
import tempfile
import unittest
from pathlib import Path
from scripts.photo_bundle import FILES, NOTICES, ROOT, digest, verify


class BundleIntegrityTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="plantcare-photo-bundle-test-")
        self.path = Path(self.temp.name)
        for name in FILES:
            file = self.path / name
            file.parent.mkdir(exist_ok=True)
            file.write_bytes(b"synthetic public artifact\n")
        for name in NOTICES:
            (self.path / "notices" / name).write_bytes(b"synthetic attribution fixture\n" * 10)
        self.image = "sha256:" + "a" * 64
        unit = (ROOT / "deploy/plantcare-photo-gateway.service.example").read_text(encoding="utf-8")
        (self.path / "plantcare-photo-gateway.service.example").write_text(
            unit.replace("sha256:REPLACE_WITH_ACCEPTED_LOCAL_IMAGE_ID", self.image), encoding="utf-8")
        (self.path / "compose.photo-processing.yaml.example").write_bytes(
            (ROOT / "deploy/compose.photo-processing.yaml.example").read_bytes())
        self.manifest = {"format": 1, "candidate": "b" * 40, "imageId": self.image,
                         "platform": "linux/amd64", "activation": "disabled", "licenseReview": "incomplete"}
        self.save(rehash=True)

    def tearDown(self):
        self.temp.cleanup()

    def save(self, rehash=False):
        if rehash:
            self.manifest["files"] = {name: digest(self.path / name) for name in FILES}
        (self.path / "manifest.json").write_text(json.dumps(self.manifest), encoding="utf-8")

    def test_untouched_preparation_bundle_is_read_only_to_verifier(self):
        before = {p.relative_to(self.path).as_posix(): digest(p) for p in self.path.rglob("*") if p.is_file()}
        self.assertEqual(verify(self.path)["imageId"], self.image)
        self.assertEqual(before, {p.relative_to(self.path).as_posix(): digest(p) for p in self.path.rglob("*") if p.is_file()})

    def test_tampered_gateway_is_rejected(self):
        (self.path / "photo_gateway.py").write_text("changed", encoding="utf-8")
        with self.assertRaises(ValueError): verify(self.path)

    def test_extra_private_file_is_rejected(self):
        (self.path / ".env").write_text("SYNTHETIC=fixture", encoding="utf-8")
        with self.assertRaises(ValueError): verify(self.path)

    def test_missing_image_is_rejected(self):
        (self.path / "decoder-image.tar").unlink()
        with self.assertRaises(ValueError): verify(self.path)

    def test_incompatible_or_enabled_manifests_are_rejected(self):
        for key, value in (("platform", "linux/arm64"), ("activation", "enabled"),
                           ("candidate", "HEAD"), ("imageId", "latest"), ("licenseReview", "complete")):
            with self.subTest(key=key):
                previous = self.manifest[key]; self.manifest[key] = value; self.save()
                with self.assertRaises(ValueError): verify(self.path)
                self.manifest[key] = previous

    def test_manifest_paths_cannot_escape(self):
        self.manifest["files"]["../outside"] = "a" * 64; self.save()
        with self.assertRaises(ValueError): verify(self.path)

    def test_image_binding_cannot_be_changed_by_rehashing_unit(self):
        unit = self.path / "plantcare-photo-gateway.service.example"
        unit.write_text(unit.read_text(encoding="utf-8").replace(self.image, "sha256:" + "c" * 64), encoding="utf-8")
        self.save(rehash=True)
        with self.assertRaises(ValueError): verify(self.path)

    def test_activation_override_is_rejected_even_with_new_hashes(self):
        file = self.path / "compose.photo-processing.yaml.example"
        file.write_text(file.read_text(encoding="utf-8").replace('UploadsEnabled: "false"', 'UploadsEnabled: "true"'), encoding="utf-8")
        self.save(rehash=True)
        with self.assertRaises(ValueError): verify(self.path)

    def test_missing_attribution_is_rejected(self):
        (self.path / "notices" / NOTICES[0]).write_bytes(b"")
        self.save(rehash=True)
        with self.assertRaises(ValueError): verify(self.path)

    def test_malformed_manifest_structure_is_rejected(self):
        (self.path / "manifest.json").write_text("[]", encoding="utf-8")
        with self.assertRaises(ValueError): verify(self.path)


if __name__ == "__main__":
    unittest.main()
