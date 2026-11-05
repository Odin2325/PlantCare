using ImageMagick;
using ImageMagick.Drawing;

// Test-only generated pixels/metadata. No user images, credentials or network.
Directory.CreateDirectory("/out");
using (var image = new MagickImage(MagickColors.Red, 60, 30))
{
    new Drawables().FillColor(MagickColors.Green).Rectangle(30, 0, 59, 14)
        .FillColor(MagickColors.Blue).Rectangle(0, 15, 29, 29)
        .FillColor(MagickColors.Yellow).Rectangle(30, 15, 59, 29).Draw(image);
    var exif = new ExifProfile();
    exif.SetValue(ExifTag.Orientation, (ushort)6);
    exif.SetValue(ExifTag.Artist, "Generated format fixture");
    image.SetProfile(exif);
    image.Orientation = Orientation.RightTop;
    image.SetAttribute("comment", "Generated private metadata fixture");
    image.Quality = 100;
    image.Write("/out/oriented.jpg", MagickFormat.Jpeg);
}
foreach (var (name, format) in new[] { ("transparent.png", MagickFormat.Png), ("static.webp", MagickFormat.WebP) })
{
    using var image = new MagickImage(MagickColors.Transparent, 40, 20);
    var exif = new ExifProfile();
    exif.SetValue(ExifTag.Artist, "Generated format fixture");
    image.SetProfile(exif);
    image.SetAttribute("comment", "Generated private metadata fixture");
    image.Write("/out/" + name, format);
}
Console.WriteLine("PASS: generated JPEG/PNG/WebP format fixtures only.");
