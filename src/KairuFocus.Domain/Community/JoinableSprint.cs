namespace KairuFocus.Domain.Community;

/// <summary>
/// The sprint or group session a member can reach through another member's name: its root, the end and the
/// type of its current step, and whether that step is running or the group waits for its host.
/// </summary>
public sealed record JoinableSprint(Guid RootSessionId, DateTime EndsAtUtc, GroupStepType StepType, GroupPhase Phase);
