using ImageMagick;

namespace PlantCare.PhotoProcessing;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length != 2 || args[0] is not ("--normalize" or "--decode-child"))
                throw new PhotoValidationException("photo-processing-unavailable");
            SandboxRequirements.Verify();
            NormalizedPhoto photo;
            if (args[0] == "--decode-child")
            {
                var policy = Path.Combine(AppContext.BaseDirectory, "policy");
                if (!File.Exists(Path.Combine(policy, "policy.xml")))
                    throw new PhotoValidationException("photo-sandbox-unavailable");
                MagickNET.SetEnvironmentVariable("MAGICK_CONFIGURE_PATH", policy);
                photo = await PhotoNormalizer.NormalizeAsync(Console.OpenStandardInput(), args[1]);
            }
            else
            {
                byte[] input;
                try
                {
                    input = await PhotoProcessSupervisor.ReadBoundedAsync(Console.OpenStandardInput(), PhotoNormalizer.MaximumInputBytes)
                        .WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException) { throw new PhotoValidationException("photo-processing-timeout"); }
                var command = new PhotoProcessCommand(Environment.ProcessPath!,
                    new[] { typeof(Program).Assembly.Location, "--decode-child" });
                var supervisor = new PhotoProcessSupervisor(command, TimeSpan.FromSeconds(10));
                photo = await supervisor.NormalizeAsync(input, args[1]);
            }
            try
            {
                await Console.OpenStandardOutput().WriteAsync(PhotoProcessSupervisor.EncodeResponse(photo))
                    .AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException) { throw new PhotoValidationException("photo-processing-timeout"); }
            return 0;
        }
        catch (PhotoValidationException error) { Console.Error.WriteLine(error.Code); return 2; }
        catch { Console.Error.WriteLine("photo-processing-failed"); return 3; }
    }
}
