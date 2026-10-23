namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>Best UTC week (Monday) or month (1st) and its XP.</summary>
public sealed record XpRecordViewModel(DateOnly PeriodStart, int Xp);
