namespace KairuFocus.Application.Community.Commands.LeaveOrStopGroupSession;

public sealed record LeaveOrStopGroupSessionResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    /// <summary>True when the host ended the group for everyone, false when a participant left alone.</summary>
    public bool Stopped { get; init; }

    private LeaveOrStopGroupSessionResult() { }

    public static LeaveOrStopGroupSessionResult Success(bool stopped) =>
        new() { IsSuccess = true, Stopped = stopped };

    public static LeaveOrStopGroupSessionResult Failure(string error) =>
        new() { Error = error };
}
