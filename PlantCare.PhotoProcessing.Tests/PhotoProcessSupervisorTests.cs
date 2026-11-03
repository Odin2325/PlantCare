using System.Diagnostics;
using PhotoProcessFixture;
using PlantCare.PhotoProcessing;

namespace PlantCare.PhotoProcessing.Tests;

[CollectionDefinition("photo-process", DisableParallelization = true)]
public sealed class PhotoProcessCollection;

[Collection("photo-process")]
public sealed class PhotoProcessSupervisorTests
{
    [Fact]
    public async Task SuccessfulResultIsBoundedAndUsesFreshChildOnEachRequest()
    {
        var pidFile = Path.GetTempFileName();
        try
        {
            var supervisor = Create("success", pidFile);
            var first = await supervisor.NormalizeAsync(new byte[100], "image/png");
            var firstPid = File.ReadAllText(pidFile);
            AssertDead(pidFile);
            var second = await supervisor.NormalizeAsync(new byte[100], "image/png");
            AssertDead(pidFile);
            Assert.NotEqual(firstPid, File.ReadAllText(pidFile));
            Assert.Equal(20u, first.Width);
            Assert.Equal(10u, second.Height);
            Assert.Equal(new byte[] {255,216,255,217}, first.Image);
        }
        finally { File.Delete(pidFile); }
    }

    [Theory]
    [InlineData("crash")]
    [InlineData("private-error")]
    [InlineData("bad-header")]
    [InlineData("bad-size")]
    [InlineData("extra")]
    [InlineData("flood")]
    [InlineData("stderr-flood")]
    public async Task ChildFailureAndUnboundedOutputAreSanitizedAndAdmissionIsReusable(string mode)
    {
        var error = await Assert.ThrowsAsync<PhotoValidationException>(() => Create(mode).NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png"));
        Assert.Equal("photo-processing-failed", error.Code);
        Assert.Equal(error.Code, error.Message);
        await Create("success").NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png");
    }

    [Fact]
    public async Task StartFailureIsSanitizedAndDoesNotPoisonAdmission()
    {
        var supervisor = new PhotoProcessSupervisor(new PhotoProcessCommand(
            Path.Combine(Path.GetTempPath(), "nonexistent-photo-worker-" + Guid.NewGuid().ToString("N")), Array.Empty<string>()), TimeSpan.FromSeconds(2));
        var error = await Assert.ThrowsAsync<PhotoValidationException>(() => supervisor.NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png"));
        Assert.Equal("photo-processing-unavailable", error.Code);
        await Create("success").NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png");
    }

    [Fact]
    public async Task KnownValidationErrorSurvivesBoundary()
    {
        var error = await Assert.ThrowsAsync<PhotoValidationException>(() => Create("error").NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png"));
        Assert.Equal("photo-invalid", error.Code);
    }

    [Fact]
    public async Task ChildDoesNotInheritParentSecrets()
    {
        var previous = Environment.GetEnvironmentVariable("PLANTCARE_TEST_SECRET");
        try
        {
            Environment.SetEnvironmentVariable("PLANTCARE_TEST_SECRET", "synthetic-test-only");
            await Create("environment").NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png");
        }
        finally { Environment.SetEnvironmentVariable("PLANTCARE_TEST_SECRET", previous); }
    }

    [Theory]
    [InlineData("hang", 0)]
    [InlineData("block-input", 8 * 1024 * 1024)]
    public async Task DeadlineKillsAndReapsEvenWhenChildNeverReadsInput(string mode, int inputSize)
    {
        var pidFile = Path.GetTempFileName();
        try
        {
            var error = await Assert.ThrowsAsync<PhotoValidationException>(() =>
                Create(mode, pidFile, TimeSpan.FromSeconds(2)).NormalizeAsync(new byte[inputSize], "image/png"));
            Assert.Equal("photo-processing-timeout", error.Code);
            AssertDead(pidFile);
            await Create("success").NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png");
        }
        finally { File.Delete(pidFile); }
    }

    [Fact]
    public async Task CancellationKillsChildAndBusyRequestDoesNotQueue()
    {
        var pidFile = Path.GetTempFileName();
        try
        {
            using var cancel = new CancellationTokenSource();
            var run = Create("hang", pidFile).NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png", cancel.Token);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (new FileInfo(pidFile).Length == 0) await Task.Delay(20, deadline.Token);
            var busy = await Assert.ThrowsAsync<PhotoValidationException>(() => Create("success").NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png"));
            Assert.Equal("photo-busy", busy.Code);
            cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            AssertDead(pidFile);
            await Create("success").NormalizeAsync(ReadOnlyMemory<byte>.Empty, "image/png");
        }
        finally { File.Delete(pidFile); }
    }

    private static void AssertDead(string path)
    {
        var id = int.Parse(File.ReadAllText(path));
        try { using var process = Process.GetProcessById(id); Assert.True(process.HasExited); }
        catch (ArgumentException) { /* No process with this ID remains. */ }
    }

    private static PhotoProcessSupervisor Create(string mode, string? pidFile = null, TimeSpan? timeout = null)
    {
        var dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
            ?? (OperatingSystem.IsWindows() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe") : "/usr/share/dotnet/dotnet");
        var runtime = Path.ChangeExtension(typeof(PhotoProcessSupervisorTests).Assembly.Location, ".runtimeconfig.json");
        var arguments = new List<string> { "exec", "--runtimeconfig", runtime, typeof(FixtureMarker).Assembly.Location, mode };
        if (pidFile is not null) arguments.Add(pidFile);
        return new(new PhotoProcessCommand(dotnet, arguments), timeout ?? TimeSpan.FromSeconds(10));
    }
}
