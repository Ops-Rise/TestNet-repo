using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public class OwnDataOrAdminHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<OwnDataOrAdminHandler> logger
) : AuthorizationHandler<OwnDataOrAdminRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<OwnDataOrAdminHandler> _logger = logger;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnDataOrAdminRequirement requirement
    )
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Haal de Auth0UserId van de ingelogde gebruiker op uit de claims
        var auth0UserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("auth0UserId: {Auth0UserIdClaim}", auth0UserIdClaim);
        if (auth0UserIdClaim == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Haal de ID uit de routeparameters op (bijvoorbeeld voor /users/{auth0UserId})
        var routeData = httpContext?.Request.RouteValues;
        if (routeData == null || !routeData.ContainsKey("auth0UserId"))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var routeId = routeData["auth0UserId"]?.ToString();
        _logger.LogInformation("routeId: {RouteId}", routeId);

        // Controleer of de gebruiker een Admin is of eigenaar van de gegevens
        if (context.User.IsInRole("Administrator") || auth0UserIdClaim == routeId)
        {
            _logger.LogInformation("User is admin or owner of data.");
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogInformation("User is not admin or owner of data.");
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
