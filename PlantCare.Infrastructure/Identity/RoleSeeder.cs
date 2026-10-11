using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace PlantCare.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        const string adminRoleName = "Admin";

        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(adminRoleName));
            if (!result.Succeeded)
                throw new InvalidOperationException("Admin role creation failed: " +
                    string.Join(", ", result.Errors.Select(error => error.Code)));
        }
    }
}
