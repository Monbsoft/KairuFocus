namespace Kairudev.Infrastructure.Persistence.Internal;

/// <summary>EF Core row for the singleton PomodoroSettings (id always = 1).</summary>
internal sealed class PomodoroSettingsRow
{
    public int Id { get; set; } = 1;
    public int SprintDurationMinutes { get; set; }
    public int ShortBreakDurationMinutes { get; set; }
    public int LongBreakDurationMinutes { get; set; }
}
