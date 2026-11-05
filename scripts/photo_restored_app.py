"""Real isolated restored API/key and erased-account replay acceptance."""
import copy
import hashlib
import json
import re
import tarfile
import time
import uuid
import zipfile


def restored_app(sql, docker, compose, project, scratch, browser, payload, sample,
                 config, source_environment, passwords, progress):
    if not re.fullmatch(r"plantcare-photo-http-[a-f0-9]{32}", project):
        raise ValueError("Restored app requires its disposable project.")
    output = scratch / "output"
    authority = payload.parent / "authority"
    authority.mkdir()
    key_hashes = {}
    with tarfile.open(payload / "keys.tar", "r:") as archive:
        for member in archive.getmembers():
            name = member.name.removeprefix("./")
            if member.isdir() and name == ".": continue
            if (not member.isfile() or not re.fullmatch(r"key-[a-f0-9-]{36}\.xml", name)
                    or not 1 <= member.size <= 262144 or name in key_hashes):
                raise ValueError("Unexpected generated key archive member.")
            key_hashes[name] = hashlib.sha256(archive.extractfile(member).read()).hexdigest()
    if not 1 <= len(key_hashes) <= 64:
        raise ValueError("Generated key ring is missing or oversized.")

    def wait(name):
        deadline = time.monotonic() + 180
        while not (output / name).exists():
            if browser.poll() is not None or time.monotonic() > deadline:
                path = output / "recovery-app-failure.json"
                if path.exists():
                    stage = json.loads(path.read_text(encoding="utf-8")).get("step", "")
                    if re.fullmatch(r"[a-z-]{1,80}", stage): progress["browserPhase"] = stage
                raise RuntimeError("Restored browser checkpoint did not complete.")
            time.sleep(.1)
        return json.loads((output / name).read_text(encoding="utf-8"))

    def signal(name):
        temporary = output / (name + ".tmp")
        temporary.write_text('{"continue":true}', encoding="utf-8")
        temporary.replace(output / (name + ".json"))

    def save():
        (scratch / "compose.json").write_text(json.dumps(config), encoding="utf-8")

    def volume(name, target):
        return {"type": "volume", "source": name, "target": target}

    db = "PlantCareEncryptedPhotoRestore"
    api_environment = dict(source_environment)
    for name in ("Application__ClientBaseUrl", "Email__ClientBaseUrl"):
        api_environment[name] = "https://plantcare-recovered.test:8443"
    api_environment.update({"ConnectionStrings__PlantCareDatabase": source_environment["ConnectionStrings__PlantCareDatabase"].replace("Database=PlantCare;", "Database=" + db + ";"),
                            "PhotoProcessing__Enabled": "false", "PhotoJournal__UploadsEnabled": "false"})
    for name in ("recovered-keys", "recovered-empty-keys", "recovered-caddy-data", "recovered-caddy-config"):
        config["volumes"][name] = {}
    recovered = copy.deepcopy(config["services"]["api"])
    recovered.update(environment=api_environment, volumes=[volume("recovered-empty-keys", "/var/lib/plantcare/keys")], profiles=["recovered"])
    config["services"]["recovered-api"] = recovered
    proxy = copy.deepcopy(config["services"]["proxy"])
    (scratch / "RecoveredCaddyfile").write_text((scratch / "Caddyfile").read_text(encoding="utf-8").replace("reverse_proxy api:8080", "reverse_proxy recovered-api:8080"), encoding="utf-8")
    proxy.update(environment={"PLANTCARE_DOMAIN": "plantcare-recovered.test:8443"}, profiles=["recovered"],
                 networks={"default": {"aliases": ["plantcare-recovered.test"]}}, volumes=[
                     {"type": "bind", "source": str(scratch / "RecoveredCaddyfile"), "target": "/etc/caddy/Caddyfile", "read_only": True},
                     volume("recovered-caddy-data", "/data"), volume("recovered-caddy-config", "/config")])
    config["services"]["recovered-proxy"] = proxy
    admin = copy.deepcopy(config["services"]["migrate"])
    admin["user"] = "0:0"
    admin["environment"]["ConnectionStrings__PlantCareDatabase"] = admin["environment"]["ConnectionStrings__PlantCareDatabase"].replace("Database=PlantCare;", "Database=" + db + ";")
    admin["environment"].update({"Recovery__AllowAccountErasureApply": "true", "Recovery__AccountErasureSource": "plantcare-local"})
    admin["volumes"].append({"type": "bind", "source": str(authority), "target": "/authority"})
    config["services"]["recovered-admin"] = admin
    key_seed = copy.deepcopy(admin)
    key_seed.update(entrypoint=["sh", "-c"], volumes=[
        {"type": "bind", "source": str(payload), "target": "/capture", "read_only": True},
        volume("recovered-keys", "/var/lib/plantcare/keys")])
    config["services"]["recovered-key-seed"] = key_seed
    save()
    try:
        progress["phase"] = "restricted-runtime-and-empty-key-control"
        docker(*compose, "run", "--rm", "--no-deps", "recovered-admin", "--provision-runtime-users")
        docker(*compose, "up", "-d", "--no-build", "--wait", "--wait-timeout", "120", "recovered-api", "recovered-proxy")
        signal("recovery-empty-ready")
        wait("recovery-empty-done.json")
        progress["phase"] = "restore-key-ring-and-real-app"
        docker(*compose, "stop", "recovered-api")
        docker(*compose, "run", "--rm", "--no-deps", "recovered-key-seed",
               "tar -xf /capture/keys.tar -C /var/lib/plantcare/keys && chown -R 1654:1654 /var/lib/plantcare/keys && chmod 700 /var/lib/plantcare/keys && chmod 600 /var/lib/plantcare/keys/key-*.xml")
        recovered["volumes"] = [volume("recovered-keys", "/var/lib/plantcare/keys")]
        save()
        docker(*compose, "up", "-d", "--no-build", "--wait", "--wait-timeout", "120", "recovered-api")
        api = project + "-recovered-api-1"
        mounts = json.loads(docker("inspect", "--format", "{{json .Mounts}}", api).stdout)
        if any("docker.sock" in item["Destination"] or item["Destination"] == "/run/plantcare-photo" for item in mounts):
            raise ValueError("Restored API mount boundary changed.")
        docker("exec", api, "dotnet", "PlantCare.Api.dll", "--check-runtime-access", "api")
        actual = {}
        for line in docker("exec", api, "sh", "-c", "cd /var/lib/plantcare/keys && sha256sum key-*.xml").stdout.decode().splitlines():
            digest, name = line.split(); actual[name] = digest
        if actual != key_hashes: raise ValueError("Recovered key ring differs from decrypted capture.")
        signal("recovery-app-ready")
        result = wait("recovery-app-done.json")
        owner, outsider = (str(uuid.UUID(result[key])) for key in ("ownerId", "outsiderId"))
        count = int(sample["entries"])
        sql(f"""
IF (SELECT COUNT(*) FROM PlantPhotoMedia)<>{count} OR (SELECT COUNT(*) FROM AspNetUsers)<>2
 THROW 51000,'Source fixture changed during restored app test',1;
IF EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.AspNetUsers WHERE Id='{owner}')
 OR EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoEntries)
 OR EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoMedia)
 OR EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.UserPlants)
 THROW 51000,'Restored owner data remains after erasure',1;
IF NOT EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.AccountErasures WHERE UserId='{owner}')
 OR NOT EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.AspNetUsers WHERE Id='{outsider}')
 THROW 51000,'Restored erasure marker or other account missing',1;
""")
        with zipfile.ZipFile(output / "recovered-photos.zip") as archive:
            entries = json.loads(archive.read("manifest.json"))["images"]
            names = {item["file"] for item in entries}
            if len(entries) != count or len(names) != count or set(archive.namelist()) != names | {"manifest.json"}:
                raise ValueError("Recovered private ZIP entries differ.")
            for item in entries:
                if not re.fullmatch(r"photos/[a-f0-9]{32}\.jpg", item["file"]):
                    raise ValueError("Recovered private ZIP filename differs.")
                if hashlib.sha256(archive.read(item["file"])).hexdigest() != sample["imageSha256"] or item["sha256"].lower() != sample["imageSha256"]:
                    raise ValueError("Recovered private ZIP image/hash differs.")
        progress["phase"] = "capture-current-erasure-and-replay-old-snapshot"
        docker(*compose, "stop", "recovered-api", "recovered-proxy")
        docker(*compose, "run", "--rm", "--no-deps", "recovered-admin", "--export-account-erasures", "plantcare-local", "/authority/erasures.json")
        sql("""
DROP DATABASE [PlantCareEncryptedPhotoRestore];
RESTORE DATABASE [PlantCareEncryptedPhotoRestore] FROM DISK=N'/var/opt/mssql/data/photo-encrypted-returned.bak'
 WITH CHECKSUM,RECOVERY,
 MOVE N'PlantCare' TO N'/var/opt/mssql/data/photo-encrypted-restored.mdf',
 MOVE N'PlantCare_log' TO N'/var/opt/mssql/data/photo-encrypted-restored.ldf';
""")
        docker(*compose, "run", "--rm", "--no-deps", "recovered-admin", "--apply-account-erasures", "/authority/erasures.json")
        sql(f"""
IF EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.AspNetUsers WHERE Id='{owner}')
 OR EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoMedia)
 OR EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.PlantPhotoEntries)
 THROW 51000,'Old snapshot revived erased photo data',1;
IF NOT EXISTS(SELECT 1 FROM PlantCareEncryptedPhotoRestore.dbo.AccountErasures WHERE UserId='{owner}')
 THROW 51000,'Replayed erasure marker missing',1;
""")
        docker(*compose, "up", "-d", "--no-build", "--wait", "--wait-timeout", "120", "recovered-api", "recovered-proxy")
        signal("recovery-replay-ready")
        wait("recovery-replay-done.json")
        progress["phase"] = "accepted"
        return {"restoredSessionAccepted": True, "emptyKeyRingRefusedSession": True,
                "restoredKeyFilesVerified": len(actual), "restrictedApiVerified": True,
                "photosReadAndExportVerified": count, "ownerIsolationVerified": True,
                "freshLoginVerified": True, "accountAndMediaErased": True,
                "currentErasureReappliedBeforeRestart": True, "oldSnapshotCannotReviveAccount": True,
                "otherAccountStillUsable": True, "sourceFixtureUnchanged": True,
                "restoredUploadsEnabled": False, "restoredWorkerStarted": False}
    finally:
        docker(*compose, "stop", "recovered-api", "recovered-proxy")
