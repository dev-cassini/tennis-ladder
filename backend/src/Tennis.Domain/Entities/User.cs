namespace Tennis.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<UserAuthIdentity> AuthIdentities { get; set; } = new List<UserAuthIdentity>();
    public ICollection<Ladder> CreatedLadders { get; set; } = new List<Ladder>();
    public ICollection<LadderMembership> LadderMemberships { get; set; } = new List<LadderMembership>();
}
