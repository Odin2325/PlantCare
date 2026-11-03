using System.Buffers.Binary;
using ImageMagick;
using PlantCare.PhotoProcessing;

namespace PlantCare.PhotoProcessing.Tests;

public sealed class PhotoNormalizerTests
{
    [Theory]
    [InlineData(MagickFormat.Jpeg, "image/jpeg")]
    [InlineData(MagickFormat.Png, "image/png")]
    [InlineData(MagickFormat.WebP, "image/webp")]
    public async Task SupportedImagesBecomeSmallMetadataFreeJpegs(MagickFormat format, string mime)
    {
        using var source = new MagickImage(MagickColors.Green, 1800, 900);
        source.SetAttribute("comment", "private camera comment");
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Artist, "private photographer");
        source.SetProfile(exif);
        var result = await Normalize(source.ToByteArray(format), mime);
        Assert.Equal(1600u, result.Width);
        Assert.Equal(800u, result.Height);
        Assert.InRange(result.Image.Length, 1, 512 * 1024);
        Assert.InRange(result.Thumbnail.Length, 1, 64 * 1024);
        foreach (var bytes in new[] { result.Image, result.Thumbnail })
        {
            using var output = new MagickImage(bytes);
            Assert.Equal(MagickFormat.Jpeg, output.Format);
            Assert.Empty(output.ProfileNames);
            Assert.Null(output.GetAttribute("comment"));
            Assert.False(output.HasAlpha);
        }
        using var thumb = new MagickImage(result.Thumbnail);
        Assert.Equal(320u, thumb.Width);
        Assert.Equal(160u, thumb.Height);
    }

    [Fact]
    public async Task OrientationIsAppliedBeforeMetadataIsDiscarded()
    {
        using var source = new MagickImage(MagickColors.Green, 60, 30);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)6);
        source.SetProfile(exif);
        source.Orientation = Orientation.RightTop;
        var result = await Normalize(source.ToByteArray(MagickFormat.Jpeg), "image/jpeg");
        Assert.Equal(30u, result.Width);
        Assert.Equal(60u, result.Height);
        using var output = new MagickImage(result.Image);
        Assert.Empty(output.ProfileNames);
    }

    [Fact]
    public async Task TransparentImageBecomesWhiteWithoutUpscaling()
    {
        using var source = new MagickImage(MagickColors.Transparent, 40, 20);
        var result = await Normalize(source.ToByteArray(MagickFormat.Png), "image/png");
        using var output = new MagickImage(result.Image);
        Assert.Equal(40u, output.Width);
        Assert.Equal(20u, output.Height);
        using var pixels = output.GetPixels();
        var pixel = pixels.GetPixel(10, 10).ToColor()!;
        Assert.InRange(pixel.R, (byte)250, byte.MaxValue);
        Assert.InRange(pixel.G, (byte)250, byte.MaxValue);
        Assert.InRange(pixel.B, (byte)250, byte.MaxValue);
    }

    [Theory]
    [InlineData("image/svg+xml")]
    [InlineData("image/gif")]
    [InlineData("image/heic")]
    [InlineData("text/plain")]
    public async Task UnsupportedMimeIsRejectedBeforeReading(string mime)
    {
        using var unreadable = new RejectReadStream();
        await Reject(unreadable, mime, "photo-format-unsupported");
    }

    [Fact]
    public async Task ContentMustMatchMimeAndCorruptOrTruncatedImagesAreRejected()
    {
        using var source = new MagickImage(MagickColors.Red, 40, 20);
        var jpeg = source.ToByteArray(MagickFormat.Jpeg);
        await Reject(new MemoryStream(jpeg), "image/png", "photo-invalid");
        await Reject(new MemoryStream(jpeg[..^10]), "image/jpeg", "photo-invalid");
        await Reject(new MemoryStream("<svg/>"u8.ToArray()), "image/jpeg", "photo-invalid");
        await Reject(new MemoryStream(new byte[] {255,216,0,0,255,217}), "image/jpeg", "photo-invalid");
        var png = source.ToByteArray(MagickFormat.Png);
        await Reject(new MemoryStream(png[..^1]), "image/png", "photo-invalid");
    }

    [Fact]
    public async Task ContainerDimensionsAreCheckedBeforePixelDecoding()
    {
        using var source = new MagickImage(MagickColors.Red, 20, 20);
        var bytes = source.ToByteArray(MagickFormat.Png);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16), 5000);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20), 4100);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(29), Crc32(bytes.AsSpan(12, 17)));
        await Reject(new MemoryStream(bytes), "image/png", "photo-dimensions-exceeded");
    }

    [Fact]
    public async Task AnimatedContainersAreRejectedBeforeDecoder()
    {
        using var source = new MagickImage(MagickColors.Red, 20, 20);
        var png = source.ToByteArray(MagickFormat.Png);
        // A validly framed acTL chunk suffices to prohibit animation before decode.
        var animated = new byte[png.Length + 20];
        png.AsSpan(0, 33).CopyTo(animated);
        BinaryPrimitives.WriteUInt32BigEndian(animated.AsSpan(33), 8);
        "acTL"u8.CopyTo(animated.AsSpan(37));
        png.AsSpan(33).CopyTo(animated.AsSpan(53));
        await Reject(new MemoryStream(animated), "image/png", "photo-animation-unsupported");
        var webp = new byte[22];
        "RIFF"u8.CopyTo(webp);
        BinaryPrimitives.WriteUInt32LittleEndian(webp.AsSpan(4), 14);
        "WEBPVP8X"u8.CopyTo(webp.AsSpan(8));
        BinaryPrimitives.WriteUInt32LittleEndian(webp.AsSpan(16), 1);
        webp[20] = 2;
        await Reject(new MemoryStream(webp), "image/webp", "photo-animation-unsupported");
    }

    [Fact]
    public async Task UntrustedLengthsAndOversizedUnseekableInputAreBounded()
    {
        var malformed = new byte[24];
        new byte[] {137,80,78,71,13,10,26,10}.CopyTo(malformed, 0);
        BinaryPrimitives.WriteUInt32BigEndian(malformed.AsSpan(8), uint.MaxValue);
        await Reject(new MemoryStream(malformed), "image/png", "photo-invalid");
        using var input = new EndlessStream();
        await Reject(input, "image/jpeg", "photo-too-large");
        Assert.Equal(PhotoNormalizer.MaximumInputBytes + 1, input.BytesRead);
    }

    [Fact]
    public async Task IncompressibleImageCannotExceedOutputBudget()
    {
        var pixels = new byte[1600 * 1600 * 3];
        new Random(42).NextBytes(pixels);
        using var source = new MagickImage();
        source.ReadPixels(pixels, new PixelReadSettings(1600, 1600, StorageType.Char, PixelMapping.RGB));
        var bytes = source.ToByteArray(MagickFormat.Png);
        Assert.True(bytes.Length <= PhotoNormalizer.MaximumInputBytes);
        await Reject(new MemoryStream(bytes), "image/png", "photo-output-too-large");
    }

    [Fact]
    public async Task BusyInputIsRejectedAndCancellationReleasesSlot()
    {
        using var cancel = new CancellationTokenSource();
        using var input = new WaitingStream();
        var first = PhotoNormalizer.NormalizeAsync(input, "image/jpeg", cancel.Token);
        await input.Started.Task;
        await Reject(new MemoryStream(), "image/jpeg", "photo-busy");
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await Reject(new MemoryStream(), "image/jpeg", "photo-invalid");
    }

    private static Task<NormalizedPhoto> Normalize(byte[] bytes, string mime) =>
        PhotoNormalizer.NormalizeAsync(new MemoryStream(bytes), mime);

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        uint crc = uint.MaxValue;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0u);
        }
        return ~crc;
    }

    private static async Task Reject(Stream stream, string mime, string code)
    {
        var error = await Assert.ThrowsAsync<PhotoValidationException>(() => PhotoNormalizer.NormalizeAsync(stream, mime));
        Assert.Equal(code, error.Code);
        Assert.Equal(code, error.Message);
    }

    private class RejectReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Unexpected read");
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
    }

    private sealed class EndlessStream : RejectReadStream
    {
        public int BytesRead { get; private set; }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            buffer.Span.Clear();
            BytesRead += buffer.Length;
            return ValueTask.FromResult(buffer.Length);
        }
    }

    private sealed class WaitingStream : RejectReadStream
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }
    }
}
