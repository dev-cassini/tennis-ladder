namespace Tennis.Application.Auth;

public sealed record Auth0UserInfo(
    string? Sub,
    string? Email,
    string? Name);
