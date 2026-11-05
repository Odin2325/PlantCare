"""Near-quota generated SQL/key capture through a new disposable restic repository."""
import hashlib
import json
import os
import re
import secrets
import subprocess
import time


def encrypted_recovery(sql, docker, project, scratch, tool, sample, images, decoder, progress):
    if not re.fullmatch(r"plantcare-photo-http-[a-f0-9]{32}", project):
        raise ValueError("Encrypted rehearsal requires its disposable project.")
    root = scratch / "encrypted-fixture"
    root.mkdir(mode=0o700)
    if root.resolve().parent != scratch.resolve() or root.is_symlink():
        raise ValueError("Unexpected encrypted fixture location.")
    # Scope the ACL change to this newly generated fixture. Do not read saved
    # backup configuration, DPAPI keys, passwords or repository environment.
    env = {key: value for key, value in os.environ.items() if not key.upper().startswith("RESTIC_")}
    env["PLANTCARE_TEST_SCRATCH"] = str(root)
    secured = subprocess.run(["pwsh", "-NoProfile", "-Command",
        "$sid=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value; "
        "& icacls.exe $env:PLANTCARE_TEST_SCRATCH /inheritance:r /grant:r ('*'+$sid+':(OI)(CI)F') "
        "'*S-1-5-18:(OI)(CI)F' '*S-1-5-32-544:(OI)(CI)F'; exit $LASTEXITCODE"],
        env=env, capture_output=True, timeout=30)
    if secured.returncode:
        raise RuntimeError("Cannot secure the private generated recovery fixture.")
    env.pop("PLANTCARE_TEST_SCRATCH")
    capture, repository, restored, cache = (root / name for name in ("capture", "repository", "restored", "cache"))
    for path in (capture, restored, cache): path.mkdir()
    env["RESTIC_PASSWORD"] = secrets.token_hex(32)
    env["GOMAXPROCS"] = "2"
    env["GOMEMLIMIT"] = "256MiB"  # Go runtime target, not an OS memory guarantee.
    database = project + "-database-1"
    backup = "/var/opt/mssql/data/photo-encrypted-source.bak"
    recovery_file = "/var/opt/mssql/data/photo-encrypted-returned.bak"

    def restic(*args, password=None, check=True):
        child = dict(env)
        if password is not None: child["RESTIC_PASSWORD"] = password
        result = subprocess.run([str(tool), "--repo", str(repository), "--cache-dir", str(cache), *args],
                                cwd=capture, env=child, capture_output=True, timeout=180)
        if check and result.returncode:
            raise RuntimeError("Disposable encrypted repository operation failed; output withheld.")
        return result

    def digest(path):
        with path.open("rb") as stream: return hashlib.file_digest(stream, "sha256").hexdigest()

    def size(path):
        return sum(item.stat().st_size for item in path.rglob("*") if item.is_file())

    times = {}
    progress["phase"] = "capture-generated-sql-keys"
    start = time.perf_counter()
    sql("BACKUP DATABASE [PlantCare] TO DISK=N'" + backup + "' WITH COPY_ONLY,CHECKSUM,INIT; "
        "RESTORE VERIFYONLY FROM DISK=N'" + backup + "' WITH CHECKSUM;")
    times["sqlBackupAndVerifySeconds"] = round(time.perf_counter() - start, 3)
    start = time.perf_counter()
    docker("cp", database + ":" + backup, str(capture / "database.bak"))
    # Copy only the generated app's data-protection directory. It is private
    # payload, never decoded/printed. No normal local/staging volume is mounted.
    keys = docker("exec", project + "-api-1", "tar", "-cf", "-", "-C", "/var/lib/plantcare/keys", ".").stdout
    if not keys: raise ValueError("Generated key capture is empty.")
    (capture / "keys.tar").write_bytes(keys)
    manifest = {"format": 1, "images": images, "decoderImage": decoder,
                "files": {name: digest(capture / name) for name in ("database.bak", "keys.tar")}}
    (capture / "manifest.json").write_text(json.dumps(manifest, sort_keys=True), encoding="utf-8")
    times["copySqlKeysAndManifestSeconds"] = round(time.perf_counter() - start, 3)
    captured_bytes = size(capture)
    progress["phase"] = "initialize-and-encrypt"
    start = time.perf_counter(); restic("init")
    times["repositoryInitSeconds"] = round(time.perf_counter() - start, 3)
    initial_bytes = size(repository)
    start = time.perf_counter()
    result = restic("backup", ".", "--host", "plantcare-generated-rehearsal", "--json")
    summaries = [row for line in result.stdout.decode().splitlines() if (row := json.loads(line)).get("message_type") == "summary"]
    if len(summaries) != 1 or not re.fullmatch(r"[a-f0-9]{64}", summaries[0].get("snapshot_id", "")):
        raise ValueError("Disposable encrypted snapshot was not confirmed.")
    snapshot = summaries[0]["snapshot_id"]
    times["encryptedBackupSeconds"] = round(time.perf_counter() - start, 3)
    repository_bytes = size(repository)
    progress["phase"] = "read-data-check-and-wrong-key-refusal"
    start = time.perf_counter(); restic("check", "--read-data")
    times["repositoryReadDataCheckSeconds"] = round(time.perf_counter() - start, 3)
    if restic("snapshots", "--json", password=secrets.token_hex(32), check=False).returncode == 0:
        raise ValueError("Wrong key unexpectedly accessed the fixture repository.")
    progress["phase"] = "decrypt-and-verify-payload"
    start = time.perf_counter(); restic("restore", snapshot, "--target", str(restored), "--verify")
    times["decryptAndVerifyPayloadSeconds"] = round(time.perf_counter() - start, 3)
    # restic preserves the source absolute path beneath its target. Require one
    # complete payload, no links, and independently compare every restored file.
    files = list(restored.rglob("*"))
    if any(path.is_symlink() for path in files):
        raise ValueError("Unexpected link in restored fixture.")
    payloads = [path.parent for path in files if path.is_file() and path.name == "manifest.json"]
    if len(payloads) != 1: raise ValueError("Restored fixture manifest is missing or ambiguous.")
    payload = payloads[0]
    expected = {"database.bak", "keys.tar", "manifest.json"}
    if {path for path in files if path.is_file()} != {payload / name for name in expected}:
        raise ValueError("Restored fixture has unexpected files.")
    for name in expected:
        if digest(payload / name) != digest(capture / name):
            raise ValueError("Decrypted capture hash differs from source.")
    progress["phase"] = "restore-sql-and-check-every-photo"
    start = time.perf_counter()
    docker("cp", str(payload / "database.bak"), database + ":" + recovery_file)
    sql("""
IF DB_ID(N'PlantCareEncryptedPhotoRestore') IS NOT NULL THROW 51000,'Restore target is not empty',1;
IF EXISTS(SELECT 1 FROM sys.database_files WHERE name NOT IN(N'PlantCare',N'PlantCare_log'))
 THROW 51000,'Unexpected disposable logical file names',1;
RESTORE DATABASE [PlantCareEncryptedPhotoRestore] FROM DISK=N'""" + recovery_file + """'
 WITH CHECKSUM,RECOVERY,
 MOVE N'PlantCare' TO N'/var/opt/mssql/data/photo-encrypted-restored.mdf',
 MOVE N'PlantCare_log' TO N'/var/opt/mssql/data/photo-encrypted-restored.ldf';
DBCC CHECKDB ([PlantCareEncryptedPhotoRestore]) WITH NO_INFOMSGS;
""")
    try:
        count, total = int(sample["entries"]), int(sample["storedBytes"])
        if not 1 <= count <= 50 or not 1 <= total <= 20971520:
            raise ValueError("Unexpected generated photo totals.")
        sql(f"""
IF (SELECT COUNT(*) FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoEntries)<>{count}
 OR (SELECT COUNT(*) FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoMedia)<>{count}
 THROW 51000,'Restored photo counts differ',1;
IF (SELECT SUM(CONVERT(bigint,DATALENGTH(Image))+DATALENGTH(Thumbnail)) FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoMedia)<>{total}
 THROW 51000,'Restored media bytes differ',1;
IF EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoMedia
 WHERE LOWER(CONVERT(varchar(64),HASHBYTES('SHA2_256',Image),2))<>ImageSha256
 OR LOWER(CONVERT(varchar(64),HASHBYTES('SHA2_256',Thumbnail),2))<>ThumbnailSha256)
 THROW 51000,'Restored image or thumbnail hash differs',1;
IF EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoEntries WHERE Notes<>N'Generated second leaf' OR Notes IS NULL)
 THROW 51000,'Restored photo notes differ',1;
""")
    finally:
        sql("DROP DATABASE [PlantCareEncryptedPhotoRestore];")
    times["sqlCopyRestoreCheckdbAndDropSeconds"] = round(time.perf_counter() - start, 3)
    # The original database/account stays alive for the existing real HTTP
    # quota-release/erasure checks. Do not start an app/worker on the restored DB.
    progress["phase"] = "accepted"
    return {"timings": times, "capturedBytes": captured_bytes,
            "sqlBackupBytes": (capture / "database.bak").stat().st_size, "keyArchiveBytes": len(keys),
            "repositoryInitialBytes": initial_bytes, "repositoryAfterBackupBytes": repository_bytes,
            "repositoryGrowthBytes": repository_bytes - initial_bytes,
            "photosRestored": count, "storedMediaBytes": total,
            "payloadHashesVerified": True, "allImageAndThumbnailHashesVerified": True,
            "checkdbPassed": True, "wrongPasswordRefused": True,
            "restoredDatabaseDropped": True, "existingRepositoriesAccessed": False}
