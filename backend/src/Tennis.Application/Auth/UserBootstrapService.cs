using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tennis.Application.Data;
using Tennis.Domain.Entities;

namespace Tennis.Application.Auth;

public sealed class UserBootstrapService(
    AppDbContext dbContext)
{
    public async Task<CurrentUserContext> ResolveCurrentUserAsync(
        ClaimsPrincipal principal,
        string? accessToken,
        string auth0Domain,
        CancellationToken cancellationToken = default)
    {
        var subject = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Authenticated user is missing the subject claim.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? string.Empty;

        var displayName = principal.FindFirst("name")?.Value
            ?? principal.Identity?.Name
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName))
        {
            var userInfo = await GetUserInfoAsync(accessToken, auth0Domain, cancellationToken);

            email = string.IsNullOrWhiteSpace(email)
                ? userInfo.Email ?? string.Empty
                : email;

            displayName = string.IsNullOrWhiteSpace(displayName)
                ? userInfo.Name ?? userInfo.Email ?? subject
                : displayName;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Authenticated user is missing the email claim.");
        }

        var provider = subject.Split('|', 2)[0];

        var identity = await dbContext.UserAuthIdentities
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Subject == subject, cancellationToken);

        if (identity is not null)
        {
            identity.User.Email = email;
            identity.User.DisplayName = displayName;
            identity.User.LastSeenUtc = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return CurrentUserContext.FromUser(identity.User, identity);
        }

        var existingUser = await dbContext.Users
            .Include(user => user.AuthIdentities)
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (existingUser is null)
        {
            existingUser = new User
            {
                Email = email,
                DisplayName = displayName
            };

            dbContext.Users.Add(existingUser);
        }
        else
        {
            existingUser.DisplayName = displayName;
            existingUser.LastSeenUtc = DateTimeOffset.UtcNow;
        }

        var authIdentity = new UserAuthIdentity
        {
            User = existingUser,
            Provider = provider,
            Subject = subject
        };

        dbContext.UserAuthIdentities.Add(authIdentity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CurrentUserContext.FromUser(existingUser, authIdentity);
    }

    private async Task<Auth0UserInfo> GetUserInfoAsync(
        string? accessToken,
        string auth0Domain,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Authenticated user is missing the access token.");
        }

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{auth0Domain}/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var userInfo = await response.Content.ReadFromJsonAsync<Auth0UserInfo>(cancellationToken);

        return userInfo
            ?? throw new InvalidOperationException("Auth0 userinfo response was empty.");
    }
}
