namespace Tennis.Domain.Entities;

public sealed class Ladder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public LadderStatus Status { get; set; } = LadderStatus.Draft;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LaunchedUtc { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<LadderMembership> Memberships { get; set; } = new List<LadderMembership>();
}
