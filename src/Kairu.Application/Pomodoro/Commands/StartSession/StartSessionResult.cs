using Kairu.Application.Pomodoro.Common;

namespace Kairu.Application.Pomodoro.Commands.StartSession;

public sealed record StartSessionResult
{
    public bool IsSuccess { get; init; }
    public PomodoroSessionViewModel? Session { get; init; }
    public string? Error { get; init; }

    private StartSessionResult() { }

    public static StartSessionResult Success(PomodoroSessionViewModel session) =>
        new() { IsSuccess = true, Session = session };

    public static StartSessionResult Failure(string error) =>
        new() { Error = error };
}
