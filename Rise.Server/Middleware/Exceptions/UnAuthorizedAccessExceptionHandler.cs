using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Rise.Server.Middleware;

internal sealed class UnauthorizedAccessExceptionHandler : IExceptionHandler
{
    private readonly ILogger<UnauthorizedAccessExceptionHandler> _logger;

    public UnauthorizedAccessExceptionHandler(ILogger<UnauthorizedAccessExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedAccessException unauthorizedAccessException)
        {
            return false;
        }

        _logger.LogError(
            unauthorizedAccessException,
            "Exception occurred: {Message}",
            unauthorizedAccessException.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = unauthorizedAccessException.Message
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}