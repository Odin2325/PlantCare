using System.Buffers.Binary;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using PlantCare.PhotoTransport;

namespace PlantCare.PhotoTransport.Tests;

public sealed class UnixPhotoProcessorTests
{
    [Fact]
    public async Task DisabledTransportNeverConnects()
    {
        var client = new UnixPhotoProcessor(new());
        var error = await Assert.ThrowsAsync<PhotoProcessingException>(() => client.NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png"));
        Assert.Equal("photo-processing-unavailable", error.Code);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("bad-magic")]
    [InlineData("oversize")]
    [InlineData("truncated")]
    [InlineData("extra")]
    [InlineData("private-error")]
    [InlineData("busy")]
    [InlineData("bad-photo")]
    public async Task ProtocolIsBoundedAndFailuresAreSanitized(string mode)
    {
        if (!OperatingSystem.IsLinux()) return; // Real Linux run is required below.
        using var fixture = new SocketFixture();
        var peer = fixture.AcceptAsync(async stream =>
        {
            var request = new byte[9];
            await stream.ReadExactlyAsync(request);
            Assert.Equal("PCR1", System.Text.Encoding.ASCII.GetString(request[..4]));
            Assert.Equal(2, request[4]);
            Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(request.AsSpan(5)));
            var input = new byte[3];
            await stream.ReadExactlyAsync(input);
            Assert.Equal(new byte[] {1,2,3}, input);
            var photo = Photo();
            if (mode == "bad-photo") photo[0] = 0;
            var body = mode == "private-error" ? "secret/path/raw log"u8.ToArray() : mode == "busy" ? "photo-busy"u8.ToArray() : photo;
            var header = new byte[9];
            "PCS1"u8.CopyTo(header);
            header[4] = (byte)(mode is "private-error" or "busy" ? 1 : 0);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(5), body.Length);
            if (mode == "bad-magic") header[0] = 0;
            if (mode == "oversize") BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(5), int.MaxValue);
            await stream.WriteAsync(header);
            if (mode != "truncated") await stream.WriteAsync(body);
            if (mode == "extra") await stream.WriteAsync(new byte[1]);
        });
        var client = fixture.Client();
        if (mode == "success")
        {
            var result = await client.NormalizeAsync(new byte[] {1,2,3}, "image/png");
            Assert.Equal(8u, result.Width);
            Assert.Equal(4u, result.Height);
        }
        else
        {
            var error = await Assert.ThrowsAsync<PhotoProcessingException>(() => client.NormalizeAsync(new byte[] {1,2,3}, "image/png"));
            Assert.Equal(mode == "busy" ? "photo-busy" : "photo-processing-failed", error.Code);
            Assert.Equal(error.Code, error.Message);
        }
        await peer;
    }

    [Fact]
    public async Task WrongGatewayIdentityIsRejectedBeforeSendingImage()
    {
        if (!OperatingSystem.IsLinux()) return;
        using var fixture = new SocketFixture();
        var peer = fixture.AcceptAsync(async stream => Assert.Equal(0, await stream.ReadAsync(new byte[1])));
        var error = await Assert.ThrowsAsync<PhotoProcessingException>(() => fixture.Client(wrongUid: true).NormalizeAsync(new byte[100], "image/png"));
        Assert.Equal("photo-processing-unavailable", error.Code);
        await peer;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TimeoutOrCancellationClosesSocketAndReleasesAdmission(bool cancel)
    {
        if (!OperatingSystem.IsLinux()) return;
        using var fixture = new SocketFixture();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var peer = fixture.AcceptAsync(async stream =>
        {
            await stream.ReadExactlyAsync(new byte[12]);
            started.SetResult();
            Assert.Equal(0, await stream.ReadAsync(new byte[1]));
        });
        using var cts = new CancellationTokenSource();
        var client = fixture.Client();
        var request = client.NormalizeAsync(new byte[3], "image/png", cts.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var busy = await Assert.ThrowsAsync<PhotoProcessingException>(() => client.NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png"));
        Assert.Equal("photo-busy", busy.Code);
        if (cancel)
        {
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        }
        else
        {
            var error = await Assert.ThrowsAsync<PhotoProcessingException>(() => request);
            Assert.Equal("photo-processing-timeout", error.Code);
        }
        await peer;
        var second = fixture.AcceptAsync(async stream =>
        {
            await stream.ReadExactlyAsync(new byte[9]);
            var body = Photo();
            var header = new byte[9];
            "PCS1"u8.CopyTo(header);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(5), body.Length);
            await stream.WriteAsync(header); await stream.WriteAsync(body);
        });
        await client.NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png");
        await second;
    }

    private static byte[] Photo()
    {
        var data = new byte[28]; "PCP1"u8.CopyTo(data);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 8);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), 4);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(12), 4);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(16), 4);
        new byte[] {255,216,255,217,255,216,255,217}.CopyTo(data, 20);
        return data;
    }

    private sealed class SocketFixture : IDisposable
    {
        private readonly string path = "/tmp/pc-photo-" + Guid.NewGuid().ToString("N") + ".sock";
        private readonly Socket listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        public SocketFixture() { listener.Bind(new UnixDomainSocketEndPoint(path)); listener.Listen(1); }
        public UnixPhotoProcessor Client(bool wrongUid = false) => new(new(true, path, wrongUid ? GetUid() + 1 : GetUid(), 1));
        public async Task AcceptAsync(Func<Stream, Task> handler)
        {
            using var socket = await listener.AcceptAsync();
            using var stream = new NetworkStream(socket);
            await handler(stream);
        }
        public void Dispose() { listener.Dispose(); File.Delete(path); }
        [DllImport("libc", EntryPoint = "geteuid")] private static extern uint GetUid();
    }
}
