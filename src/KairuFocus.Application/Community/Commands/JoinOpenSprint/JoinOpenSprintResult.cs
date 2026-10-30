namespace KairuFocus.Application.Community.Commands.JoinOpenSprint;

public sealed record JoinOpenSprintResult
{
    public bool IsSuccess { get; init; }
    public DateTimeOffset? SharedEndAt { get; init; }
    public string? Error { get; init; }

    private JoinOpenSprintResult() { }

    public static JoinOpenSprintResult Success(DateTimeOffset sharedEndAt) =>
        new() { IsSuccess = true, SharedEndAt = sharedEndAt };

    public static JoinOpenSprintResult Failure(string error) =>
        new() { Error = error };
}
