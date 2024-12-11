using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mollie.Api.Client;

namespace Rise.Server.Middleware;

internal sealed class MollieApiExceptionHandler(ILogger<MollieApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not MollieApiException mollieException)
        {
            return false;
        }

        logger.LogError(mollieException, "Mollie API exception occurred: {Title} - {Detail}", mollieException.Details.Title, mollieException.Details.Detail);

        var (statusCode, errorMessage) = mollieException.Details.Status switch
        {
            404 => (StatusCodes.Status404NotFound, "De betaling kon niet worden gevonden."),
            401 => (StatusCodes.Status401Unauthorized, "U bent onbevoegd voor deze aanvraag."),
            400 => (StatusCodes.Status400BadRequest, "Fout in aanvraag. Controleer de verzonden gegevens."),
            _ => (StatusCodes.Status500InternalServerError, "Er is een fout opgetreden, contacteer support.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "Payment Provider Error",
            Detail = errorMessage
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}