using System.Security.Cryptography;

namespace PlantCare.Domain.Entities;

public sealed class PlantPhotoMedia
{
    private PlantPhotoMedia() { }
    public Guid Id { get; private set; }
    public byte[] Image { get; private set; } = [];
    public byte[] Thumbnail { get; private set; } = [];
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string ImageSha256 { get; private set; } = "";
    public string ThumbnailSha256 { get; private set; } = "";
    public PlantPhotoEntry Entry { get; private set; } = null!;

    public static PlantPhotoMedia Create(Guid entryId, byte[] image, byte[] thumbnail, int width, int height)
    {
        if (entryId == Guid.Empty || width is < 1 or > 1600 || height is < 1 or > 1600)
            throw new ArgumentException("Invalid normalized photo dimensions.");
        ValidateBytes(image, 524288); ValidateBytes(thumbnail, 65536);
        return new() { Id = entryId, Image = image.ToArray(), Thumbnail = thumbnail.ToArray(), Width = width, Height = height,
            ImageSha256 = Convert.ToHexStringLower(SHA256.HashData(image)), ThumbnailSha256 = Convert.ToHexStringLower(SHA256.HashData(thumbnail)) };
    }
    public static void ValidateBytes(byte[] bytes, int maximum)
    {
        if (bytes.Length < 4 || bytes.Length > maximum || bytes[0] != 255 || bytes[1] != 216 || bytes[^2] != 255 || bytes[^1] != 217)
            throw new ArgumentException("Invalid normalized JPEG content.");
    }
}
