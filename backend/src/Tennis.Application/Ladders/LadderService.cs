using Microsoft.EntityFrameworkCore;
using Tennis.Application.Auth;
using Tennis.Application.Data;
using Tennis.Domain.Entities;

namespace Tennis.Application.Ladders;

public sealed class LadderService(AppDbContext dbContext)
{
    public async Task<LadderResult<LadderSummary>> CreateAsync(
        CurrentUserContext currentUser,
        CreateLadderRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return LadderResult<LadderSummary>.Failure(
                "validation_error",
                "The ladder could not be created.",
                new Dictionary<string, string[]> { ["name"] = ["Enter a ladder name."] });
        }

        if (name.Length > 120)
        {
            return LadderResult<LadderSummary>.Failure(
                "validation_error",
                "The ladder could not be created.",
                new Dictionary<string, string[]> { ["name"] = ["Use 120 characters or fewer."] });
        }

        var ladder = new Ladder
        {
            Name = name,
            CreatedByUserId = currentUser.Id,
            Memberships =
            [
                new LadderMembership
                {
                    UserId = currentUser.Id,
                    Role = LadderMembershipRole.Organizer,
                    DisplayName = currentUser.DisplayName
                }
            ]
        };

        dbContext.Ladders.Add(ladder);
        await dbContext.SaveChangesAsync(cancellationToken);

        return LadderResult<LadderSummary>.Success(ToSummary(ladder, currentUser.Id));
    }

    public async Task<IReadOnlyList<LadderSummary>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ladders = await dbContext.Ladders
            .Where(ladder => ladder.Memberships.Any(membership => membership.UserId == userId))
            .Include(ladder => ladder.Memberships)
            .AsNoTracking()
            .OrderByDescending(ladder => ladder.CreatedUtc)
            .ToListAsync(cancellationToken);

        return ladders.Select(ladder => ToSummary(ladder, userId)).ToList();
    }

    public async Task<LadderResult<LadderSetup>> GetSetupAsync(
        Guid ladderId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ladder = await dbContext.Ladders
            .Include(item => item.Memberships)
            .ThenInclude(membership => membership.User)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ladderId, cancellationToken);

        if (ladder is null)
        {
            return LadderResult<LadderSetup>.Failure("not_found", "Ladder not found.");
        }

        var isOrganizer = ladder.Memberships.Any(membership =>
            membership.UserId == userId && membership.Role == LadderMembershipRole.Organizer);

        if (!isOrganizer)
        {
            return LadderResult<LadderSetup>.Failure(
                "forbidden",
                "Only a ladder organizer can access setup.");
        }

        var players = ladder.Memberships
            .Where(membership => membership.Role == LadderMembershipRole.Player)
            .OrderBy(membership => membership.Position)
            .Select(membership => new LadderPlayerSummary(
                membership.Id,
                membership.UserId,
                membership.DisplayName,
                membership.User.Email,
                membership.Position!.Value))
            .ToList();

        return LadderResult<LadderSetup>.Success(new LadderSetup(
            ladder.Id,
            ladder.Name,
            ladder.Status.ToString(),
            players));
    }

    private static LadderSummary ToSummary(Ladder ladder, Guid userId)
    {
        var roles = ladder.Memberships
            .Where(membership => membership.UserId == userId)
            .Select(membership => membership.Role.ToString())
            .OrderBy(role => role)
            .ToList();

        return new LadderSummary(
            ladder.Id,
            ladder.Name,
            ladder.Status.ToString(),
            roles,
            ladder.Memberships.Count(membership => membership.Role == LadderMembershipRole.Player));
    }
}
