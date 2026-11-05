# Large input and near-quota export preparation

Local generated-data check, 2026-11-05. Normal/staging uploads remain disabled.

```powershell
python scripts/Test-PhotoHttp.py --capacity --budget --large-account --api <local-api-image> --bundle <verified-bundle-directory>
```

This opt-in mode uses the disposable internal HTTPS/API/gateway/SQL stack, pinned
existing images and the preparation-only memory limits. It cannot combine with
the mixed-worker or snapshot-policy modes. No real account, saved environment,
encrypted repository or VPS is accessed. Normal authentication/upload limits stay
unchanged.

## Gates

- Send a valid PNG exactly 8 MiB through the authenticated multipart endpoint.
  Its generated private ancillary padding changes input size without changing
  image pixels. The normalized JPEG must match the unpadded sample's SHA256,
  demonstrating that this padding is not retained. It is not a camera fixture.
- Send a generated 5000 × 4000 image (20 MP); the returned JPEG must decode to
  1600 × 1280 and respect the output-size limit.
- Refuse an 8-MiB-plus-one-byte file and a 9-MiB-plus-one-byte request with 413,
  and a 21-MP image with the sanitized pixel-limit error. Rejections must leave
  the original photo count unchanged; successful temporary uploads are deleted.
- Pause the browser and copy its genuinely normalized JPEG/thumbnail within the
  UUID-owned SQL volume. Seed enough copies across two synthetic plants to use
  at least 98% of the account byte limit without exceeding either plant/count
  quota. This avoids dozens of duplicate HTTP decodes and does not bypass limits
  in a normal database. The repeated-media dataset tests bytes/export behavior,
  not photo variety, maximum entry count or independent camera files.
- Refuse the next full-sized photo through the real endpoint on a plant with
  spare entry slots. Its photo count must remain unchanged.
- Export the near-quota account with password verification and no-store headers.
  Independently verify every ZIP filename, JPEG length and SHA256 against the
  normalized SQL source, with no unexpected or duplicate archive entries.
- Check kernel peak/limit accounting, zero limit/OOM events and no restart for
  API, SQL, gateway and proxy. Release only seeded copies, then accept another
  real upload/delete to prove quota reuse. Continue the original browser account
  erasure, private ZIP verification, SQL media cleanup and project teardown.

The safe `.operations-local/photo-large-account.json` report is accepted only
when the entire lifecycle and UUID-project cleanup succeed. Failure diagnostics
contain fixed phase labels, HTTP status codes and SQL error numbers; no raw
browser/SQL logs, generated passwords or message contents are forwarded.

## Local acceptance, 2026-11-05

The focused run passed with the accepted mobile-checkpoint API image and the
protocol-1 preparation bundle from `755c477b4f6bf93d100f3610e430c6ba59a79c7f`.
Runtime image IDs are pinned in the safe report. Syntax checks also passed;
no full CI, push or deployment was run.

- Accepted exactly 8,388,608 input bytes and 20,000,000 pixels. Rejected the
  plus-one-byte file/body cases and 21,000,000 pixels as specified above.
- Seeded 38 entries containing 20,770,078 media bytes, about 99.04% of the
  20,971,520-byte account ceiling, across plants with 20 and 18 entries.
- The next full-sized upload to the second plant was refused with 409 and left
  its count unchanged. Releasing the copies allowed a subsequent real upload.
- The 19,837,170-byte ZIP passed independent filename/content checks and SHA256
  verification for all 38 JPEGs. This was a near-byte-quota sample, not an exact
  maximum for every possible image/thumbnail mix.
- Original browser deletion/account erasure, private ZIP verification, SQL media
  erasure and UUID project/volume cleanup passed. No decoder job remained.

| Service | Kernel peak MiB | Test memory limit MiB |
| --- | ---: | ---: |
| API | 222.6 | 384 |
| SQL | 688.6 | 2048 |
| Gateway | 43.2 | 128 |
| Proxy | 34.8 | 128 |

All four services reported zero kernel memory-limit/OOM events and zero restarts.
The fixture cleanup initially exposed sqlcmd's default session settings on a
cascading indexed-table write. The coordinator now sets the required indexed
write options and XACT_ABORT for its own seed/cleanup transactions. No schema or
application settings changed. The rerun passed the entire lifecycle.

## Remaining limits

This approaches the account byte ceiling for one real normalized image size; it
does not prove the largest theoretical ZIP for every thumbnail/image mix or a
50-entry account. The test browser is outside the proposed production budget.
Transient decoder memory is covered separately by decoder capacity tests, not
the long-lived service table. Individual service peaks are not a simultaneous
host peak. Multi-user/service saturation, camera formats/metadata variety,
encrypted backup/restore sizing and actual VPS/systemd/device acceptance remain
separate gates. No live resource or recovery settings change from this check.
