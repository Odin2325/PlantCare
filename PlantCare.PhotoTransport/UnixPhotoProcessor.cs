using System.Buffers.Binary;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace PlantCare.PhotoTransport;

public sealed record ProcessedPhoto(byte[] Image, byte[] Thumbnail, uint Width, uint Height);
public sealed class PhotoProcessingException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
public interface IPhotoProcessor
{
    Task<ProcessedPhoto> NormalizeAsync(ReadOnlyMemory<byte> input, string contentType, CancellationToken cancellationToken = default);
}
public sealed record PhotoProcessorSettings(bool Enabled = false,
    string SocketPath = "/run/plantcare-photo/gateway.sock", uint GatewayUid = 0, int TimeoutSeconds = 45);

/// <summary>Private local IPC only. No HTTP, native codecs, process launch or Docker dependency.</summary>
public sealed class UnixPhotoProcessor : IPhotoProcessor
{
    public const int MaximumInputBytes = 8 * 1024 * 1024;
    public const int MaximumResultBytes = 20 + 512 * 1024 + 64 * 1024;
    private readonly PhotoProcessorSettings settings;
    private readonly SemaphoreSlim admission = new(1, 1);
    private static readonly HashSet<string> Errors = new(StringComparer.Ordinal)
    {
        "photo-invalid", "photo-too-large", "photo-format-unsupported", "photo-animation-unsupported",
        "photo-dimensions-exceeded", "photo-output-too-large", "photo-processing-failed",
        "photo-processing-unavailable", "photo-processing-timeout", "photo-busy"
    };

    public UnixPhotoProcessor(PhotoProcessorSettings settings)
    {
        if (settings.Enabled && (string.IsNullOrWhiteSpace(settings.SocketPath)
            || !Path.IsPathFullyQualified(settings.SocketPath) || Encoding.UTF8.GetByteCount(settings.SocketPath) > 100
            || settings.TimeoutSeconds is < 1 or > 60)) throw new ArgumentException("Invalid photo transport configuration.");
        this.settings = settings;
    }

    public async Task<ProcessedPhoto> NormalizeAsync(ReadOnlyMemory<byte> input, string contentType, CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled || !OperatingSystem.IsLinux()) throw Error("photo-processing-unavailable");
        if (input.Length > MaximumInputBytes) throw Error("photo-too-large");
        byte format = contentType switch { "image/jpeg" => 1, "image/png" => 2, "image/webp" => 3, _ => 0 };
        if (format == 0) throw Error("photo-format-unsupported");
        if (!await admission.WaitAsync(0, cancellationToken)) throw Error("photo-busy");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(settings.SocketPath), deadline.Token);
            var length = 12u;
            if (GetSocketOption(socket.Handle.ToInt32(), 1, 17, out var peer, ref length) != 0
                || length != 12 || peer.Uid != settings.GatewayUid) throw Error("photo-processing-unavailable");
            using var stream = new NetworkStream(socket, ownsSocket: false);
            var header = new byte[9];
            "PCR1"u8.CopyTo(header);
            header[4] = format;
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(5), input.Length);
            var sending = SendAsync(stream, header, input, deadline.Token);
            // Read concurrently so a gateway can reject a busy request before its body.
            try
            {
                var received = await ReceiveAsync(stream, deadline.Token);
                await sending;
                return received;
            }
            finally
            {
                deadline.Cancel();
                socket.Dispose();
                try { await sending; } catch { /* Receive/cancellation result remains authoritative. */ }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw Error("photo-processing-timeout"); }
        catch (SocketException) { throw Error("photo-processing-unavailable"); }
        catch (IOException) { throw Error("photo-processing-failed"); }
        finally { admission.Release(); }
    }

    private static async Task SendAsync(Stream stream, byte[] header, ReadOnlyMemory<byte> input, CancellationToken ct)
    {
        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(input, ct);
    }

    private static async Task<ProcessedPhoto> ReceiveAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[9];
        await stream.ReadExactlyAsync(header, ct);
        var size = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(5));
        if (!header.AsSpan(0, 4).SequenceEqual("PCS1"u8) || header[4] > 1
            || size < 0 || size > (header[4] == 1 ? 64 : MaximumResultBytes)) throw Error("photo-processing-failed");
        var body = new byte[size];
        await stream.ReadExactlyAsync(body, ct);
        var extra = new byte[1];
        if (await stream.ReadAsync(extra, ct) != 0) throw Error("photo-processing-failed");
        if (header[4] == 1)
        {
            var code = Encoding.ASCII.GetString(body);
            throw Error(Errors.Contains(code) ? code : "photo-processing-failed");
        }
        return ParsePhoto(body);
    }

    private static ProcessedPhoto ParsePhoto(byte[] bytes)
    {
        if (bytes.Length < 20 || !bytes.AsSpan(0, 4).SequenceEqual("PCP1"u8)) throw Error("photo-processing-failed");
        var width = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        var height = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8));
        var image = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12));
        var thumb = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(16));
        if (width is 0 or > 1600 || height is 0 or > 1600 || image is < 4 or > 524288
            || thumb is < 4 or > 65536 || (long)image + thumb + 20 != bytes.Length) throw Error("photo-processing-failed");
        var fullBytes = bytes.AsSpan(20, image).ToArray();
        var thumbnail = bytes.AsSpan(20 + image, thumb).ToArray();
        foreach (var content in new[] { fullBytes, thumbnail })
            if (content[0] != 255 || content[1] != 216 || content[^2] != 255 || content[^1] != 217) throw Error("photo-processing-failed");
        return new(fullBytes, thumbnail, width, height);
    }

    private static PhotoProcessingException Error(string code) => new(code);
    [StructLayout(LayoutKind.Sequential)]
    private struct PeerCredentials { public int Pid; public uint Uid; public uint Gid; }
    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    private static extern int GetSocketOption(int socket, int level, int option, out PeerCredentials peer, ref uint length);
}
