using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace PlantCare.Api.Photos;

internal sealed record PhotoUpload(Guid RequestId, DateOnly Date, string? Notes, string Mime, byte[] Bytes);
internal static class PhotoUploadReader
{
    public const int MaximumRequestBytes = 9 * 1024 * 1024;
    public static async Task<PhotoUpload> ReadAsync(HttpRequest request, CancellationToken ct)
    {
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var type) || type.MediaType != "multipart/form-data")
            throw new InvalidDataException();
        var boundary = HeaderUtilities.RemoveQuotes(type.Boundary).Value;
        if (string.IsNullOrEmpty(boundary) || boundary.Length > 128) throw new InvalidDataException();
        using var body = new BoundedBody(request.Body);
        var reader = new MultipartReader(boundary, body) { HeadersCountLimit = 8, HeadersLengthLimit = 2048, BodyLengthLimit = MaximumRequestBytes };
        var fields = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        string? mime = null;
        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync(ct)) is not null)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
                || disposition.DispositionType != "form-data") throw new InvalidDataException();
            var name = HeaderUtilities.RemoveQuotes(disposition.Name).Value ?? "";
            if (name is not ("file" or "requestId" or "photographedOn" or "notes") || fields.ContainsKey(name) || fields.Count >= 4)
                throw new InvalidDataException();
            if (name == "file")
            {
                mime = section.ContentType;
                if (mime is not ("image/jpeg" or "image/png" or "image/webp")) throw new UnsupportedPhotoFormatException();
            }
            fields.Add(name, await ReadPart(section.Body, name == "file" ? 8 * 1024 * 1024 : 2048, ct));
        }
        // Multipart permits an epilogue. Count the entire HTTP body before any
        // processing/write, including chunked bytes after the closing boundary.
        var remainder = new byte[8192];
        while (await body.ReadAsync(remainder.AsMemory(), ct) != 0) { }
        var utf8 = new UTF8Encoding(false, true);
        string Text(string name) => fields.TryGetValue(name, out var data) ? utf8.GetString(data) : "";
        if (!Guid.TryParseExact(Text("requestId"), "D", out var id) || id == Guid.Empty
            || !DateOnly.TryParseExact(Text("photographedOn"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || !fields.TryGetValue("file", out var bytes) || bytes.Length == 0 || mime is null) throw new InvalidDataException();
        var notes = Text("notes");
        if (notes.Length > 500) throw new InvalidDataException();
        return new(id, date, notes, mime, bytes);
    }
    private static async Task<byte[]> ReadPart(Stream stream, int maximum, CancellationToken ct)
    {
        using var buffer = new MemoryStream(); var chunk = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, maximum + 1 - (int)buffer.Length)), ct);
            if (read == 0) return buffer.ToArray();
            buffer.Write(chunk, 0, read);
            if (buffer.Length > maximum) throw new BadHttpRequestException("Photo form exceeds its size limit.", 413);
        }
    }
    private sealed class BoundedBody(Stream inner) : Stream
    {
        private long consumed;
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            var read = await inner.ReadAsync(buffer[..(int)Math.Min(buffer.Length, MaximumRequestBytes + 1 - consumed)], ct);
            consumed += read;
            if (consumed > MaximumRequestBytes) throw new BadHttpRequestException("Photo request exceeds its size limit.", 413);
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
internal sealed class UnsupportedPhotoFormatException : Exception;
