using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PlantCare.Infrastructure.Operations;

public sealed class WorkerHealthOptions
{
    public string HeartbeatPath { get; init; } = "";
    public int MaxWorkerAgeSeconds { get; init; } = 180;
    public long MinFreeBytes { get; init; } = 5L * 1024 * 1024 * 1024;
}

public sealed record WorkerHeartbeat(DateTimeOffset? LastSuccessUtc, bool LastPassSucceeded);

public sealed class WorkerHeartbeatStore(IOptions<WorkerHealthOptions> options, TimeProvider time)
{
    private DateTimeOffset? lastSuccess;
    public void Reset() { lastSuccess = null; Write(false); }
    public void Succeeded() { lastSuccess = time.GetUtcNow(); Write(true); }
    public void Failed() => Write(false);

    private void Write(bool succeeded)
    {
        var path = options.Value.HeartbeatPath;
        if (string.IsNullOrWhiteSpace(path)) return; // Optional outside Compose.
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new WorkerHeartbeat(lastSuccess, succeeded)));
        File.Move(temporary, path, overwrite: true);
    }
}
