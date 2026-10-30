namespace KairuFocus.Web.Services;

public sealed record XpSummaryDto(int TotalXp, int WeekXp);

// ── Community (iteration #42) ───────────────────────────────────────────────
// Mirrors of the API contracts (GET api/gamification/community/*). Calendar dates are UTC
// DateOnly values to display as-is; instants are DateTimeOffset values to display in local time.

/// <summary>GET community/progress. Comparison is null in global; AverageWeeklyXp is global only.</summary>
public sealed record CommunityProgressDto(
    DateOnly? WindowStart,
    int Xp,
    int Sprints,
    int FocusMinutes,
    CommunityComparisonDto? Comparison,
    int? AverageWeeklyXp,
    CommunityAllTimeDto AllTime,
    CommunityRecordDto? BestWeek,
    CommunityRecordDto? BestMonth,
    IReadOnlyList<CommunityBarDto> Bars);

public sealed record CommunityComparisonDto(int PreviousXp, DateTimeOffset PreviousCutoffAt, int DeltaXp);

public sealed record CommunityAllTimeDto(int TotalXp, int Sprints, DateTimeOffset? FirstGainAt);

public sealed record CommunityRecordDto(DateOnly PeriodStart, int Xp);

public sealed record CommunityBarDto(DateOnly PeriodStart, int Xp, bool IsCurrent, bool IsRecord);

/// <summary>GET community/leaderboard. Me is null for a non-participant and for a participant at 0 XP.</summary>
public sealed record CommunityLeaderboardDto(
    bool IsParticipant,
    int RankedCount,
    CommunityStandingDto? Me,
    IReadOnlyList<CommunityRowDto> Top);

public sealed record CommunityStandingDto(int Rank, bool IsTied, int Xp, string Name, string? AvatarUrl, bool IsInTop);

public sealed record CommunityRowDto(int Rank, string Name, string? AvatarUrl, int Xp, bool IsMe);

/// <summary>GET community/recap: four numbers, no identifier.</summary>
public sealed record CommunityRecapDto(int Sprints, int FocusMinutes, int ActiveMembers, int MySprints);

/// <summary>GET community/presence: anonymous counts.</summary>
public sealed record CommunityPresenceDto(int InSprintNow, int SprintedToday);

