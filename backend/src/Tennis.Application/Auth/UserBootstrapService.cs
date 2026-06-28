using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tennis.Application.Data;
using Tennis.Domain.Entities;

namespace Tennis.Application.Auth;

public sealed class UserBootstrapService(AppDbContext dbContext)
{
    public async Task<CurrentUserContext> ResolveCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var subject = principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Authenticated user is missing the subject claim.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? throw new InvalidOperationException("Authenticated user is missing the email claim.");

        var displayName = principal.FindFirst("name")?.Value
            ?? principal.Identity?.Name
            ?? email;

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
}
