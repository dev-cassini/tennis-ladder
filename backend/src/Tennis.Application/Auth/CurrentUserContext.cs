using Tennis.Domain.Entities;

namespace Tennis.Application.Auth;

public sealed record CurrentUserContext(
    Guid Id,
    string Email,
    string DisplayName,
    string Subject,
    string Provider)
{
    public static CurrentUserContext FromUser(User user, UserAuthIdentity identity) =>
        new(user.Id, user.Email, user.DisplayName, identity.Subject, identity.Provider);
}
