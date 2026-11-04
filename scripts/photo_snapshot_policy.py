"""SQL policy rehearsal helpers. Called only by the disposable HTTP test project."""
import json
import re
import time
import uuid

ACK = "EXEC sys.sp_set_session_context @key=N'PlantCareSnapshotOnlyApproval', @value=N'SNAPSHOT_ONLY_NO_POINT_IN_TIME';\n"


def prepare(sql, root):
    policy = (root / "deploy/sql/snapshot-only-policy.sql.example").read_text(encoding="utf-8")
    def refused(query, message):
        result = sql(query, check=False)
        if result.returncode == 0 or message.encode() not in result.stdout + result.stderr:
            raise ValueError("Snapshot policy guard did not reject the expected unsafe session.")
    refused(policy, "Explicit snapshot-only recovery approval is required.")
    refused(ACK + policy, "A fresh checksum database snapshot is required.")
    if sql("SELECT recovery_model_desc FROM sys.databases WHERE name=N'PlantCare';").stdout.strip() != b"FULL":
        raise ValueError("Refused policy changed the disposable initial recovery model.")
    sql("BACKUP DATABASE [PlantCare] TO DISK=N'/var/opt/mssql/data/policy-baseline.bak' WITH COPY_ONLY,CHECKSUM,INIT; "
        "RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/data/policy-baseline.bak' WITH CHECKSUM;")
    sql(ACK + policy)
    sql(ACK + policy)  # Reapplying an acknowledged policy is idempotent.
    if sql("SELECT recovery_model_desc FROM sys.databases WHERE name=N'PlantCare';").stdout.strip() != b"SIMPLE":
        raise ValueError("Disposable snapshot-only policy was not applied.")
    return policy


