using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace PlantCare.PhotoProcessing;

/// <summary>Trusted launch configuration only. Never derive commands from an upload.</summary>
public sealed record PhotoProcessCommand(string FileName, IReadOnlyList<string> Arguments);

public sealed class PhotoProcessSupervisor(PhotoProcessCommand command, TimeSpan timeout)
{
    private static readonly SemaphoreSlim Admission = new(1, 1);
    private static bool cleanupFailed;
    public const int MaximumResponseBytes = 20 + 512 * 1024 + 64 * 1024;
    private static readonly HashSet<string> SafeErrors = new(StringComparer.Ordinal)
    {
        "photo-invalid", "photo-too-large", "photo-format-unsupported", "photo-animation-unsupported",
        "photo-dimensions-exceeded", "photo-output-too-large", "photo-processing-failed"
    };

    public async Task<NormalizedPhoto> NormalizeAsync(ReadOnlyMemory<byte> bytes, string contentType,
        CancellationToken cancellationToken = default)
    {
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        if (bytes.Length > PhotoNormalizer.MaximumInputBytes) throw new PhotoValidationException("photo-too-large");
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
            throw new PhotoValidationException("photo-format-unsupported");
        if (!await Admission.WaitAsync(0, cancellationToken)) throw new PhotoValidationException("photo-busy");
        Process? process = null;
        try
        {
            if (cleanupFailed) throw new PhotoValidationException("photo-processing-unavailable");
            var start = new ProcessStartInfo(command.FileName)
            {
                UseShellExecute = false, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
            };
            foreach (var argument in command.Arguments) start.ArgumentList.Add(argument);
            start.ArgumentList.Add(contentType);
            // No inherited connection strings, credentials, diagnostics or profiler settings.
            start.Environment.Clear();
            if (OperatingSystem.IsWindows()) start.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            start.Environment["DOTNET_EnableDiagnostics"] = "0";
            start.Environment["DOTNET_GCHeapHardLimit"] = "4000000";
            start.Environment["DOTNET_gcServer"] = "0";
            start.Environment["DOTNET_PROCESSOR_COUNT"] = "1";
            start.Environment["HOME"] = "/nonexistent";
            start.Environment["TMPDIR"] = "/nonexistent";
            process = new Process { StartInfo = start };
            if (!process.Start()) throw new PhotoValidationException("photo-processing-unavailable");
            var work = ExchangeAsync(process, bytes);
            try { return await work.WaitAsync(timeout, cancellationToken); }
            catch (TimeoutException) { throw new PhotoValidationException("photo-processing-timeout"); }
            finally
            {
                // Observe failures from pipe tasks after a timeout/cancellation without
                // blocking cleanup on a hostile child that still owns the pipes.
                _ = work.ContinueWith(task => _ = task.Exception,
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }
        }
        catch (System.ComponentModel.Win32Exception) { throw new PhotoValidationException("photo-processing-unavailable"); }
        catch (IOException) { throw new PhotoValidationException("photo-processing-failed"); }
        finally
        {
            if (process is not null)
            {
                try
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (InvalidOperationException) { /* Start failed: no process exists. */ }
                catch { cleanupFailed = true; }
                process.Dispose();
            }
            Admission.Release();
        }
    }

    private static async Task<NormalizedPhoto> ExchangeAsync(Process process, ReadOnlyMemory<byte> bytes)
    {
        var output = ReadBoundedAsync(process.StandardOutput.BaseStream, MaximumResponseBytes);
        var errors = ReadBoundedAsync(process.StandardError.BaseStream, 1024);
        var input = WriteInputAsync(process, bytes);
        var exit = process.WaitForExitAsync();
        // Fail fast on any pipe fault; do not wait for all pipes to close first.
        var pending = new List<Task> { output, errors, input, exit };
        foreach (var task in pending)
            _ = task.ContinueWith(fault => _ = fault.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        while (pending.Count > 0)
        {
            var done = await Task.WhenAny(pending);
            await done;
            pending.Remove(done);
        }
        if (process.ExitCode != 0)
        {
            var code = Encoding.ASCII.GetString(await errors).Trim();
            throw new PhotoValidationException(SafeErrors.Contains(code) ? code : "photo-processing-failed");
        }
        if ((await errors).Length != 0) throw new PhotoValidationException("photo-processing-failed");
        return DecodeResponse(await output);
    }

    private static async Task WriteInputAsync(Process process, ReadOnlyMemory<byte> bytes)
    {
        try { await process.StandardInput.BaseStream.WriteAsync(bytes); }
        finally { process.StandardInput.Close(); }
    }

    internal static async Task<byte[]> ReadBoundedAsync(Stream stream, int maximum)
    {
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maximum + 1 - (int)output.Length)));
            if (read == 0) return output.ToArray();
            output.Write(buffer, 0, read);
            if (output.Length > maximum) throw new PhotoValidationException("photo-processing-failed");
        }
    }

    internal static byte[] EncodeResponse(NormalizedPhoto photo)
    {
        var bytes = new byte[20 + photo.Image.Length + photo.Thumbnail.Length];
        "PCP1"u8.CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), photo.Width);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), photo.Height);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), photo.Image.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), photo.Thumbnail.Length);
        photo.Image.CopyTo(bytes, 20);
        photo.Thumbnail.CopyTo(bytes, 20 + photo.Image.Length);
        return bytes;
    }

    private static NormalizedPhoto DecodeResponse(byte[] bytes)
    {
        if (bytes.Length < 20 || !bytes.AsSpan(0, 4).SequenceEqual("PCP1"u8)) throw new PhotoValidationException("photo-processing-failed");
        var width = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        var height = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8));
        var image = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12));
        var thumb = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(16));
        if (width is 0 or > 1600 || height is 0 or > 1600 || image is < 4 or > 524288
            || thumb is < 4 or > 65536 || (long)image + thumb + 20 != bytes.Length)
            throw new PhotoValidationException("photo-processing-failed");
        var fullBytes = bytes.AsSpan(20, image).ToArray();
        var thumbBytes = bytes.AsSpan(20 + image, thumb).ToArray();
        foreach (var content in new[] { fullBytes, thumbBytes })
            if (content[0] != 255 || content[1] != 216 || content[^2] != 255 || content[^1] != 217)
                throw new PhotoValidationException("photo-processing-failed");
        return new(fullBytes, thumbBytes, width, height);
    }
}
