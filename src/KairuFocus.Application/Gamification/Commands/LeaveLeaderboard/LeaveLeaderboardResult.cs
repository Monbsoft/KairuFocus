namespace KairuFocus.Application.Gamification.Commands.LeaveLeaderboard;

public sealed record LeaveLeaderboardResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    private LeaveLeaderboardResult() { }

    public static LeaveLeaderboardResult Success() => new() { IsSuccess = true };
    public static LeaveLeaderboardResult Failure(string error) => new() { Error = error };
}
