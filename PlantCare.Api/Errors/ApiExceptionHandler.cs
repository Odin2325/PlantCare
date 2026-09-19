using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PlantCare.Api.Errors;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ArgumentException argumentException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Invalid request.",
                    argumentException.Message),

            InvalidOperationException invalidOperationException =>
                (
                    StatusCodes.Status400BadRequest,
                    "The operation is not valid.",
                    invalidOperationException.Message),

            DbUpdateException =>
                (
                    StatusCodes.Status409Conflict,
                    "The requested change conflicts with existing data.",
                    "The operation could not be completed because related data exists or the data changed."),

            _ =>
                (
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.",
                    "The server could not complete the request.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed with status code {StatusCode} while processing {Method} {Path}.",
                statusCode,
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType =
            "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}
