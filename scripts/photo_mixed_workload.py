"""Generated-only mixed workload checkpoint for Test-PhotoHttp.py, never a host tool."""
import concurrent.futures
import json
import re
import threading
import time
import uuid


def overlaps(left, right):
    return left[0] < right[1] and right[0] < left[1]


def mixed_checkpoint(sql, docker, compose, project, scratch, browser, progress):
    if not re.fullmatch(r"plantcare-photo-http-[a-f0-9]{32}", project):
        raise ValueError("Mixed workload requires its disposable project.")
    output = scratch / "output"

    def wait_file(name, seconds=180):
        deadline = time.monotonic() + seconds
        while not (output / name).exists():
            if browser.poll() is not None or time.monotonic() > deadline:
                failure = output / "mixed-failure.json"
                if failure.exists():
                    detail = json.loads(failure.read_text(encoding="utf-8"))
                    label = detail.get("step", "")
                    if re.fullmatch(r"[a-z-]{1,80}", label):
                        progress["browserPhase"] = label
                    if isinstance(detail.get("responseStatus"), int) and 100 <= detail["responseStatus"] <= 599:
                        progress["responseStatus"] = detail["responseStatus"]
                raise RuntimeError("Mixed browser checkpoint did not complete.")
            time.sleep(.1)
        return json.loads((output / name).read_text(encoding="utf-8"))

    progress["phase"] = "browser-ready"
    ready = wait_file("mixed-ready.json")
    plant = str(uuid.UUID(ready["plantId"]))
    progress["phase"] = "seed-due-schedules"
    # Only generated records in the UUID-owned SQL volume. No real schedules or
    # app settings are touched. Clone the catalog identity, not private data.
    sql("""
DECLARE @owner uniqueidentifier, @species uniqueidentifier;
SELECT @owner=UserId,@species=PlantSpeciesId FROM UserPlants WHERE Id='""" + plant + """';
IF @owner IS NULL THROW 51000,'Synthetic plant missing',1;
DECLARE @i int=0;
WHILE @i<100
BEGIN
 DECLARE @p uniqueidentifier=NEWID();
 INSERT UserPlants(Id,UserId,PlantSpeciesId,Nickname,IsActive,CreatedAtUtc)
 VALUES(@p,@owner,@species,N'Mixed workload',1,SYSDATETIMEOFFSET());
 INSERT CareSchedules(Id,UserPlantId,ActionType,IntervalDays,ScheduleMode,WeekDays,NextDueAtUtc,IsEnabled,IsArchived)
 VALUES(NEWID(),@p,1,7,0,0,DATEADD(minute,-1,SYSDATETIMEOFFSET()),1,0);
 SET @i+=1;
END;
""")
    services = ("api", "database", "gateway", "proxy", "worker")

    def metrics(service):
        container = project + "-" + service + "-1"
        result = docker("exec", container, "sh", "-c",
                        "cat /sys/fs/cgroup/memory.peak /sys/fs/cgroup/memory.max /sys/fs/cgroup/memory.events").stdout.decode().splitlines()
        if len(result) < 3 or not all(re.fullmatch(r"[1-9][0-9]*", v) for v in result[:2]):
            raise ValueError("Mixed cgroup accounting unavailable.")
        events = dict(line.split() for line in result[2:])
        state = json.loads(docker("inspect", "--format", "{{json .State}}", container).stdout)
        restarts = int(docker("inspect", "--format", "{{.RestartCount}}", container).stdout)
        if (not state["Running"] or state["OOMKilled"] or restarts
                or any(int(events.get(key, -1)) != 0 for key in ("oom", "oom_kill"))):
            raise ValueError("Mixed service stopped, restarted or hit OOM.")
        peak, limit = map(int, result[:2])
        if peak >= limit:
            raise ValueError("Mixed service exhausted its memory envelope.")
        return {"peakBytes": peak, "limitBytes": limit, "oom": 0, "oomKill": 0, "restarts": restarts}

    stopped = threading.Event()
    backups = []

    def backup_loop():
        while not stopped.is_set():
            start = time.time() * 1000
            sql("BACKUP DATABASE [PlantCare] TO DISK=N'/var/opt/mssql/data/mixed-load.bak' WITH COPY_ONLY,CHECKSUM,INIT;")
            backups.append([start, time.time() * 1000])
            stopped.wait(.15)

    with concurrent.futures.ThreadPoolExecutor(max_workers=1) as pool:
        progress["phase"] = "concurrent-browser-worker-backups"
        pending = pool.submit(backup_loop)
        try:
            # Release the browser first: worker startup and all four reminder
            # batches must occur inside the measured browser workload window.
            (output / "mixed-start.json").write_text('{"continue":true}', encoding="utf-8")
            docker(*compose, "up", "-d", "--no-build", "worker")
            done = wait_file("mixed-done.json")
        finally:
            stopped.set()
            pending.result(timeout=90)
    progress["phase"] = "verify-overlap"
    intervals = done["operations"]
    if len(intervals) != 8 or len(backups) < 2:
        raise ValueError("Mixed workload did not exercise repeated uploads, exports and backups.")
    overlap_count = sum(any(overlaps(interval, backup) for backup in backups) for interval in intervals)
    if not overlap_count:
        raise ValueError("No measured photo operation overlapped a SQL backup.")
    progress["phase"] = "verify-reminders"
    verification = json.loads(sql("""
DECLARE @owner uniqueidentifier=(SELECT UserId FROM UserPlants WHERE Id='""" + plant + """');
IF (SELECT COUNT(*) FROM Notifications n JOIN CareSchedules c ON c.Id=n.CareScheduleId
 JOIN UserPlants p ON p.Id=c.UserPlantId WHERE p.UserId=@owner AND p.Nickname=N'Mixed workload')<>100
 THROW 51000,'Reminder batch incomplete',1;
IF EXISTS(SELECT CareScheduleId,DueAtUtc,ReminderSequence FROM Notifications
 GROUP BY CareScheduleId,DueAtUtc,ReminderSequence HAVING COUNT(*)<>1)
 THROW 51000,'Duplicate reminders',1;
SELECT MIN(n.CreatedAtUtc) AS firstReminderUtc,MAX(n.CreatedAtUtc) AS lastReminderUtc,
 COUNT(*) AS reminders FROM Notifications n JOIN CareSchedules c ON c.Id=n.CareScheduleId
 JOIN UserPlants p ON p.Id=c.UserPlantId WHERE p.UserId=@owner AND p.Nickname=N'Mixed workload'
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
""").stdout.decode().strip())
    from datetime import datetime
    for key in ("firstReminderUtc", "lastReminderUtc"):
        value = datetime.fromisoformat(verification[key]).timestamp() * 1000
        if not done["window"][0] <= value <= done["window"][1]:
            raise ValueError("Reminder generation fell outside the browser workload window.")
    # Two more polls must preserve the same 100 reminders; the browser keeps its
    # account alive until verification completes, rather than erasing evidence.
    progress["phase"] = "verify-later-polls"
    time.sleep(11)
    sql("IF (SELECT COUNT(*) FROM Notifications n JOIN CareSchedules c ON c.Id=n.CareScheduleId JOIN UserPlants p ON p.Id=c.UserPlantId WHERE p.Nickname=N'Mixed workload')<>100 THROW 51000,'Reminder count changed',1;")
    worker = project + "-worker-1"
    heartbeat = json.loads(docker("exec", worker, "cat", "/var/lib/plantcare/worker-health/notifications.json").stdout)
    if not heartbeat["LastPassSucceeded"] or not heartbeat["LastSuccessUtc"]:
        raise ValueError("Reminder worker has no successful heartbeat.")
    sql("RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/data/mixed-load.bak' WITH CHECKSUM;")
    progress["phase"] = "verify-service-resources"
    resources = {service: metrics(service) for service in services}
    backup_bytes = int(sql("SELECT TOP(1) CONVERT(bigint,backup_size) FROM msdb.dbo.backupset WHERE database_name=N'PlantCare' AND is_copy_only=1 ORDER BY backup_finish_date DESC;").stdout.strip())
    (output / "mixed-continue.json").write_text('{"continue":true}', encoding="utf-8")
    progress["phase"] = "accepted"
    return {"services": resources, "reminders": 100, "duplicateReminders": 0,
            "photoOperations": len(intervals), "operationsOverlappingBackup": overlap_count,
            "backups": len(backups), "lastBackupBytes": backup_bytes,
            "backupVerifyOnly": True, "workerHeartbeat": True,
            "windowMilliseconds": done["window"][1] - done["window"][0]}
