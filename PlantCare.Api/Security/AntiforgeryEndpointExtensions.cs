using Microsoft.AspNetCore.Antiforgery;

namespace PlantCare.Api.Security;

public static class AntiforgeryEndpointExtensions
{
    public static TBuilder RequireAntiforgeryValidation<TBuilder>(
        this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(
            async (context, next) =>
            {
                if (!AntiforgeryRequestPolicy.RequiresValidation(
                        context.HttpContext.Request))
                {
                    return await next(context);
                }

                var antiforgery =
                    context.HttpContext.RequestServices
                        .GetRequiredService<IAntiforgery>();

                try
                {
                    await antiforgery.ValidateRequestAsync(
                        context.HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    return Results.Problem(
                        statusCode:
                            StatusCodes.Status400BadRequest,
                        title:
                            "Antiforgery validation failed.",
                        detail:
                            "The request did not contain a valid antiforgery token.");
                }

                return await next(context);
            });

        return builder;
    }
}
