using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tennis.Application.Auth;
using Tennis.Application.Data;
using Tennis.Application.Ladders;
using Tennis.Domain.Entities;

namespace Tennis.Application.Tests.Ladders;

public sealed class LadderServiceTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly AppDbContext dbContext;
    private readonly LadderService service;

    public LadderServiceTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new AppDbContext(options);
        dbContext.Database.EnsureCreated();
        service = new LadderService(dbContext);
    }

    [Fact]
    public async Task CreateAsync_AssignsOrganizerMembership_ToCreatingUser()
    {
        var organizer = await AddUserAsync("organizer@example.com", "Olivia Organizer");

        var result = await service.CreateAsync(
            ToCurrentUser(organizer),
            new CreateLadderRequest("Friday Night Singles"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Friday Night Singles", result.Value!.Name);
        Assert.Equal("Draft", result.Value.Status);
        Assert.Contains("Organizer", result.Value.Roles);

        var membership = await dbContext.LadderMemberships.SingleAsync();
        Assert.Equal(organizer.Id, membership.UserId);
        Assert.Equal(LadderMembershipRole.Organizer, membership.Role);
    }

    [Fact]
    public async Task ReplacePlayersAsync_RejectsDuplicateEmails()
    {
        var organizer = await AddUserAsync("organizer@example.com", "Olivia Organizer");
        var player = await AddUserAsync("player@example.com", "Pat Player");
        var ladder = await CreateLadderAsync(organizer);

        var result = await service.ReplacePlayersAsync(
            ladder.Id,
            organizer.Id,
            new ReplacePlayersRequest(
            [
                new DraftPlayerRequest("Pat One", player.Email),
                new DraftPlayerRequest("Pat Two", player.Email.ToUpperInvariant())
            ]));

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_error", result.Error!.Code);
        Assert.Contains("players.0.email", result.Error.ValidationErrors!.Keys);
        Assert.Contains("players.1.email", result.Error.ValidationErrors.Keys);
    }

    [Fact]
    public async Task ReplacePlayersAsync_RejectsEmailWithoutExistingAccount()
    {
        var organizer = await AddUserAsync("organizer@example.com", "Olivia Organizer");
        var ladder = await CreateLadderAsync(organizer);

        var result = await service.ReplacePlayersAsync(
            ladder.Id,
            organizer.Id,
            new ReplacePlayersRequest(
            [
                new DraftPlayerRequest("Missing Player", "missing@example.com")
            ]));

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_error", result.Error!.Code);
        Assert.Equal(
            "This email does not belong to an existing Tennis Ladder account.",
            result.Error.ValidationErrors!["players.0.email"].Single());
    }

    [Fact]
    public async Task ReplacePlayersAsync_ForbidsNonOrganizer()
    {
        var organizer = await AddUserAsync("organizer@example.com", "Olivia Organizer");
        var otherUser = await AddUserAsync("other@example.com", "Other User");
        var ladder = await CreateLadderAsync(organizer);

        var result = await service.ReplacePlayersAsync(
            ladder.Id,
            otherUser.Id,
            new ReplacePlayersRequest([]));

        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task UpdatePlayerOrderAsync_PersistsEveryPlayerInRequestedOrder()
    {
        var organizer = await AddUserAsync("organizer@example.com", "Olivia Organizer");
        var firstPlayer = await AddUserAsync("first@example.com", "First Player");
        var secondPlayer = await AddUserAsync("second@example.com", "Second Player");
        var ladder = await CreateLadderAsync(organizer);
        var setup = await AddPlayersAsync(ladder.Id, organizer.Id, firstPlayer, secondPlayer);
        var reversedIds = setup.Players
            .OrderByDescending(player => player.Position)
            .Select(player => player.MembershipId)
            .ToList();

        var result = await service.UpdatePlayerOrderAsync(
            ladder.Id,
            organizer.Id,
            new UpdatePlayerOrderRequest(reversedIds));

        Assert.True(result.IsSuccess);
        Assert.Equal("Second Player", result.Value!.Players[0].DisplayName);
        Assert.Equal(1, result.Value.Players[0].Position);
        Assert.Equal("First Player", result.Value.Players[1].DisplayName);
        Assert.Equal(2, result.Value.Players[1].Position);
    }

    [Fact]
    public async Task LaunchAsync_RejectsLadderWithFewerThanTwoPlayers()
    {
        var organizer = await AddUserAsync("organizer@example.com", "Olivia Organizer");
        var player = await AddUserAsync("player@example.com", "Pat Player");
        var ladder = await CreateLadderAsync(organizer);
        await AddPlayersAsync(ladder.Id, organizer.Id, player);

        var result = await service.LaunchAsync(ladder.Id, organizer.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("launch_requirements_not_met", result.Error!.Code);
    }

    [Fact]
    public async Task LaunchAsync_MakesOrderedStandingsVisibleToPlayers()
    {
        var organizer = await AddUserAsync("organizer@example.com", "Olivia Organizer");
        var firstPlayer = await AddUserAsync("first@example.com", "First Player");
        var secondPlayer = await AddUserAsync("second@example.com", "Second Player");
        var outsider = await AddUserAsync("outsider@example.com", "Outside User");
        var ladder = await CreateLadderAsync(organizer);
        await AddPlayersAsync(ladder.Id, organizer.Id, firstPlayer, secondPlayer);

        var launchResult = await service.LaunchAsync(ladder.Id, organizer.Id);
        var playerResult = await service.GetDetailAsync(ladder.Id, secondPlayer.Id);
        var outsiderResult = await service.GetDetailAsync(ladder.Id, outsider.Id);

        Assert.True(launchResult.IsSuccess);
        Assert.Equal("Active", launchResult.Value!.Status);
        Assert.NotNull(launchResult.Value.LaunchedUtc);
        Assert.Equal(["First Player", "Second Player"],
            launchResult.Value.Standings.Select(standing => standing.DisplayName));

        Assert.True(playerResult.IsSuccess);
        Assert.True(playerResult.Value!.Standings.Single(
            standing => standing.UserId == secondPlayer.Id).IsCurrentUser);

        Assert.False(outsiderResult.IsSuccess);
        Assert.Equal("forbidden", outsiderResult.Error!.Code);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }

    private async Task<User> AddUserAsync(string email, string displayName)
    {
        var user = new User { Email = email, DisplayName = displayName };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<LadderSummary> CreateLadderAsync(User organizer)
    {
        var result = await service.CreateAsync(
            ToCurrentUser(organizer),
            new CreateLadderRequest("Test Ladder"));
        return result.Value!;
    }

    private async Task<LadderSetup> AddPlayersAsync(
        Guid ladderId,
        Guid organizerId,
        params User[] players)
    {
        var result = await service.ReplacePlayersAsync(
            ladderId,
            organizerId,
            new ReplacePlayersRequest(players
                .Select(player => new DraftPlayerRequest(player.DisplayName, player.Email))
                .ToList()));
        return result.Value!;
    }

    private static CurrentUserContext ToCurrentUser(User user) =>
        new(user.Id, user.Email, user.DisplayName, $"auth0|{user.Id}", "auth0");
}
