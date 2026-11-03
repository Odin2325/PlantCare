namespace PlantCare.PhotoProcessing;

internal static class SandboxRequirements
{
    public static void Verify()
    {
        try
        {
            if (!OperatingSystem.IsLinux()) throw new InvalidOperationException();
            var status = File.ReadAllLines("/proc/self/status");
            var uid = status.Single(line => line.StartsWith("Uid:")).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (uid.Skip(1).Any(value => value == "0")) throw new InvalidOperationException();
            if (!status.Contains("NoNewPrivs:\t1") || !status.Contains("CapEff:\t0000000000000000")
                || !status.Contains("CapBnd:\t0000000000000000") || !status.Contains("Seccomp:\t2")) throw new InvalidOperationException();
            var rootMount = File.ReadAllLines("/proc/self/mountinfo").First(line => line.Split(' ')[4] == "/").Split(' ');
            if (!rootMount[5].Split(',').Contains("ro")) throw new InvalidOperationException();
            if (Directory.GetDirectories("/sys/class/net").Any(path => Path.GetFileName(path) != "lo")) throw new InvalidOperationException();
            var memory = ReadLimit("memory.max");
            var swap = ReadLimit("memory.swap.max");
            var pids = ReadLimit("pids.max");
            var cpu = File.ReadAllText("/sys/fs/cgroup/cpu.max").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (memory > 384L * 1024 * 1024 || memory < 64L * 1024 * 1024 || swap != 0
                || pids is < 1 or > 64 || cpu.Length != 2 || !long.TryParse(cpu[0], out var quota)
                || !long.TryParse(cpu[1], out var period) || quota <= 0 || period <= 0 || quota > period)
                throw new InvalidOperationException();
        }
        catch { throw new PhotoValidationException("photo-sandbox-unavailable"); }
    }

    private static long ReadLimit(string file) => long.Parse(File.ReadAllText("/sys/fs/cgroup/" + file).Trim(), System.Globalization.CultureInfo.InvariantCulture);
}