def checkpoint(sql, ready):
    # All values are synthetic but still parsed as data, never SQL fragments.
    if not isinstance(ready, dict) or not isinstance(ready.get("photoId"), str):
        raise ValueError("Unexpected synthetic snapshot metadata.")
    photo = str(uuid.UUID(ready["photoId"]))
    sha = ready.get("imageSha256")
    if not isinstance(sha, str) or not re.fullmatch(r"[a-f0-9]{64}", sha):
        raise ValueError("Unexpected normalized snapshot image hash.")
    churn = sql("""
CREATE TABLE dbo.PhotoPolicyChurn (Id int NOT NULL PRIMARY KEY, Payload varbinary(max) NOT NULL);
DECLARE @i int=0, @peak bigint=0;
WHILE @i < 12
BEGIN
 INSERT dbo.PhotoPolicyChurn VALUES (1,CONVERT(varbinary(max),REPLICATE(CONVERT(varchar(max),'x'),2097152)));
 SELECT @peak=CASE WHEN used_log_space_in_bytes>@peak THEN used_log_space_in_bytes ELSE @peak END FROM sys.dm_db_log_space_usage;
 DELETE dbo.PhotoPolicyChurn;
 CHECKPOINT;
 SET @i+=1;
END;
DROP TABLE dbo.PhotoPolicyChurn;
CHECKPOINT;
DECLARE @after bigint=(SELECT used_log_space_in_bytes FROM sys.dm_db_log_space_usage);
IF @after >= @peak THROW 51000,'Expected logical log reuse after checkpoint was not observed.',1;
SELECT @peak AS peakUsedLogBytes, @after AS afterCheckpointUsedLogBytes FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
""")
    # SQLCMD's default text width truncates JSON; the caller sets a wide result.
    log = json.loads(churn.stdout.decode().strip())
    start = time.perf_counter()
    sql("BACKUP DATABASE [PlantCare] TO DISK=N'/var/opt/mssql/data/photo-bearing.bak' WITH COPY_ONLY,CHECKSUM,INIT; "
        "RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/data/photo-bearing.bak' WITH CHECKSUM;")
    backup_seconds = time.perf_counter() - start
    start = time.perf_counter()
    sql("""
IF DB_ID(N'PlantCarePhotoPolicyRestore') IS NOT NULL THROW 51000,'Restore target is not empty.',1;
IF EXISTS(SELECT 1 FROM sys.database_files WHERE name NOT IN(N'PlantCare',N'PlantCare_log'))
 THROW 51000,'Unexpected disposable logical file names.',1;
RESTORE DATABASE [PlantCarePhotoPolicyRestore] FROM DISK=N'/var/opt/mssql/data/photo-bearing.bak'
 WITH CHECKSUM,RECOVERY,
 MOVE N'PlantCare' TO N'/var/opt/mssql/data/photo-policy-restored.mdf',
 MOVE N'PlantCare_log' TO N'/var/opt/mssql/data/photo-policy-restored.ldf';
DBCC CHECKDB ([PlantCarePhotoPolicyRestore]) WITH NO_INFOMSGS;
""")
    restore_seconds = time.perf_counter() - start
    try:
        sql(f"""
IF (SELECT recovery_model_desc FROM sys.databases WHERE name=N'PlantCarePhotoPolicyRestore')<>N'SIMPLE'
 THROW 51000,'Restored recovery model differs.',1;
IF (SELECT COUNT(*) FROM PlantCarePhotoPolicyRestore.dbo.PlantPhotoEntries)<>1
 OR (SELECT COUNT(*) FROM PlantCarePhotoPolicyRestore.dbo.PlantPhotoMedia)<>1
 THROW 51000,'Restored journal counts differ.',1;
IF NOT EXISTS(SELECT 1 FROM PlantCarePhotoPolicyRestore.dbo.PlantPhotoMedia
 WHERE Id=CONVERT(uniqueidentifier,'{photo}') AND ImageSha256='{sha}'
 AND LOWER(CONVERT(varchar(64),HASHBYTES('SHA2_256',Image),2))='{sha}'
 AND LOWER(CONVERT(varchar(64),HASHBYTES('SHA2_256',Thumbnail),2))=ThumbnailSha256)
 THROW 51000,'Restored normalized media hashes differ.',1;
IF NOT EXISTS(SELECT 1 FROM PlantCarePhotoPolicyRestore.dbo.PlantPhotoEntries
 WHERE Id=CONVERT(uniqueidentifier,'{photo}') AND Notes=N'Generated second leaf')
 THROW 51000,'Restored journal notes differ.',1;
""")
        size = sql("SELECT TOP(1) backup_size AS sqlBackupBytes FROM msdb.dbo.backupset "
                   "WHERE database_name=N'PlantCare' AND type='D' ORDER BY backup_set_id DESC FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;")
        return {**log, **json.loads(size.stdout.decode().strip()),
                "backupAndVerifyCommandSeconds": round(backup_seconds, 3),
                "restoreAndCheckdbCommandSeconds": round(restore_seconds, 3), "normalizedPhotoVerified": True}
    finally:
        # Fixed additional database exists only inside this UUID-owned SQL server.
        sql("DROP DATABASE [PlantCarePhotoPolicyRestore];")


def reject_log_chain_retirement(sql, policy):
    # Run after browser erasure, solely in the disposable SQL server. Establish a
    # real log chain to prove the policy refuses to retire it even with approval.
    sql("ALTER DATABASE [PlantCare] SET RECOVERY FULL; "
        "BACKUP DATABASE [PlantCare] TO DISK=N'/var/opt/mssql/data/policy-chain-base.bak' WITH CHECKSUM,INIT; "
        "BACKUP LOG [PlantCare] TO DISK=N'/var/opt/mssql/data/policy-chain-log.trn' WITH CHECKSUM,INIT;")
    result = sql(ACK + policy, check=False)
    if result.returncode == 0 or b"Existing log-backup history requires separate review." not in result.stdout + result.stderr:
        raise ValueError("Existing log-chain retirement was not refused.")
    if sql("SELECT recovery_model_desc FROM sys.databases WHERE name=N'PlantCare';").stdout.strip() != b"FULL":
        raise ValueError("Refused log-chain change altered the disposable database.")
