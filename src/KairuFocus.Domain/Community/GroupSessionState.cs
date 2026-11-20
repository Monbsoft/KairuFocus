namespace KairuFocus.Domain.Community;

/// <summary>State of a group session at a given instant, including an end that is only computed
/// (host absent) and not recorded yet.</summary>
public sealed record GroupSessionState(
    GroupPhase Phase,
    GroupStep Step,
    DateTime? EndedAtUtc,
    GroupSessionEndReason? EndReason);
