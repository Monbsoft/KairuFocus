namespace KairuFocus.Application.Community.Commands.LeaveCommunity;

public sealed record LeaveCommunityResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    private LeaveCommunityResult() { }

    public static LeaveCommunityResult Success() => new() { IsSuccess = true };
    public static LeaveCommunityResult Failure(string error) => new() { Error = error };
}
