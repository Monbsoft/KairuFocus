namespace KairuFocus.Domain.Gamification;

/// <summary>
/// Anonymous aggregate over the completed sprints of all members (ADR-024):
/// number of sprints and sum of <see cref="CommunityRules.CountedFocus"/>.
/// </summary>
public sealed record CompletedSprintTotals(int Sprints, TimeSpan Focus);
