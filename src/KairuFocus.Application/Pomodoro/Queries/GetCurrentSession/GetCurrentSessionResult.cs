using KairuFocus.Application.Pomodoro.Common;

namespace KairuFocus.Application.Pomodoro.Queries.GetCurrentSession;

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
