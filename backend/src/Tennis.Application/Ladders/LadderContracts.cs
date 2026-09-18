namespace Tennis.Application.Ladders;

public sealed record CreateLadderRequest(string Name);

public sealed record DraftPlayerRequest(string DisplayName, string Email);

public sealed record ReplacePlayersRequest(IReadOnlyList<DraftPlayerRequest> Players);

public sealed record UpdatePlayerOrderRequest(IReadOnlyList<Guid> MembershipIds);

public sealed record LadderSummary(
    Guid Id,
    string Name,
    string Status,
    IReadOnlyList<string> Roles,
    int PlayerCount);

public sealed record LadderPlayerSummary(
    Guid MembershipId,
    Guid UserId,
    string DisplayName,
    string Email,
    int Position);

public sealed record LadderSetup(
    Guid Id,
    string Name,
    string Status,
    IReadOnlyList<LadderPlayerSummary> Players);

public sealed record LadderError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public sealed record LadderResult<T>(T? Value, LadderError? Error)
{
    public bool IsSuccess => Error is null;

    public static LadderResult<T> Success(T value) => new(value, null);

    public static LadderResult<T> Failure(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new(default, new LadderError(code, message, validationErrors));
}
