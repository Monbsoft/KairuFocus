namespace KairuFocus.Application.Gamification.Commands.JoinLeaderboard;

public sealed record JoinLeaderboardResult
{
    public bool IsSuccess { get; init; }
    public string? Pseudonym { get; init; }
    public string? Error { get; init; }

    private JoinLeaderboardResult() { }

    public static JoinLeaderboardResult Success(string pseudonym) => new() { IsSuccess = true, Pseudonym = pseudonym };
    public static JoinLeaderboardResult Failure(string error) => new() { Error = error };
}
