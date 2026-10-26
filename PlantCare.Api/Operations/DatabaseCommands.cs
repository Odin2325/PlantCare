using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PlantCare.Infrastructure.Identity;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.Operations;

public static class DatabaseCommands
{
    private static readonly string[] ApiWrites = ["AspNetUsers", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens",
        "PlantSpecies", "UserPlants", "UserPlantTags", "CareSchedules", "CareEvents", "NotificationPreferences",
        "CalendarSubscriptions", "CalendarShares", "CalendarInvitations", "PushSubscriptions", "ExternalCalendarConnections", "ExternalCalendarEvents"];
    private static readonly string[] ApiReads = ["AspNetRoles", "AspNetRoleClaims", "AspNetUserRoles", "PushDeliveries", "__EFMigrationsHistory"];
    private static readonly string[] WorkerReads = ["UserPlants", "CareSchedules", "NotificationPreferences", "AccountErasures", "PlantSpecies"];
    private static readonly string[] WorkerWrites = ["ExternalCalendarEvents"];

    public static bool IsCommand(string[] args) => args.Length > 0 &&
        args[0] is "--migrate-database" or "--provision-runtime-users" or "--check-runtime-access";

    public static async Task ExecuteAsync(IServiceProvider services, IConfiguration configuration, string[] args)
    {
        var db = services.GetRequiredService<PlantCareDbContext>();
        if (!db.Database.IsSqlServer()) throw new InvalidOperationException("Database operator commands require SQL Server.");
        try
        {
            if (args is ["--migrate-database"])
            {
                await db.Database.MigrateAsync();
                await RoleSeeder.SeedAsync(services);
            }
            else if (args is ["--provision-runtime-users"])
            {
                var apiPassword = configuration["DatabaseRuntime:ApiPassword"];
                var workerPassword = configuration["DatabaseRuntime:WorkerPassword"];
                ValidatePassword(apiPassword); ValidatePassword(workerPassword);
                var operatorPassword = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).Password;
                if (apiPassword == workerPassword || apiPassword == operatorPassword || workerPassword == operatorPassword)
                    throw new ArgumentException("Use different passwords for API, worker and operator.");
                await ProvisionAsync(db, "plantcare_api", apiPassword!, ApiPolicy());
                await ProvisionAsync(db, "plantcare_worker", workerPassword!, WorkerPolicy());
            }
            else if (args is ["--check-runtime-access", "api" or "worker"])
                await CheckAsync(db, args[1]);
            else throw new ArgumentException("Use --migrate-database, --provision-runtime-users or --check-runtime-access api|worker.");
        }
        catch (SqlException exception)
        {
            // SQL/connection errors must not print credentials or SQL parameters.
            throw new InvalidOperationException($"Database operator command failed (SQL error {exception.Number}); credentials withheld.");
        }
    }

    private static Dictionary<string, string[]> ApiPolicy()
    {
        var policy = ApiWrites.ToDictionary(table => table, _ => new[] { "SELECT", "INSERT", "UPDATE", "DELETE" });
        foreach (var table in ApiReads) policy[table] = ["SELECT"];
        policy["HarvestRecords"] = ["SELECT", "INSERT", "DELETE"];
        policy["AccountErasures"] = ["SELECT", "INSERT"];
        policy["Notifications"] = ["SELECT", "INSERT", "UPDATE"];
        return policy;
    }

    private static Dictionary<string, string[]> WorkerPolicy()
    {
        var policy = WorkerWrites.ToDictionary(table => table, _ => new[] { "SELECT", "INSERT", "UPDATE", "DELETE" });
        foreach (var table in WorkerReads) policy[table] = ["SELECT"];
        policy["Notifications"] = ["SELECT", "INSERT", "UPDATE"];
        policy["PushDeliveries"] = ["SELECT", "INSERT", "UPDATE"];
        policy["PushSubscriptions"] = ["SELECT", "DELETE"];
        policy["ExternalCalendarConnections"] = ["SELECT", "UPDATE"];
        return policy;
    }

    private static void ValidatePassword(string? password)
    {
        if (password is null || !Regex.IsMatch(password, "^[A-Za-z0-9!@#%^*_+=.-]{32,128}$"))
            throw new ArgumentException("Runtime passwords must contain 32–128 characters from letters, digits or !@#%^*_+=.-.");
    }

    private static async Task ProvisionAsync(PlantCareDbContext db, string login, string password, Dictionary<string, string[]> policy)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", password);
        command.CommandText = """
            IF EXISTS (SELECT 1 FROM sys.server_role_members WHERE member_principal_id = SUSER_ID(@login))
                THROW 51000, 'Runtime login has server-role membership; review it manually.', 1;
            IF EXISTS (SELECT 1 FROM sys.server_permissions WHERE grantee_principal_id = SUSER_ID(@login) AND permission_name <> 'CONNECT SQL')
                THROW 51000, 'Runtime login has unexpected server permissions.', 1;
            IF EXISTS (SELECT 1 FROM sys.databases WHERE owner_sid = SUSER_SID(@login))
                THROW 51000, 'Runtime login owns a database.', 1;
            IF EXISTS (SELECT 1 FROM sys.database_role_members WHERE member_principal_id = USER_ID(@login))
                THROW 51000, 'Runtime user has database-role membership.', 1;
            IF EXISTS (SELECT 1 FROM sys.schemas WHERE principal_id = USER_ID(@login))
                THROW 51000, 'Runtime user owns a schema.', 1;
            """;
        await command.ExecuteNonQueryAsync();
        // Refuse unmanaged permissions instead of quietly keeping an elevated user.
        command.CommandText = "SELECT class, OBJECT_NAME(major_id), permission_name, state, OBJECT_SCHEMA_NAME(major_id), minor_id FROM sys.database_permissions WHERE grantee_principal_id = USER_ID(@login)";
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var permissionClass = reader.GetByte(0);
                var table = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var permission = reader.GetString(2);
                var state = reader.GetString(3);
                if (state != "G" || !(permissionClass == 0 && permission == "CONNECT" ||
                    permissionClass == 1 && !reader.IsDBNull(4) && reader.GetString(4) == "dbo" && reader.GetInt32(5) == 0 &&
                    policy.TryGetValue(table, out var allowed) && allowed.Contains(permission)))
                    throw new InvalidOperationException("Runtime user has unmanaged permissions; review them before provisioning.");
            }
        }
        command.CommandText = """
            DECLARE @sql nvarchar(max);
            IF SUSER_ID(@login) IS NULL
                SET @sql = N'CREATE LOGIN ' + QUOTENAME(@login) + N' WITH PASSWORD = ' + QUOTENAME(@password, '''') + N', CHECK_POLICY=ON, CHECK_EXPIRATION=OFF';
            ELSE IF PWDCOMPARE(@password, (SELECT password_hash FROM sys.sql_logins WHERE name=@login)) <> 1
                SET @sql = N'ALTER LOGIN ' + QUOTENAME(@login) + N' WITH PASSWORD = ' + QUOTENAME(@password, '''');
            IF @sql IS NOT NULL EXEC sp_executesql @sql;
            IF USER_ID(@login) IS NULL
                SET @sql = N'CREATE USER ' + QUOTENAME(@login) + N' FOR LOGIN ' + QUOTENAME(@login);
            ELSE SET @sql = N'ALTER USER ' + QUOTENAME(@login) + N' WITH LOGIN = ' + QUOTENAME(@login);
            EXEC sp_executesql @sql;
            """;
        await command.ExecuteNonQueryAsync();
        foreach (var (table, permissions) in policy)
        {
            command.CommandText = $"GRANT {string.Join(", ", permissions)} ON OBJECT::[dbo].[{table}] TO [{login}]";
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task CheckAsync(PlantCareDbContext db, string role)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ORIGINAL_LOGIN()";
        if ((string?)await command.ExecuteScalarAsync() != "plantcare_" + role)
            throw new InvalidOperationException("Runtime connection does not use its intended restricted login.");
        command.CommandText = "SELECT IS_SRVROLEMEMBER('sysadmin') + HAS_PERMS_BY_NAME(NULL, NULL, 'ALTER ANY LOGIN') + HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL')";
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 0)
            throw new InvalidOperationException("Runtime connection has administrative permissions.");
        var policy = role == "api" ? ApiPolicy() : WorkerPolicy();
        foreach (var (table, permissions) in policy)
        {
            foreach (var permission in permissions)
            {
                command.CommandText = $"SELECT HAS_PERMS_BY_NAME(N'dbo.{table}', 'OBJECT', N'{permission}')";
                if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1)
                    throw new InvalidOperationException("A required runtime permission is missing.");
            }
        }
        // Actual negative SQL operations: if unexpectedly allowed, no data is
        // modified (DDL rolls back; UPDATE affects zero rows).
        foreach (var sql in new[]
        {
            "BEGIN TRANSACTION; CREATE TABLE dbo.PlantCarePermissionProbe (Id int); ROLLBACK;",
            "UPDATE dbo.__EFMigrationsHistory SET ProductVersion=ProductVersion WHERE 1=0;",
            "INSERT dbo.AspNetUserRoles DEFAULT VALUES;",
            "DELETE dbo.AccountErasures WHERE 1=0;",
            role == "worker" ? "SELECT TOP(1) Id FROM dbo.AspNetUsers;" : "UPDATE dbo.AspNetRoles SET Name=Name WHERE 1=0;",
            role == "worker" ? "UPDATE dbo.PlantSpecies SET CommonName=CommonName WHERE 1=0;" : "UPDATE dbo.AspNetUserRoles SET UserId=UserId WHERE 1=0;"
        })
        {
            command.CommandText = sql;
            var denied = false;
            try { await command.ExecuteNonQueryAsync(); }
            catch (SqlException exception) when (exception.Number is 229 or 262 or 297 or 916) { denied = true; }
            finally
            {
                command.CommandText = "IF @@TRANCOUNT > 0 ROLLBACK;";
                await command.ExecuteNonQueryAsync();
            }
            if (!denied) throw new InvalidOperationException("A protected SQL operation was not denied.");
        }
    }
}
