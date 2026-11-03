using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Api.Security;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;
using PlantCare.Infrastructure.Identity;
using PlantCare.Infrastructure.Persistence;
using PlantCare.Infrastructure.Photos;
using PlantCare.PhotoTransport;

namespace PlantCare.Api.IntegrationTests;

public sealed class PhotoUploadTests : IAsyncLifetime
{
    private const string Password="SyntheticPhotoUpload9!";
    private readonly FakeProcessor processor = new();
    private readonly BodyProbe probe = new();
    private readonly PlantCareApiFactory factory;
    public PhotoUploadTests() => factory = new(useRealAuthentication:true,
        pushTestSqlPassword:Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD"),
        customizeServices:services=>{
            services.RemoveAll<PhotoJournalOptions>();services.AddSingleton(new PhotoJournalOptions(true));
            services.RemoveAll<IPhotoProcessor>();services.AddSingleton<IPhotoProcessor>(processor);
            services.AddSingleton<IStartupFilter>(new ProbeStartup(probe));
        });
    public Task InitializeAsync()=>factory.ResetDatabaseAsync();
    public async Task DisposeAsync(){try{await factory.DeletePushTestDatabaseAsync();}finally{await factory.DisposeAsync();}}
    private async Task<(HttpClient Client,Guid Owner,Guid Plant)> Setup()
    {
        await using var scope=factory.Services.CreateAsyncScope();
        var email=Guid.NewGuid().ToString("N")+"@example.test";
        var user=new ApplicationUser{Email=email,UserName=email,EmailConfirmed=true};
        Assert.True((await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().CreateAsync(user,Password)).Succeeded);
        var species=PlantSpecies.Create("Upload fixture",null,"Test",SunlightRequirement.BrightIndirectLight,"Test",7,"Test",null,null,"Test",null,null,null,false);
        var plant=UserPlant.Create(user.Id,species.Id,"Upload plant",null,null,null,DateTimeOffset.UtcNow);
        await factory.SeedAsync(db=>{db.PlantSpecies.Add(species);db.UserPlants.Add(plant);return Task.CompletedTask;});
        var client=factory.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
        await client.AddAntiforgeryTokenAsync();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true",new{email,password=Password})).EnsureSuccessStatusCode();
        await client.AddAntiforgeryTokenAsync();return(client,user.Id,plant.Id);
    }
    private static MultipartFormDataContent Form(Guid id, string mime="image/png",byte[]? bytes=null)
    {
        var form=new MultipartFormDataContent();form.Add(new StringContent(id.ToString()),"requestId");
        form.Add(new StringContent(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")),"photographedOn");
        form.Add(new StringContent("Private upload note"),"notes");
        var file=new ByteArrayContent(bytes??[1,2,3]);file.Headers.ContentType=new MediaTypeHeaderValue(mime);form.Add(file,"file","ignored-original-name.png");return form;
    }

    [Fact]
    public async Task UploadAndRequestLookupAreIdempotentAndOwnerOnly()
    {
        var a=await Setup();var b=await Setup();using var owner=a.Client;using var other=b.Client;
        var id=Guid.NewGuid();var url=$"/api/my-plants/{a.Plant}/photos";
        var first=await owner.PostAsync(url,Form(id));first.EnsureSuccessStatusCode();
        var item=(await first.Content.ReadFromJsonAsync<PhotoEntryDto>())!;
        var duplicate=await owner.PostAsync(url,Form(id));duplicate.EnsureSuccessStatusCode();
        Assert.Equal(item.Id,(await duplicate.Content.ReadFromJsonAsync<PhotoEntryDto>())!.Id);
        Assert.Equal(item.Id,(await owner.GetFromJsonAsync<PhotoEntryDto>(url+"/requests/"+id))!.Id);
        Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(url+"/requests/"+id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await other.PostAsync(url,Form(Guid.NewGuid()))).StatusCode);
        await factory.SeedAsync(async db=>{Assert.Single(await db.PlantPhotoEntries.ToArrayAsync());Assert.Single(await db.PlantPhotoMedia.ToArrayAsync());Assert.Empty(await db.Notifications.ToArrayAsync());});
    }

    [Fact]
    public async Task MissingHeaderForeignOwnerArchiveAndBusyRejectBeforeReadingAnyBody()
    {
        var a=await Setup();var b=await Setup();using var owner=a.Client;using var other=b.Client;
        var url=$"/api/my-plants/{a.Plant}/photos";
        owner.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsync(url,Form(Guid.NewGuid()))).StatusCode);Assert.Equal(0,probe.Reads);
        await owner.AddAntiforgeryTokenAsync();
        Assert.Equal(HttpStatusCode.NotFound,(await other.PostAsync(url,Form(Guid.NewGuid()))).StatusCode);Assert.Equal(0,probe.Reads);
        using(var lease=factory.Services.GetRequiredService<PhotoUploadAdmission>().TryEnter())
        {Assert.NotNull(lease);Assert.Equal((HttpStatusCode)429,(await owner.PostAsync(url,Form(Guid.NewGuid()))).StatusCode);Assert.Equal(0,probe.Reads);}
        await factory.SeedAsync(async db=>(await db.UserPlants.SingleAsync(p=>p.Id==a.Plant)).Archive());
        Assert.Equal(HttpStatusCode.Conflict,(await owner.PostAsync(url,Form(Guid.NewGuid()))).StatusCode);Assert.Equal(0,probe.Reads);Assert.Equal(0,processor.Calls);
    }

    [Theory]
    [InlineData("unsupported",415)]
    [InlineData("oversize",413)]
    [InlineData("duplicate",400)]
    public async Task InvalidFormsNeverReachDecoderOrWriteRows(string mode,int status)
    {
        var a=await Setup();using var owner=a.Client;var form=Form(Guid.NewGuid(),mode=="unsupported"?"image/svg+xml":"image/png",
            mode=="oversize"?new byte[8*1024*1024+1]:null);
        if(mode=="duplicate")form.Add(new StringContent("again"),"notes");
        Assert.Equal((HttpStatusCode)status,(await owner.PostAsync($"/api/my-plants/{a.Plant}/photos",form)).StatusCode);
        Assert.Equal(0,processor.Calls);await factory.SeedAsync(async db=>Assert.Empty(await db.PlantPhotoEntries.ToArrayAsync()));
    }

    [Fact]
    public async Task ChunkedEpilogueCannotBypassTheTotalRequestLimit()
    {
        var a=await Setup();using var owner=a.Client;using var form=Form(Guid.NewGuid());
        var frame=await form.ReadAsByteArrayAsync();var bytes=new byte[frame.Length+9*1024*1024];frame.CopyTo(bytes,0);
        using var content=new UnknownLengthContent(bytes);content.Headers.ContentType=form.Headers.ContentType;
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await owner.PostAsync($"/api/my-plants/{a.Plant}/photos",content)).StatusCode);
        Assert.Equal(0,processor.Calls);await factory.SeedAsync(async db=>Assert.Empty(await db.PlantPhotoEntries.ToArrayAsync()));
    }

    [Fact]
    public async Task DecoderFailureIsSanitizedAndReleasesAdmissionWithoutRows()
    {
        var a=await Setup();using var owner=a.Client;processor.FailureCode="private implementation detail";
        var response=await owner.PostAsync($"/api/my-plants/{a.Plant}/photos",Form(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        Assert.DoesNotContain("private implementation",await response.Content.ReadAsStringAsync());
        using var lease=factory.Services.GetRequiredService<PhotoUploadAdmission>().TryEnter();Assert.NotNull(lease);
        await factory.SeedAsync(async db=>Assert.Empty(await db.PlantPhotoEntries.ToArrayAsync()));
    }

    [Fact]
    public async Task ErasedOwnerCannotBeSavedAfterDecodingAndAdmissionIsReleased()
    {
        var a=await Setup();using var owner=a.Client;processor.Block=true;
        var request=owner.PostAsync($"/api/my-plants/{a.Plant}/photos",Form(Guid.NewGuid()));
        await processor.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await factory.SeedAsync(db=>{db.AccountErasures.Add(AccountErasure.Create(a.Owner,DateTimeOffset.UtcNow));return Task.CompletedTask;});
        processor.Release.TrySetResult();Assert.Equal(HttpStatusCode.NotFound,(await request).StatusCode);
        using var lease=factory.Services.GetRequiredService<PhotoUploadAdmission>().TryEnter();Assert.NotNull(lease);
        await factory.SeedAsync(async db=>Assert.Empty(await db.PlantPhotoMedia.IgnoreQueryFilters().ToArrayAsync()));
    }

    private sealed class FakeProcessor:IPhotoProcessor
    {
        public int Calls;public bool Block;public string? FailureCode;
        public TaskCompletionSource Started{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<ProcessedPhoto> NormalizeAsync(ReadOnlyMemory<byte> input,string mime,CancellationToken ct=default)
        {Interlocked.Increment(ref Calls);Started.TrySetResult();if(FailureCode is not null)throw new PhotoProcessingException(FailureCode);if(Block)await Release.Task.WaitAsync(ct);return new([255,216,1,2,255,217],[255,216,1,2,255,217],8,4);}
    }
    private sealed class BodyProbe { public int Reads; }
    private sealed class UnknownLengthContent(byte[] bytes):HttpContent
    {
        protected override bool TryComputeLength(out long length){length=0;return false;}
        protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context)=>stream.WriteAsync(bytes).AsTask();
    }
    private sealed class ProbeStartup(BodyProbe probe):IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)=>app=>{
            app.Use((context,continuation)=>{if(context.Request.Method=="POST"&&context.Request.Path.Value!.EndsWith("/photos"))context.Request.Body=new ProbeStream(context.Request.Body,probe);return continuation();});next(app);
        };
    }
    private sealed class ProbeStream(Stream inner,BodyProbe probe):Stream
    {
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override int Read(byte[] b,int o,int c){Interlocked.Increment(ref probe.Reads);return inner.Read(b,o,c);}
        public override Task<int> ReadAsync(byte[] b,int o,int c,CancellationToken ct){Interlocked.Increment(ref probe.Reads);return inner.ReadAsync(b,o,c,ct);}
        public override ValueTask<int> ReadAsync(Memory<byte> b,CancellationToken ct=default){Interlocked.Increment(ref probe.Reads);return inner.ReadAsync(b,ct);}
        public override void Flush()=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();
        public override void SetLength(long v)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
    }
}
