namespace KairuFocus.Web.Services;

public sealed record PomodoroSettingsDto(
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes,
    int SprintsBeforeLongBreak,
    int DailySprintGoal);

public sealed record FocusSummaryDto(
    int SprintsToday,
    int FocusMinutesToday,
    int DailySprintGoal,
    int Streak);

public sealed record PomodoroSessionDto(
    Guid Id,
    string SessionType,
    string Status,
    int PlannedDurationMinutes,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<Guid> LinkedTaskIds,
    double? DurationSeconds,
    DateTime? SharedEndAt,
    bool IsGroupHost,
    bool IsClosedToJoin);

public sealed record SuggestedSessionTypeDto(
    string SuggestedType,
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes);

public sealed record FocusStatsDto(IReadOnlyList<FocusDayDto> Days);

public sealed record FocusDayDto(DateOnly Date, int SprintCount, int FocusMinutes);

public sealed record CompleteSessionResponseDto(int XpAwarded);

/// <summary>Result of a start-session/start-free-sprint call: either the created session
/// (<see cref="Session"/> set, <see cref="Error"/> null) or a user-facing French error message
/// (<see cref="Session"/> null, <see cref="Error"/> set) — never both.</summary>
public sealed record StartSessionOutcome(PomodoroSessionDto? Session, string? Error);

/// <summary>Result of a current-session lookup that, unlike <c>GetCurrentSessionAsync</c>, tells
/// "the server answered" from "the call failed". <see cref="Succeeded"/> is <c>true</c> when the
/// server answered: <see cref="Session"/> is then the active session, or <c>null</c> for a 204
/// (no active session). <see cref="Succeeded"/> is <c>false</c> on a non-2xx status, a network
/// error, a timeout or an unparsable body (<see cref="Session"/> is then <c>null</c> and says
/// nothing about the server state). <see cref="Unauthorized"/> is <c>true</c> only for a 401
/// (expired token), which is a failed call (<see cref="Succeeded"/> <c>false</c>), not a network
/// problem.</summary>
public sealed record CurrentSessionLookup(PomodoroSessionDto? Session, bool Succeeded, bool Unauthorized = false);
