namespace KairuFocus.Application.Community.Commands.SetSprintClosed;

public sealed record SetSprintClosedResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    private SetSprintClosedResult() { }

    public static SetSprintClosedResult Success() =>
        new() { IsSuccess = true };

    public static SetSprintClosedResult Failure(string error) =>
        new() { Error = error };
}
