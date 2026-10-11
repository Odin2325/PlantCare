using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace PlantCare.Api.Security;

internal static class AntiforgeryRequestPolicy
{
    private static readonly HashSet<string> SafeMethods =
    [
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    ];

    public static async ValueTask<bool> RequiresValidationAsync(HttpContext context)
    {
        var request = context.Request;
        if (SafeMethods.Contains(request.Method))
        {
            return false;
        }

        var authorization = request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(
            "Bearer ",
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // A header alone does not prove bearer authentication. In particular,
        // anonymous Identity endpoints must not accept an invalid token as a
        // substitute for antiforgery validation.
        var result = await context.AuthenticateAsync(IdentityConstants.BearerScheme);
        return !result.Succeeded;
    }
}
