# Isolated photo decoder process

Implemented locally on 2026-11-03, following the
[normalizer prototype](photo-normalization.md). No uploads, private API routes,
new image storage, migrations, deployment or paid service are enabled. The API and
reminder worker still do not reference the decoder project. No Docker socket or
Docker privileges are added to either application service.
The subsequent [private worker connection](photo-worker-connection.md) implements
and tests the API-side transport and trusted host gateway described as future
work below. It remains disabled and does not enable uploads.

## Boundary implemented

The photo project is now a standalone executable. Its `--normalize` command takes
one supported MIME argument and one bounded binary input on stdin, then exits
after one result. It runs only inside a restricted Linux container. A supervisor
starts a fresh decoder child with a fixed executable/argument list (no shell),
clears inherited environment variables, and communicates through bounded pipes.
No filenames, remote URLs, credentials or plant/account identifiers enter this
protocol. No original or result file is written to a host directory.

The child decodes and normalizes using the existing image rules. A private binary
response contains a versioned magic value, dimensions, lengths, JPEG and thumbnail.
The supervisor checks framing, dimensions, byte caps and JPEG markers before
accepting it; it does not decode untrusted images in the parent. A successful
child exit and empty stderr are required. Child errors cross the boundary only
through an explicit safe-code allowlist. Other stderr/native messages, malformed
responses, crashes and excessive output produce a generic failure code.

Input is at most 8 MiB. Standard output is at most 20 header bytes plus 512 KiB
image and 64 KiB thumbnail; stderr is at most 1 KiB and is never logged or returned
verbatim. The caller must check exit status and complete framing, not accept a
partial stdout stream. CLI intake, child processing/exchange, and final stdout
delivery each have a ten-second deadline. Timeout/cancellation kills the child
process tree and waits for exit before releasing admission. A cleanup failure
poisons admission instead of starting another child. The one-shot container exits
after the job; it is not a reusable multi-tenant decoder service.

## Linux restrictions

`deploy/Dockerfile.photo-processing` produces a separate runtime image with a
non-root app user and the decoder's bundled notices. No port is exposed. Its
startup guard fails closed unless it can verify all of these locally:

- cgroup v2 memory maximum between 64 and 384 MiB, with no swap allowance;
- CPU quota of at most one CPU and PID limit at most 64;
- non-root real/effective/saved/filesystem identity;
- no effective or bounding capabilities, no-new-privileges and seccomp filtering;
- a read-only root mount and only the loopback network interface.

The native policy permits only JPEG/PNG/WebP coders, denies all delegates,
filters and image-file paths, and prohibits disk/mapped pixel caches. Fresh image
creation now uses direct pixels rather than the disabled XC pseudo-coder.
Policy configuration is loaded from the immutable image, not created in temp.
CLR diagnostics are disabled; managed heap limits supplement the kernel limits.
Neither library settings nor the CLR heap limit substitute for the cgroup cap.
Memory/CPU limits cover the supervisor and its child together.

The Docker host must provide these controls; cgroup v1 or unsupported enforcement
is rejected rather than silently running unrestricted. The native decoder is
still complex untrusted-input code: these layers reduce its access and resource
impact, not prove immunity to vulnerabilities. No privileged container, host
network, bind-mounted uploads, database connection, credentials or external
storage service is needed for this local acceptance.

## Focused acceptance and repeat commands

The normalizer plus supervisor tests passed on Linux: **29 cases**, including
fresh process IDs, successful reaping, child crash, invalid framing/lengths,
unbounded stdout/stderr, sanitized failures, stripped parent environment,
start failure, stalled child input, timeout, cancellation, immediate busy
rejection and subsequent admission reuse. Earlier Windows execution passed the
then-current 28 cases; Linux acceptance includes the additional start-failure case.
These are synthetic fixtures. The fixture executable is a test dependency only;
it is not copied into the production photo image.

```powershell
docker build -f deploy/Dockerfile.photo-processing --target focused-tests `
  -t plantcare-photo-processing-tests:local .
docker build -f deploy/Dockerfile.photo-processing --target runtime `
  -t plantcare-photo-processing:local .
python scripts/Test-PhotoProcessingSandbox.py
```

The second acceptance script creates a synthetic PNG in memory, exercises the
actual restricted Linux executable and its normalized JPEG, validates the bounded
binary output, verifies sanitized invalid-image errors, and removes one sandbox
control at a time to ensure startup refuses it. Each test container has a unique
fixed-prefix name, no mounted data and no Docker log recording. The script removes
only those disposable containers, including after a harness timeout. Container
startup has a separate harness deadline; it is not the decoder's deadline.

An initial capability test exposed a missing bounding-set check; it was added.
One acceptance attempt hit a Docker-start timeout while another large local test
image was unpacking; sequential rerun passed. No decoder deadline or production
health threshold was relaxed. Keep these image-build and acceptance steps
sequential on the development machine.

The full CI workflow now includes standalone sandbox-image acceptance after the
backend tests. Catalog-only scope still skips it. This workflow addition selects
full validation at the next committed checkpoint; no scope selector was weakened.
No pipeline was triggered for this local increment. No full application suite,
staging rollout, SSH operation, email, push or backup/restore was performed.

References for enforcement semantics: [Docker resource constraints](https://docs.docker.com/engine/containers/resource_constraints/)
and [ImageMagick security policy](https://imagemagick.org/security-policy/).

## Remaining before uploads

This completes the standalone Linux process-boundary slice, not an upload
feature. Next, define and test how the authenticated API hands a bounded request
to the isolated worker **without** giving the API Docker access, credentials to
the decoder, or losing per-job isolation. The current CLI is an operator/test
interface, not a public endpoint or a ready-made API transport. Do not reference
the native decoder executable directly from the API; extract pure protocol
contracts separately if needed. A persistent broker requires a separate lifecycle
design rather than assuming the one-shot container's cleanup properties persist.

Then implement private SQL journal storage, quotas/concurrency, owner and CSRF
checks, export/deletion/erasure recovery and the owned-plant UI. Before activation,
run full exact-candidate CI and verify resource/capacity behavior on the actual
VPS, migrated backups and restore, and the existing account-isolation/privacy
gates. Local Docker acceptance does not certify the VPS configuration. Uploads
stay disabled until those gates pass.
