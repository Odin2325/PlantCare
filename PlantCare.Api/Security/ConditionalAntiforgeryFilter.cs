using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PlantCare.Api.Security;

public sealed class ConditionalAntiforgeryFilter(
    IAntiforgery antiforgery)
    : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(
        AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        if (!AntiforgeryRequestPolicy.RequiresValidation(
                httpContext.Request))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Antiforgery validation failed.",
                Detail = "The request did not contain a valid antiforgery token."
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }
}
