"""Input boundaries and near-quota export in the UUID-owned HTTP test project."""
import hashlib
import json
import re
import struct
import time
import uuid
import zipfile
import zlib

# Match the application's indexed-table write session, rather than sqlcmd's
# default QUOTED_IDENTIFIER OFF. Cascading fixture deletion can reach filtered
# indexes even when the seeded photo tables have no filtered index themselves.
SQL_SESSION = """SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF; SET XACT_ABORT ON;
"""


def padded_png(data, target):
    # A private ancillary chunk before IEND: valid container padding, not a
    # fabricated camera file or metadata retained in normalized output.
    if not data.endswith(b"\0\0\0\0IEND\xaeB`\x82") or target < len(data) + 12:
        raise ValueError("Unsupported generated PNG padding.")
    payload = b"x" * (target - len(data) - 12)
    kind = b"pcAr"
    chunk = struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload) & 0xffffffff)
    return data[:-12] + chunk + data[-12:]


def large_checkpoint(sql, docker, project, scratch, browser, progress):
    if not re.fullmatch(r"plantcare-photo-http-[a-f0-9]{32}", project):
        raise ValueError("Large-account check requires its disposable project.")
    output = scratch / "output"

    def wait(name):
        deadline = time.monotonic() + 180
        while not (output / name).exists():
            if browser.poll() is not None or time.monotonic() > deadline:
                path = output / "large-failure.json"
                if path.exists():
                    detail = json.loads(path.read_text(encoding="utf-8"))
                    if re.fullmatch(r"[a-z-]{1,80}", detail.get("step", "")):
                        progress["browserPhase"] = detail["step"]
                    if isinstance(detail.get("responseStatus"), int) and 100 <= detail["responseStatus"] <= 599:
                        progress["responseStatus"] = detail["responseStatus"]
                raise RuntimeError("Large-account browser checkpoint did not complete.")
            time.sleep(.1)
        return json.loads((output / name).read_text(encoding="utf-8"))

    progress["phase"] = "input-boundaries"
    ready = wait("large-ready.json")
    source = str(uuid.UUID(ready["photoId"]))
    second = str(uuid.uuid4())
    progress["phase"] = "seed-genuine-media-copies"
    sample = json.loads(sql(SQL_SESSION + """
DECLARE @source uniqueidentifier='""" + source + """',@second uniqueidentifier='""" + second + """';
IF (SELECT COUNT(*) FROM PlantPhotoEntries)<>1 THROW 51000,'Unexpected synthetic photo state',1;
DECLARE @plant uniqueidentifier,@owner uniqueidentifier,@species uniqueidentifier,@pair bigint,@count int;
SELECT @plant=e.UserPlantId,@owner=p.UserId,@species=p.PlantSpeciesId,
 @pair=CONVERT(bigint,DATALENGTH(m.Image))+DATALENGTH(m.Thumbnail)
FROM PlantPhotoEntries e JOIN PlantPhotoMedia m ON m.Id=e.Id JOIN UserPlants p ON p.Id=e.UserPlantId WHERE e.Id=@source;
SET @count=20971520/@pair;
IF @count<21 OR @count>40 OR @pair*@count<20552089 THROW 51000,'Fixture does not approach account byte quota',1;
BEGIN TRANSACTION;
UPDATE PhotoJournalQuota SET Revision=Revision+1 WHERE Id=1;
INSERT UserPlants(Id,UserId,PlantSpeciesId,Nickname,IsActive,CreatedAtUtc)
 VALUES(@second,@owner,@species,N'Large export fixture',1,SYSDATETIMEOFFSET());
DECLARE @i int=1;
WHILE @i<@count
BEGIN
 DECLARE @id uniqueidentifier=NEWID();
 INSERT PlantPhotoEntries(Id,UserPlantId,UploadRequestId,UploadFingerprint,PhotographedOn,Notes,CreatedAtUtc,Version)
 SELECT @id,CASE WHEN @i<20 THEN @plant ELSE @second END,NEWID(),UploadFingerprint,PhotographedOn,Notes,CreatedAtUtc,NEWID()
 FROM PlantPhotoEntries WHERE Id=@source;
 INSERT PlantPhotoMedia(Id,Image,Thumbnail,Width,Height,ImageSha256,ThumbnailSha256)
 SELECT @id,Image,Thumbnail,Width,Height,ImageSha256,ThumbnailSha256 FROM PlantPhotoMedia WHERE Id=@source;
 SET @i+=1;
END;
COMMIT;
SELECT @count AS entries,@pair*@count AS storedBytes,@pair AS pairBytes,
 ImageSha256 AS imageSha256,DATALENGTH(Image) AS imageBytes FROM PlantPhotoMedia WHERE Id=@source
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
""").stdout.decode().strip())
    (output / "large-seeded.json").write_text(json.dumps({"plantId": second, "entries": sample["entries"]}), encoding="utf-8")
    progress["phase"] = "quota-export-and-rejection"
    done = wait("large-done.json")
    progress["phase"] = "independent-zip-verification"
    with zipfile.ZipFile(output / "large-photos.zip") as archive:
        manifest = json.loads(archive.read("manifest.json"))
        items = manifest["images"]
        if len(items) != sample["entries"]:
            raise ValueError("Large export omitted normalized media.")
        names = set()
        for item in items:
            name = item["file"]
            if not re.fullmatch(r"photos/[a-f0-9]{32}\.jpg", name) or name in names:
                raise ValueError("Large export has unexpected or repeated filenames.")
            names.add(name)
            data = archive.read(name)
            if (len(data) != sample["imageBytes"] or not data.startswith(b"\xff\xd8") or not data.endswith(b"\xff\xd9")
                    or hashlib.sha256(data).hexdigest() != sample["imageSha256"] or item["sha256"].lower() != sample["imageSha256"]):
                raise ValueError("Large export normalized image/hash mismatch.")
        if set(archive.namelist()) != names | {"manifest.json"}:
            raise ValueError("Large export includes unexpected files.")
    resources = {}
    for service in ("api", "database", "gateway", "proxy"):
        container = project + "-" + service + "-1"
        lines = docker("exec", container, "sh", "-c", "cat /sys/fs/cgroup/memory.peak /sys/fs/cgroup/memory.max /sys/fs/cgroup/memory.events").stdout.decode().splitlines()
        peak, limit = map(int, lines[:2])
        events = dict(line.split() for line in lines[2:])
        state = json.loads(docker("inspect", "--format", "{{json .State}}", container).stdout)
        restarts = int(docker("inspect", "--format", "{{.RestartCount}}", container).stdout)
        if (peak <= 0 or peak >= limit or not state["Running"] or state["OOMKilled"] or restarts
                or any(int(events.get(key, -1)) != 0 for key in ("max", "oom", "oom_kill"))):
            raise ValueError("Large workload exceeded its memory/resource contract.")
        resources[service] = {"peakBytes": peak, "limitBytes": limit, "limitHits": 0, "oomKills": 0, "restarts": 0}
    progress["phase"] = "release-only-synthetic-copies"
    progress["services"] = resources
    cleanup = sql(SQL_SESSION + """
BEGIN TRANSACTION;
UPDATE PhotoJournalQuota SET Revision=Revision+1 WHERE Id=1;
DELETE e FROM PlantPhotoEntries e JOIN UserPlants p ON p.Id=e.UserPlantId
 WHERE p.UserId=(SELECT p2.UserId FROM UserPlants p2 JOIN PlantPhotoEntries e2 ON e2.UserPlantId=p2.Id WHERE e2.Id='""" + source + """')
 AND e.Id<>'""" + source + """';
DELETE UserPlants WHERE Id='""" + second + """';
COMMIT;
IF (SELECT COUNT(*) FROM PlantPhotoEntries)<>1 THROW 51000,'Synthetic quota cleanup failed',1;
""", check=False)
    if cleanup.returncode:
        codes = re.findall(rb"Msg (\d+), Level", cleanup.stdout + cleanup.stderr)
        progress["sqlErrorNumbers"] = sorted({int(code) for code in codes})
        raise RuntimeError("Synthetic quota cleanup did not complete.")
    (output / "large-continue.json").write_text('{"continue":true}', encoding="utf-8")
    progress["phase"] = "accepted"
    return {"entries": sample["entries"], "storedBytes": sample["storedBytes"], "accountLimitBytes": 20971520,
            "zipBytes": (output / "large-photos.zip").stat().st_size, "zipHashVerifiedImages": sample["entries"],
            "services": resources, "browser": done}
