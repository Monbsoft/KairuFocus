namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>One bar of the progression chart, labelled by the UTC start of its week or month.</summary>
public sealed record XpBarViewModel(DateOnly PeriodStart, int Xp, bool IsCurrent, bool IsRecord);
