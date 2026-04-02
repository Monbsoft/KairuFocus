namespace Kairu.Domain.Pomodoro;

public sealed record PomodoroSessionId(Guid Value)
{
    public static PomodoroSessionId New() => new(Guid.NewGuid());
    public static PomodoroSessionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
