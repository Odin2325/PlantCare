using System.Buffers.Binary;
using ImageMagick;

namespace PlantCare.PhotoProcessing;

public sealed class PhotoValidationException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed record NormalizedPhoto(byte[] Image, byte[] Thumbnail, uint Width, uint Height);

/// <summary>
/// Decoder prototype for a dedicated, disposable photo process. Never register in
/// the API: ImageMagick resource limits are process-global and are not OS limits.
/// </summary>
public static class PhotoNormalizer
{
    public const int MaximumInputBytes = 8 * 1024 * 1024;
    private static readonly SemaphoreSlim Slot = new(1, 1);
    private static readonly Lazy<bool> Initialized = new(Initialize);

    public static async Task<NormalizedPhoto> NormalizeAsync(Stream source, string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!await Slot.WaitAsync(0, cancellationToken)) throw new PhotoValidationException("photo-busy");
        try
        {
            if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
                throw new PhotoValidationException("photo-format-unsupported");
            using var input = new MemoryStream();
            var buffer = new byte[16 * 1024];
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0,
                    Math.Min(buffer.Length, MaximumInputBytes + 1 - (int)input.Length)), cancellationToken);
                if (read == 0) break;
                input.Write(buffer, 0, read);
                if (input.Length > MaximumInputBytes) throw new PhotoValidationException("photo-too-large");
            }
            var bytes = input.ToArray();
            var format = ValidateContainer(bytes, contentType);
            cancellationToken.ThrowIfCancellationRequested();
            _ = Initialized.Value;
            var settings = new MagickReadSettings { Format = format };
            using var image = new MagickImage();
            var warning = false;
            image.Warning += (_, _) => warning = true;
            // Ping before raster allocation. Container checks already exclude animation.
            image.Ping(bytes, settings);
            ValidateDimensions(image.Width, image.Height);
            if (warning) throw new PhotoValidationException("photo-invalid");
            image.Read(bytes, settings);
            if (warning) throw new PhotoValidationException("photo-invalid");
            ValidateDimensions(image.Width, image.Height);
            cancellationToken.ThrowIfCancellationRequested();
            image.AutoOrient();
            image.ColorSpace = ColorSpace.sRGB;
            image.BackgroundColor = MagickColors.White;
            image.Alpha(AlphaAction.Remove);
            image.Strip();
            image.Resize(new MagickGeometry(1600, 1600) { Greater = true });
            // Copy pixels onto a fresh image: no source attributes/profiles survive.
            using var clean = new MagickImage();
            using (var pixels = image.GetPixels())
                clean.ReadPixels(pixels.ToByteArray(PixelMapping.RGB)!,
                    new PixelReadSettings(image.Width, image.Height, StorageType.Char, PixelMapping.RGB));
            clean.ColorSpace = ColorSpace.sRGB;
            clean.Strip();
            var full = Encode(clean, 512 * 1024);
            using var thumbnail = clean.Clone();
            thumbnail.Resize(new MagickGeometry(320, 320) { Greater = true });
            thumbnail.Strip();
            var small = Encode(thumbnail, 64 * 1024);
            cancellationToken.ThrowIfCancellationRequested();
            return new(full, small, clean.Width, clean.Height);
        }
        catch (MagickException)
        {
            // Native errors can contain paths/metadata; never return their messages.
            throw new PhotoValidationException("photo-invalid");
        }
        finally { Slot.Release(); }
    }

    private static bool Initialize()
    {
        ResourceLimits.Memory = 128UL * 1024 * 1024;
        ResourceLimits.MaxMemoryRequest = 128UL * 1024 * 1024;
        ResourceLimits.MaxProfileSize = 1024 * 1024;
        ResourceLimits.Disk = 0;
        ResourceLimits.Width = 10_000;
        ResourceLimits.Height = 10_000;
        // ImageMagick counts intermediate images as well as input frames here.
        // Animation is rejected by the container parser before native decoding.
        ResourceLimits.ListLength = 16;
        ResourceLimits.Thread = 1;
        // Deliberately no process-wide Time limit: it counts process lifetime,
        // not a resettable per-request deadline. The future supervisor must kill.
        return true;
    }

    private static void ValidateDimensions(uint width, uint height)
    {
        if (width == 0 || height == 0 || width > 10_000 || height > 10_000 || (ulong)width * height > 20_000_000)
            throw new PhotoValidationException("photo-dimensions-exceeded");
    }

    private static byte[] Encode(IMagickImage<byte> image, int maximum)
    {
        // Bounded attempts; do not progressively allocate arbitrary outputs.
        foreach (var quality in new uint[] { 82, 70, 58 })
        {
            image.Quality = quality;
            using var output = new LimitedOutputStream(maximum);
            try { image.Write(output, MagickFormat.Jpeg); return output.ToArray(); }
            catch (OutputLimitException) { }
            catch (MagickException) when (output.LimitExceeded) { }
        }
        throw new PhotoValidationException("photo-output-too-large");
    }

    private static MagickFormat ValidateContainer(ReadOnlySpan<byte> bytes, string mime)
    {
        if (mime == "image/jpeg" && bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8
            && bytes[^2] == 0xff && bytes[^1] == 0xd9) return MagickFormat.Jpeg;
        if (mime == "image/png" && bytes.StartsWith(new byte[] {137,80,78,71,13,10,26,10}))
        {
            var position = 8;
            while (position <= bytes.Length - 12)
            {
                var length = BinaryPrimitives.ReadUInt32BigEndian(bytes[position..]);
                if (length > (uint)(bytes.Length - position - 12)) break;
                var kind = bytes.Slice(position + 4, 4);
                if (kind.SequenceEqual("acTL"u8)) throw new PhotoValidationException("photo-animation-unsupported");
                position += (int)length + 12;
                if (kind.SequenceEqual("IEND"u8) && length == 0 && position == bytes.Length) return MagickFormat.Png;
            }
        }
        if (mime == "image/webp" && bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8)
            && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)
            && BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]) == bytes.Length - 8)
        {
            var position = 12;
            while (position <= bytes.Length - 8)
            {
                var kind = bytes.Slice(position, 4);
                var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(position + 4)..]);
                if (length > (uint)(bytes.Length - position - 8)) break;
                if (kind.SequenceEqual("ANIM"u8) || kind.SequenceEqual("ANMF"u8)
                    || (kind.SequenceEqual("VP8X"u8) && length > 0 && (bytes[position + 8] & 2) != 0))
                    throw new PhotoValidationException("photo-animation-unsupported");
                var size = (long)length + 8 + (length & 1);
                if (size > bytes.Length - position) break;
                position += (int)size;
                if (position == bytes.Length) return MagickFormat.WebP;
            }
        }
        throw new PhotoValidationException("photo-invalid");
    }

    private sealed class OutputLimitException : IOException;

    private sealed class LimitedOutputStream(int maximum) : MemoryStream
    {
        public bool LimitExceeded { get; private set; }
        private void Check(long length)
        {
            if (length <= maximum) return;
            LimitExceeded = true;
            throw new OutputLimitException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            Check(Position + count);
            base.Write(buffer, offset, count);
        }
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Check(Position + buffer.Length);
            base.Write(buffer);
        }
        public override void WriteByte(byte value)
        {
            Check(Position + 1);
            base.WriteByte(value);
        }
        public override void SetLength(long value)
        {
            Check(value);
            base.SetLength(value);
        }
    }
}
