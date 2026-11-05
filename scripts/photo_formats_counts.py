"""Count-bound fixture coordinator; every SQL write stays in the UUID test DB."""
import hashlib
import json
import re
import time
import uuid
import zipfile
import struct
import zlib
from photo_large_workload import SQL_SESSION


def animation_probes(directory):
    # Framed animation headers exercise early policy rejection, not native
    # animated-frame decoding. These are not claimed as complete animations.
    png = (directory / "transparent.png").read_bytes()
    data = struct.pack(">II", 2, 0); kind = b"acTL"
    chunk = struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xffffffff)
    (directory / "animation-header.png").write_bytes(png[:33] + chunk + png[33:])
    (directory / "animation-header.webp").write_bytes(b"RIFF" + struct.pack("<I", 14) + b"WEBPVP8X" + struct.pack("<I", 1) + b"\x02\0")


def count_checkpoint(sql, project, scratch, browser, progress):
    if not re.fullmatch(r"plantcare-photo-http-[a-f0-9]{32}", project):
        raise ValueError("Count acceptance requires its disposable project.")
    output = scratch / "output"

    def wait(name):
        deadline = time.monotonic() + 180
        while not (output / name).exists():
            if browser.poll() is not None or time.monotonic() > deadline:
                path = output / "formats-failure.json"
                if path.exists():
                    detail = json.loads(path.read_text(encoding="utf-8"))
                    if re.fullmatch(r"[a-z-]{1,80}", detail.get("step", "")): progress["browserPhase"] = detail["step"]
                    if isinstance(detail.get("status"), int) and 100 <= detail["status"] <= 599:
                        progress["httpStatus"] = detail["status"]
                raise RuntimeError("Format/count browser checkpoint failed.")
            time.sleep(.1)
        return json.loads((output / name).read_text(encoding="utf-8"))

    def signal(name, data):
        temporary = output / (name + ".tmp")
        temporary.write_text(json.dumps(data), encoding="utf-8")
        temporary.replace(output / (name + ".json"))

    progress["phase"] = "format-inputs"
    ready = wait("formats-ready.json")
    white, original = (str(uuid.UUID(ready[key])) for key in ("whiteId", "originalId"))
    second, third = str(uuid.uuid4()), str(uuid.uuid4())
    prefix = SQL_SESSION + f"""
DECLARE @white uniqueidentifier='{white}',@original uniqueidentifier='{original}',
 @second uniqueidentifier='{second}',@third uniqueidentifier='{third}',@plant uniqueidentifier,
 @owner uniqueidentifier,@species uniqueidentifier;
SELECT @plant=e.UserPlantId,@owner=p.UserId,@species=p.PlantSpeciesId
 FROM PlantPhotoEntries e JOIN UserPlants p ON p.Id=e.UserPlantId WHERE e.Id=@white;
IF @owner IS NULL OR NOT EXISTS(SELECT 1 FROM PlantPhotoEntries WHERE Id=@original AND UserPlantId=@plant)
 THROW 51000,'Synthetic source photos missing',1;
"""

    def clone_loop(target, number):
        return f"""
DECLARE @i int=0;
WHILE @i<{number}
BEGIN
 DECLARE @id uniqueidentifier=NEWID();
 INSERT PlantPhotoEntries(Id,UserPlantId,UploadRequestId,UploadFingerprint,PhotographedOn,Notes,CreatedAtUtc,Version)
 SELECT @id,{target},NEWID(),UploadFingerprint,PhotographedOn,Notes,CreatedAtUtc,NEWID() FROM PlantPhotoEntries WHERE Id=@white;
 INSERT PlantPhotoMedia(Id,Image,Thumbnail,Width,Height,ImageSha256,ThumbnailSha256)
 SELECT @id,Image,Thumbnail,Width,Height,ImageSha256,ThumbnailSha256 FROM PlantPhotoMedia WHERE Id=@white;
 SET @i+=1;
END;
"""

    progress["phase"] = "per-plant-count"
    sql(prefix + """
IF (SELECT COUNT(*) FROM PlantPhotoEntries)<>2 THROW 51000,'Unexpected initial fixture count',1;
BEGIN TRANSACTION;
UPDATE PhotoJournalQuota SET Revision=Revision+1 WHERE Id=1;
INSERT UserPlants(Id,UserId,PlantSpeciesId,Nickname,IsActive,CreatedAtUtc)
 VALUES(@second,@owner,@species,N'Count fixture two',1,SYSDATETIMEOFFSET()),(@third,@owner,@species,N'Count fixture three',1,SYSDATETIMEOFFSET());
""" + clone_loop("@plant", 18) + "COMMIT;")
    signal("formats-plant-ready", {})
    wait("formats-plant-done.json")
    progress["phase"] = "account-count"
    # Separate batches avoid declaring the same loop variable twice in SQL.
    sql(prefix + "BEGIN TRANSACTION; UPDATE PhotoJournalQuota SET Revision=Revision+1 WHERE Id=1;"
        + clone_loop("@second", 20) + "COMMIT;")
    sql(prefix + "BEGIN TRANSACTION; UPDATE PhotoJournalQuota SET Revision=Revision+1 WHERE Id=1;"
        + clone_loop("@third", 10) + "COMMIT;")
    sample = json.loads(sql(prefix + """
IF (SELECT COUNT(*) FROM PlantPhotoEntries)<>50 THROW 51000,'Account count fixture differs',1;
IF (SELECT COUNT(*) FROM PlantPhotoEntries WHERE UserPlantId=@plant)<>20
 OR (SELECT COUNT(*) FROM PlantPhotoEntries WHERE UserPlantId=@second)<>20
 OR (SELECT COUNT(*) FROM PlantPhotoEntries WHERE UserPlantId=@third)<>10 THROW 51000,'Plant counts differ',1;
SELECT SUM(CONVERT(bigint,DATALENGTH(Image))+DATALENGTH(Thumbnail)) AS storedBytes,
 (SELECT ImageSha256 FROM PlantPhotoMedia WHERE Id=@white) AS whiteHash,
 (SELECT ImageSha256 FROM PlantPhotoMedia WHERE Id=@original) AS originalHash
 FROM PlantPhotoMedia FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
""").stdout.decode().strip())
    if not 1 <= sample["storedBytes"] < 2 * 1024 * 1024:
        raise ValueError("Count sample must stay well below the account byte quota.")
    signal("formats-account-ready", {"thirdPlantId": third})
    done = wait("formats-done.json")
    progress["phase"] = "verify-fifty-image-export"
    with zipfile.ZipFile(output / "count-photos.zip") as archive:
        items = json.loads(archive.read("manifest.json"))["images"]
        names, counts = set(), {sample["whiteHash"]: 0, sample["originalHash"]: 0}
        if len(items) != 50: raise ValueError("Count export omitted media.")
        for item in items:
            name = item["file"]
            if not re.fullmatch(r"photos/[a-f0-9]{32}\.jpg", name) or name in names:
                raise ValueError("Count export contains unexpected or repeated names.")
            names.add(name)
            data = archive.read(name); digest = hashlib.sha256(data).hexdigest()
            if (digest not in counts or item["sha256"].lower() != digest
                    or not data.startswith(b"\xff\xd8") or not data.endswith(b"\xff\xd9")):
                raise ValueError("Count export media/hash differs.")
            counts[digest] += 1
        if set(archive.namelist()) != names | {"manifest.json"} or sorted(counts.values()) != [1, 49]:
            raise ValueError("Count export mix/entries differ.")
    progress["phase"] = "release-only-count-fixtures"
    sql(prefix + """
BEGIN TRANSACTION;
UPDATE PhotoJournalQuota SET Revision=Revision+1 WHERE Id=1;
DELETE e FROM PlantPhotoEntries e JOIN UserPlants p ON p.Id=e.UserPlantId WHERE p.UserId=@owner AND e.Id<>@original;
DELETE UserPlants WHERE Id IN(@second,@third);
COMMIT;
IF (SELECT COUNT(*) FROM PlantPhotoEntries)<>1 THROW 51000,'Count fixture release failed',1;
""")
    signal("formats-continue", {})
    progress["phase"] = "accepted"
    return {"perPlantLimit": 20, "accountEntryLimit": 50, "storedBytesAtAccountCountLimit": sample["storedBytes"],
            "accountByteLimit": 20971520, "exportVerifiedImages": 50,
            "zipBytes": (output / "count-photos.zip").stat().st_size, "browser": done}
