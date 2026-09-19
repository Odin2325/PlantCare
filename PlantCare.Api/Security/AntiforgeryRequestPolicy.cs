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

    public static bool RequiresValidation(HttpRequest request)
    {
        if (SafeMethods.Contains(request.Method))
        {
            return false;
        }

        var authorization = request.Headers.Authorization.ToString();

        return !authorization.StartsWith(
            "Bearer ",
            StringComparison.OrdinalIgnoreCase);
    }
}
