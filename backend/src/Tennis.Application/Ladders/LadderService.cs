using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Tennis.Application.Auth;
using Tennis.Application.Data;
using Tennis.Domain.Entities;

namespace Tennis.Application.Ladders;

public sealed class LadderService(AppDbContext dbContext)
{
    private static readonly EmailAddressAttribute EmailValidator = new();

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

        if (ladder.Status != LadderStatus.Draft)
        {
            return LadderResult<LadderSetup>.Failure(
                "ladder_active",
                "This ladder has already launched.");
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

    public async Task<LadderResult<LadderSetup>> ReplacePlayersAsync(
        Guid ladderId,
        Guid userId,
        ReplacePlayersRequest request,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await GetOrganizerDraftAsync(ladderId, userId, cancellationToken);

        if (!accessResult.IsSuccess)
        {
            return LadderResult<LadderSetup>.Failure(
                accessResult.Error!.Code,
                accessResult.Error.Message);
        }

        var requestedPlayers = request.Players ?? [];
        var validationErrors = ValidatePlayers(requestedPlayers);

        if (validationErrors.Count > 0)
        {
            return LadderResult<LadderSetup>.Failure(
                "validation_error",
                "Check the player list and try again.",
                validationErrors);
        }

        var normalizedEmails = requestedPlayers
            .Select(player => NormalizeEmail(player.Email))
            .ToList();
        var users = await dbContext.Users
            .Where(user => normalizedEmails.Contains(user.Email.ToLower()))
            .ToListAsync(cancellationToken);
        var usersByEmail = users
            .GroupBy(user => NormalizeEmail(user.Email))
            .ToDictionary(group => group.Key, group => group.First());

        for (var index = 0; index < requestedPlayers.Count; index++)
        {
            var email = normalizedEmails[index];

            if (!usersByEmail.ContainsKey(email))
            {
                validationErrors[$"players.{index}.email"] =
                    ["This email does not belong to an existing Tennis Ladder account."];
            }
        }

        if (validationErrors.Count > 0)
        {
            return LadderResult<LadderSetup>.Failure(
                "validation_error",
                "Every player must have an existing account.",
                validationErrors);
        }

        var ladder = accessResult.Value!;
        var existingPlayers = ladder.Memberships
            .Where(membership => membership.Role == LadderMembershipRole.Player)
            .ToList();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.LadderMemberships.RemoveRange(existingPlayers);
        await dbContext.SaveChangesAsync(cancellationToken);

        var newMemberships = requestedPlayers
            .Select((requestedPlayer, index) => new LadderMembership
            {
                LadderId = ladder.Id,
                UserId = usersByEmail[normalizedEmails[index]].Id,
                Role = LadderMembershipRole.Player,
                DisplayName = requestedPlayer.DisplayName.Trim(),
                Position = index + 1
            })
            .ToList();

        dbContext.LadderMemberships.AddRange(newMemberships);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetSetupAsync(ladderId, userId, cancellationToken);
    }

    public async Task<LadderResult<LadderSetup>> UpdatePlayerOrderAsync(
        Guid ladderId,
        Guid userId,
        UpdatePlayerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await GetOrganizerDraftAsync(ladderId, userId, cancellationToken);

        if (!accessResult.IsSuccess)
        {
            return LadderResult<LadderSetup>.Failure(
                accessResult.Error!.Code,
                accessResult.Error.Message);
        }

        var ladder = accessResult.Value!;
        var players = ladder.Memberships
            .Where(membership => membership.Role == LadderMembershipRole.Player)
            .ToList();
        var requestedIds = request.MembershipIds ?? [];
        var requestedIdSet = requestedIds.ToHashSet();
        var existingIdSet = players.Select(player => player.Id).ToHashSet();

        if (requestedIds.Count != players.Count ||
            requestedIdSet.Count != requestedIds.Count ||
            !requestedIdSet.SetEquals(existingIdSet))
        {
            return LadderResult<LadderSetup>.Failure(
                "validation_error",
                "The saved order must contain every player exactly once.",
                new Dictionary<string, string[]>
                {
                    ["membershipIds"] = ["Use every player exactly once."]
                });
        }

        var playersById = players.ToDictionary(player => player.Id);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        for (var index = 0; index < requestedIds.Count; index++)
        {
            playersById[requestedIds[index]].Position = requestedIds.Count + index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < requestedIds.Count; index++)
        {
            playersById[requestedIds[index]].Position = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetSetupAsync(ladderId, userId, cancellationToken);
    }

    public async Task<LadderResult<LadderDetail>> LaunchAsync(
        Guid ladderId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await GetOrganizerDraftAsync(ladderId, userId, cancellationToken);

        if (!accessResult.IsSuccess)
        {
            return LadderResult<LadderDetail>.Failure(
                accessResult.Error!.Code,
                accessResult.Error.Message);
        }

        var ladder = accessResult.Value!;
        var players = ladder.Memberships
            .Where(membership => membership.Role == LadderMembershipRole.Player)
            .OrderBy(membership => membership.Position)
            .ToList();

        if (players.Count < 2)
        {
            return LadderResult<LadderDetail>.Failure(
                "launch_requirements_not_met",
                "Add at least two players before launching the ladder.");
        }

        var expectedPositions = Enumerable.Range(1, players.Count);
        var actualPositions = players.Select(player => player.Position!.Value);

        if (!actualPositions.SequenceEqual(expectedPositions))
        {
            return LadderResult<LadderDetail>.Failure(
                "launch_requirements_not_met",
                "Save a complete starting order before launching the ladder.");
        }

        ladder.Status = LadderStatus.Active;
        ladder.LaunchedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(ladderId, userId, cancellationToken);
    }

    public async Task<LadderResult<LadderDetail>> GetDetailAsync(
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
            return LadderResult<LadderDetail>.Failure("not_found", "Ladder not found.");
        }

        var currentMemberships = ladder.Memberships
            .Where(membership => membership.UserId == userId)
            .ToList();

        if (currentMemberships.Count == 0)
        {
            return LadderResult<LadderDetail>.Failure(
                "forbidden",
                "You are not a member of this ladder.");
        }

        var isOrganizer = currentMemberships.Any(
            membership => membership.Role == LadderMembershipRole.Organizer);

        if (ladder.Status == LadderStatus.Draft && !isOrganizer)
        {
            return LadderResult<LadderDetail>.Failure(
                "forbidden",
                "Players can view the ladder after it launches.");
        }

        var standings = ladder.Memberships
            .Where(membership => membership.Role == LadderMembershipRole.Player)
            .OrderBy(membership => membership.Position)
            .Select(membership => new LadderStanding(
                membership.Id,
                membership.UserId,
                membership.DisplayName,
                membership.Position!.Value,
                membership.UserId == userId))
            .ToList();
        var roles = currentMemberships
            .Select(membership => membership.Role.ToString())
            .OrderBy(role => role)
            .ToList();

        return LadderResult<LadderDetail>.Success(new LadderDetail(
            ladder.Id,
            ladder.Name,
            ladder.Status.ToString(),
            ladder.LaunchedUtc,
            roles,
            standings));
    }

    private async Task<LadderResult<Ladder>> GetOrganizerDraftAsync(
        Guid ladderId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var ladder = await dbContext.Ladders
            .Include(item => item.Memberships)
            .SingleOrDefaultAsync(item => item.Id == ladderId, cancellationToken);

        if (ladder is null)
        {
            return LadderResult<Ladder>.Failure("not_found", "Ladder not found.");
        }

        var isOrganizer = ladder.Memberships.Any(membership =>
            membership.UserId == userId && membership.Role == LadderMembershipRole.Organizer);

        if (!isOrganizer)
        {
            return LadderResult<Ladder>.Failure(
                "forbidden",
                "Only a ladder organizer can change setup.");
        }

        if (ladder.Status != LadderStatus.Draft)
        {
            return LadderResult<Ladder>.Failure(
                "ladder_active",
                "An active ladder can no longer be changed through setup.");
        }

        return LadderResult<Ladder>.Success(ladder);
    }

    private static Dictionary<string, string[]> ValidatePlayers(
        IReadOnlyList<DraftPlayerRequest> players)
    {
        var errors = new Dictionary<string, string[]>();
        var emailIndexes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < players.Count; index++)
        {
            var player = players[index];
            var displayName = player.DisplayName?.Trim() ?? string.Empty;
            var email = player.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors[$"players.{index}.displayName"] = ["Enter the player's name."];
            }
            else if (displayName.Length > 200)
            {
                errors[$"players.{index}.displayName"] = ["Use 200 characters or fewer."];
            }

            if (!EmailValidator.IsValid(email))
            {
                errors[$"players.{index}.email"] = ["Enter a valid email address."];
                continue;
            }

            var normalizedEmail = NormalizeEmail(email);

            if (!emailIndexes.TryGetValue(normalizedEmail, out var indexes))
            {
                indexes = [];
                emailIndexes[normalizedEmail] = indexes;
            }

            indexes.Add(index);
        }

        foreach (var duplicate in emailIndexes.Where(pair => pair.Value.Count > 1))
        {
            foreach (var index in duplicate.Value)
            {
                errors[$"players.{index}.email"] = ["Each player email can appear only once."];
            }
        }

        return errors;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

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
