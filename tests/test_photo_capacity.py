import subprocess
import unittest
from unittest.mock import patch
from scripts.photo_capacity import measure


class CapacityAccountingTests(unittest.TestCase):
    def run_with(self, stderr, *, returncode=0, cleanup_failure=False):
        def fake(*args, **kwargs):
            if args[0] == "run":
                return subprocess.CompletedProcess(args, returncode, b"", stderr)
            if cleanup_failure:
                return subprocess.CompletedProcess(args, 1, b"", b"unavailable")
            return subprocess.CompletedProcess(args, 0, b"", b"")
        with patch("scripts.photo_capacity.docker", side_effect=fake):
            return measure("synthetic-image", b"synthetic-input")

    def test_missing_peak_must_not_be_reported_as_zero(self):
        with self.assertRaises(ValueError): self.run_with(b"no accounting")

    def test_over_limit_peak_is_rejected(self):
        with self.assertRaises(ValueError): self.run_with(b"\nPCMETRICS 0 500000000 1000 0\n")

    def test_oom_event_prevents_acceptance(self):
        with self.assertRaises(ValueError): self.run_with(b"\nPCMETRICS 0 1000000 1000 1\n")

    def test_unknown_native_error_is_not_reported(self):
        with self.assertRaises(ValueError):
            self.run_with(b"synthetic-untrusted-error\nPCMETRICS 2 1000000 1000 0\n", returncode=2)

    def test_expected_rejection_keeps_kernel_accounting(self):
        row = self.run_with(b"photo-dimensions-exceeded\nPCMETRICS 2 1000000 2000 0\n", returncode=2)
        self.assertEqual(row["code"], "photo-dimensions-exceeded")
        self.assertEqual(row["memoryPeakBytes"], 1000000)
        self.assertEqual(row["cpuSeconds"], .002)

    def test_failed_removal_and_failed_inspection_cannot_claim_cleanup(self):
        with self.assertRaises(RuntimeError): self.run_with(b"no accounting", cleanup_failure=True)
