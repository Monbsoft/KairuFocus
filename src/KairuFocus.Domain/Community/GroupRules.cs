namespace KairuFocus.Domain.Community;

/// <summary>Timing rules of a group session, shared by the aggregate and the reads.</summary>
public static class GroupRules
{
    /// <summary>How long after the end of a step the host may still launch the next one.</summary>
    public static readonly TimeSpan HostWaitLimit = TimeSpan.FromMinutes(15);

    /// <summary>Instant at which the group ends if the host launched nothing since the end of the step.</summary>
    public static DateTime HostAbsentAt(DateTime stepEndsAtUtc) => stepEndsAtUtc + HostWaitLimit;

    public static GroupPhase PhaseAt(DateTime stepEndsAtUtc, DateTime? endedAtUtc, DateTime nowUtc)
    {
        if (endedAtUtc is not null)
            return GroupPhase.Ended;

        if (nowUtc < stepEndsAtUtc)
            return GroupPhase.Running;

        return nowUtc < HostAbsentAt(stepEndsAtUtc) ? GroupPhase.WaitingForHost : GroupPhase.Ended;
    }
}
