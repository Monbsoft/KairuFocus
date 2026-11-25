using KairuFocus.Web.Services;

namespace KairuFocus.Web.Helpers;

public static class SessionClock
{
    public static DateTime? DeadlineUtc(PomodoroSessionDto s)
    {
        if (s.SharedEndAt is { } shared) return AsUtc(shared);
        if (s.PlannedDurationMinutes == 0 || s.StartedAt is not { } started) return null;
        return AsUtc(started).AddMinutes(s.PlannedDurationMinutes);
    }

    public static int RemainingSeconds(DateTime deadlineUtc, DateTime nowUtc) =>
        (int)Math.Max(0, Math.Ceiling((deadlineUtc - nowUtc).TotalSeconds));

    public static int ElapsedSeconds(DateTime startedAtUtc, DateTime nowUtc) =>
        (int)Math.Max(0, Math.Floor((nowUtc - startedAtUtc).TotalSeconds));

    public static string Format(int totalSeconds)
    {
        var t = Math.Max(0, totalSeconds);
        var h = t / 3600;
        var m = t % 3600 / 60;
        var s = t % 60;
        return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    private static DateTime AsUtc(DateTime d) =>
        d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d;
}
