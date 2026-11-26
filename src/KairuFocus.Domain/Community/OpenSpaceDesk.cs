namespace KairuFocus.Domain.Community;

/// <summary>
/// A "bureau" of the open space: an open sprint or group seen with the members seated at it.
/// <see cref="Seated"/>[0] is the host, then the active members by arrival, then by name (ordinal).
/// <see cref="RootSessionId"/> stays on the server side, never in the API.
/// </summary>
public sealed record OpenSpaceDesk(
    Guid RootSessionId,
    IReadOnlyList<SprintMember> Seated,
    string? HostAmbianceCode,
    DateTime EndsAtUtc,
    GroupStepType StepType,
    GroupPhase Phase,
    bool IsClosedToJoin);
