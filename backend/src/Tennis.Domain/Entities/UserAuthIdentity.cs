namespace Tennis.Domain.Entities;

public sealed class UserAuthIdentity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset LinkedUtc { get; set; } = DateTimeOffset.UtcNow;
    public User User { get; set; } = null!;
}
