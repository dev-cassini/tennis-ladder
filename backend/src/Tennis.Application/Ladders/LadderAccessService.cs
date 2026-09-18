using Microsoft.EntityFrameworkCore;
using Tennis.Application.Data;
using Tennis.Domain.Entities;

namespace Tennis.Application.Ladders;

public sealed class LadderAccessService(AppDbContext dbContext)
{
    public Task<bool> IsOrganizerAsync(
        Guid ladderId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.LadderMemberships.AnyAsync(
            membership =>
                membership.LadderId == ladderId &&
                membership.UserId == userId &&
                membership.Role == LadderMembershipRole.Organizer,
            cancellationToken);

    public Task<bool> IsParticipantAsync(
        Guid ladderId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.LadderMemberships.AnyAsync(
            membership => membership.LadderId == ladderId && membership.UserId == userId,
            cancellationToken);
}
