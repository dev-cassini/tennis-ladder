using Tennis.Application.Auth;

namespace Tennis.Api.Auth;

public sealed class CurrentUserResolver(
    UserBootstrapService userBootstrapService,
    IConfiguration configuration)
{
    public Task<CurrentUserContext> ResolveAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var auth0Domain = configuration["Auth0:Domain"]
            ?? throw new InvalidOperationException("Auth0 domain was not configured.");
        var accessToken = httpContext.Request.Headers.Authorization
            .ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return userBootstrapService.ResolveCurrentUserAsync(
            httpContext.User,
            accessToken,
            auth0Domain,
            cancellationToken);
    }
}
