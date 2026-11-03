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

        if (!await AntiforgeryRequestPolicy.RequiresValidationAsync(
                httpContext))
        {
            return;
        }

        try
        {
            // Never extract form tokens before streamed-upload admission.
            if (context.Filters.OfType<StreamingPhotoUploadAttribute>().Any()
                && string.IsNullOrWhiteSpace(httpContext.Request.Headers["X-XSRF-TOKEN"]))
                throw new AntiforgeryValidationException("A header token is required.");
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
