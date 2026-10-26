namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>XP of the previous period up to the same moment. PreviousCutoffAt is the exclusive end of that window (UTC).</summary>
public sealed record XpComparisonViewModel(int PreviousXp, DateTimeOffset PreviousCutoffAt, int DeltaXp);
