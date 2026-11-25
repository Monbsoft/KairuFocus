namespace KairuFocus.Web.Helpers;

public static class PomodoroAutoStart
{
    public const string QueryName = "start";
    public const string SprintValue = "sprint";

    public static bool ShouldStart(string? startQuery, bool hadSessionAtLoad, bool groupLive) =>
        string.Equals(startQuery, SprintValue, StringComparison.OrdinalIgnoreCase) && !hadSessionAtLoad && !groupLive;
}
