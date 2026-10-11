using Microsoft.Data.Sqlite;

namespace PlantCare.Api.IntegrationTests;

public sealed class SqliteRuntimeTests
{
    [Fact]
    public async Task NativeEngineMeetsSecurityBaseline()
    {
        // CVE-2025-6965 affects SQLite before 3.50.2. Check the native engine
        // actually loaded by the test process, not just the managed package version.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version()";
        var versionText = (string)(await command.ExecuteScalarAsync())!;

        Assert.True(Version.Parse(versionText) >= new Version(3, 50, 2),
            $"SQLite {versionText} is below the patched security baseline 3.50.2.");
    }
}
