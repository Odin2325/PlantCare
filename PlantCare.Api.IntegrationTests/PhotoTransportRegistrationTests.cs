using Microsoft.Extensions.DependencyInjection;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.PhotoTransport;

namespace PlantCare.Api.IntegrationTests;

public sealed class PhotoTransportRegistrationTests
{
    [Fact]
    public async Task DefaultApiTransportFailsClosedWithoutLoadingNativeDecoder()
    {
        await using var factory = new PlantCareApiFactory();
        var processor = factory.Services.GetRequiredService<IPhotoProcessor>();
        var error = await Assert.ThrowsAsync<PhotoProcessingException>(() => processor.NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png"));
        Assert.Equal("photo-processing-unavailable", error.Code);
        Assert.DoesNotContain(typeof(PlantCare.Infrastructure.DependencyInjection).Assembly.GetReferencedAssemblies(),
            reference => reference.Name is "PlantCare.PhotoProcessing" || reference.Name!.StartsWith("Magick.NET"));
        Assert.DoesNotContain(typeof(UnixPhotoProcessor).Assembly.GetReferencedAssemblies(),
            reference => reference.Name is "PlantCare.PhotoProcessing" || reference.Name!.StartsWith("Magick.NET"));
    }
}
