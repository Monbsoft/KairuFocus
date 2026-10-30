namespace KairuFocus.Domain.Community;

/// <summary>Sprints completed by a member on one UTC day.</summary>
public sealed record MemberActivityDay(DateOnly DateUtc, int Sprints);

/// <summary>Aggregated activity of a member, readable by any connected member.</summary>
public sealed record MemberActivity(int CompletedSprints, TimeSpan Focus, int SprintsTogether, IReadOnlyList<MemberActivityDay> Days);
