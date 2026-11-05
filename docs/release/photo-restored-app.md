# Restored photo app and erasure replay

Local preparation, 2026-11-05. The opt-in drill extends the generated
[encrypted recovery sample](photo-encrypted-recovery.md):

```powershell
python scripts/Test-PhotoHttp.py --capacity --budget --large-account --encrypted-recovery --restored-app --api <local-api-image> --bundle <verified-bundle-directory>
```

It uses only generated accounts/data and the fresh disposable repository. Both
source and recovered services are on the same internal Docker network, with no
published ports. No normal local/staging database, backup repository, credential,
erasure ledger or schedule is read. External email/push/calendar sync remain off.

## Gates

After SQL/key decryption, CHECKDB and media-hash validation, the coordinator starts
a separate recovered API/proxy against the additional restored database. Its
connection uses the restricted API login and passes the existing runtime-access
check. It has no Docker/decoder socket mount. Photo upload flags stay off in the
recovered API; reads/export/deletion remain available. No recovered worker starts.

The original browser's generated cookies remain in memory, with only their host
mapped to the new internal HTTPS origin. An API using a separate empty key-ring
volume must reject that authenticated session. The coordinator then stops it,
validates the decrypted tar members, seeds a different private key volume and
recreates the API with those restored keys. Every key file hash must match the
capture, and the copied session must now authenticate the same account.

The recovered phone-sized browser opens the photo journal, decodes thumbnails
and confirms upload controls are absent. It reads all restored JPEGs/thumbnails
through authenticated HTTP and compares their hashes. A different restored
account cannot list/read/delete the owner's media. Logout, fresh password login,
password-verified no-store ZIP export and account deletion must succeed. The ZIP
is independently checked for filenames, counts and every image hash. SQL confirms
the recovered owner's identity, plants and media are gone, its erasure marker
exists, and the other account survives. The source fixture must remain unchanged.

The recovered API/proxy stop before exporting that recovered database's fresh
erasure ledger. Only the additional restored database is dropped/reloaded from
the older decrypted snapshot. The production erasure-apply command runs against
that database before API restart. SQL and a restarted HTTPS browser must confirm
the deleted account/media remain absent, old cookies/password cannot authenticate
it, and the other account still works. No recovered service starts before this
reconciliation. The source fixture later completes its own quota reuse/account
erasure checks independently.

All new API/proxy/key/admin services and volumes belong to the same UUID project;
its existing cleanup removes them alongside the repository and decrypted files.
Fixed phase labels and a safe acceptance summary are recorded in
`.operations-local/photo-restored-app.json` only as passed when the complete parent
lifecycle and project cleanup pass. Cookies/passwords/keys/ledger contents and raw
tool output are not reported.

## Local acceptance, 2026-11-05

The focused restored-app run passed using the existing accepted API/decoder
images and a new generated repository. One decrypted key file matched its
capture. The empty-key-ring control refused the session; the restored key ring
accepted it. The phone-width journal rendered its 20-photo first plant with
decoded thumbnails and no upload input. Authenticated HTTP reads verified all
38 full images and thumbnails, and the recovered ZIP passed independent checks.

Owner isolation, logout/fresh login, deletion of the recovered account/media and
the erasure marker all passed. After a second restore from the older snapshot,
the current ledger removed that owner/media before API restart. Old cookies and
password were rejected; the other account remained usable. SQL confirmed the
source fixture was unchanged during this work.

The source's subsequent quota reuse, account erasure and media cleanup also
passed. The restored services/database, generated repository/files and entire
UUID project were cleaned up, with no decoder job remaining. Python/JavaScript
syntax and CLI prerequisite checks passed. No push, CI or deployment ran.

## Scope

This accepts a real recovered app/key ring and erased-account replay using a
generated repeated-image dataset. It shares the disposable SQL server and its
existing server logins with the source; it is not fresh-master login remapping,
an independent replacement host or a production recovery claim. The extra API
and proxy are test infrastructure, not a simultaneous VPS budget certification.
Recovered worker behavior, diverse image sets, maximum count scenarios,
external-drive/network transfer and actual VPS/systemd/device acceptance remain
separate gates. Normal upload flags, data, repositories and schedules stay intact.
