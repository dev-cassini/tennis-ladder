namespace Tennis.Domain.Entities;

public sealed class LadderMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LadderId { get; set; }
    public Guid UserId { get; set; }
    public LadderMembershipRole Role { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? Position { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public Ladder Ladder { get; set; } = null!;
    public User User { get; set; } = null!;
}
