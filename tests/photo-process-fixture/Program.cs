using System.Buffers.Binary;
using System.Diagnostics;

namespace PhotoProcessFixture;

public static class FixtureMarker
{
    public static async Task<int> Main(string[] args)
    {
        var mode = args[0];
        if (args.Length >= 3) File.WriteAllText(args[1], Environment.ProcessId.ToString());
        if (mode == "hang" || mode == "block-input")
        {
            File.WriteAllText(args[1], Environment.ProcessId.ToString());
            await Task.Delay(Timeout.Infinite);
            return 0;
        }
        if (mode == "environment" && Environment.GetEnvironmentVariable("PLANTCARE_TEST_SECRET") is not null) return 5;
        if (mode == "crash") return 9;
        if (mode == "error") { Console.Error.WriteLine("photo-invalid"); return 2; }
        if (mode == "private-error") { Console.Error.WriteLine("private detail must not escape"); return 2; }
        if (mode is "flood" or "stderr-flood")
        {
            var stream = mode == "flood" ? Console.OpenStandardOutput() : Console.OpenStandardError();
            var block = new byte[8192];
            while (true) await stream.WriteAsync(block);
        }
        using var input = new MemoryStream();
        await Console.OpenStandardInput().CopyToAsync(input);
        var bytes = new byte[28];
        "PCP1"u8.CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 20);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 10);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), 4);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 4);
        new byte[] {255,216,255,217,255,216,255,217}.CopyTo(bytes, 20);
        if (mode == "bad-header") bytes[0] = 0;
        if (mode == "bad-size") BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), int.MaxValue);
        if (mode == "extra") bytes = [..bytes, 0];
        await Console.OpenStandardOutput().WriteAsync(bytes);
        return 0;
    }
}
