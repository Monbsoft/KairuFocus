using Kairu.Application.Pomodoro.Common;

namespace Kairu.Application.Pomodoro.Queries.GetCurrentSession;

public sealed record GetCurrentSessionResult
{
    public PomodoroSessionViewModel? Session { get; init; }
    public bool HasSession => Session is not null;

    private GetCurrentSessionResult() { }

    public static GetCurrentSessionResult WithSession(PomodoroSessionViewModel session) =>
        new() { Session = session };

    public static GetCurrentSessionResult NoSession() =>
        new();
}
