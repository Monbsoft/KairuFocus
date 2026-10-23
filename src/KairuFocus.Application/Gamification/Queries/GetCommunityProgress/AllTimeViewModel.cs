namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>Total XP, completed sprints since the first XP gain, and the instant of that first gain (UTC; null without any gain).</summary>
public sealed record AllTimeViewModel(int TotalXp, int Sprints, DateTimeOffset? FirstGainAt);
