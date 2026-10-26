namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>
/// Progression of the current user. Calendar dates are UTC <see cref="DateOnly"/> values to display as-is;
/// instants are UTC <see cref="DateTimeOffset"/> values to display in local time.
/// </summary>
/// <param name="WindowStart">Monday / 1st of the month / UTC date of the first gain (global); null for global without any gain.</param>
/// <param name="Comparison">Same moment of the previous period; null for global.</param>
/// <param name="AverageWeeklyXp">Global only; null otherwise or without any gain.</param>
public sealed record GetCommunityProgressResult(
    DateOnly? WindowStart,
    int Xp,
    int Sprints,
    int FocusMinutes,
    XpComparisonViewModel? Comparison,
    int? AverageWeeklyXp,
    AllTimeViewModel AllTime,
    XpRecordViewModel? BestWeek,
    XpRecordViewModel? BestMonth,
    IReadOnlyList<XpBarViewModel> Bars);
