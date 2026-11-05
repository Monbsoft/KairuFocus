namespace KairuFocus.Domain.Community;

/// <summary>The sprint a member can join through another member's name.</summary>
public sealed record JoinableSprint(Guid RootSessionId, DateTime EndsAtUtc);
