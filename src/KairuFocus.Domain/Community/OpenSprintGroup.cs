namespace KairuFocus.Domain.Community;

/// <summary>A sprint open to members. <see cref="RootSessionId"/> stays on the server side, never in the API.</summary>
public sealed record OpenSprintGroup(Guid RootSessionId, SprintMember Lead, DateTime EndsAtUtc, int ParticipantCount);
