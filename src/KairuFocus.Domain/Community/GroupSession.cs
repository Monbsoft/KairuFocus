using KairuFocus.Domain.Common;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

/// <summary>
/// A group session driven by its host: a chain of steps (sprints and breaks) the host launches one after
/// the other. Members follow the current step. The host and the members stay in private fields: no read
/// exposes a user id (ADR-026).
/// </summary>
/// <remarks>
/// <see cref="Version"/> is a concurrency token. It only grows on writes of the group row itself
/// (<see cref="LaunchStep"/>, <see cref="Stop"/>, <see cref="EndIfHostAbsent"/>, <see cref="EndAsHostAlone"/>,
/// <see cref="Close"/>, <see cref="Reopen"/>), never on <see cref="Join"/> or <see cref="Leave"/> which only
/// write member rows: simultaneous arrivals do not conflict.
/// </remarks>
public sealed class GroupSession : AggregateRoot<Guid>
{
    private readonly List<GroupMember> _members = [];
    private UserId _hostId = default!;

    // Required by EF Core. AggregateRoot has no parameterless constructor.
    private GroupSession() : base(Guid.Empty)
    {
        CurrentStep = default!;
    }

    private GroupSession(Guid id, UserId hostId, GroupStep firstStep)
        : base(id)
    {
        _hostId = hostId;
        CurrentStep = firstStep;
        SprintCount = 1;
    }

    public GroupStep CurrentStep { get; private set; }
    public int SprintCount { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public GroupSessionEndReason? EndReason { get; private set; }
    public bool IsClosedToJoin { get; private set; }
    public int Version { get; private set; }

    public int ActiveMemberCount => _members.Count(member => member.IsActive);

    /// <summary>Creates the group when its first member joins the host's first sprint.</summary>
    public static Result<GroupSession> Start(
        Guid id,
        UserId hostId,
        DateTime firstSprintStartedAtUtc,
        DateTime firstSprintEndsAtUtc,
        UserId firstMemberId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(hostId);
        ArgumentNullException.ThrowIfNull(firstMemberId);

        if (hostId == firstMemberId)
            return Result.Failure<GroupSession>(DomainErrors.Community.CannotJoinOwnSprint);

        if (nowUtc >= firstSprintEndsAtUtc)
            return Result.Failure<GroupSession>(DomainErrors.Community.GroupEnded);

        var step = GroupStep.Create(1, GroupStepType.Sprint, firstSprintStartedAtUtc, firstSprintEndsAtUtc);
        if (step.IsFailure)
            return Result.Failure<GroupSession>(step.Error);

        var group = new GroupSession(id, hostId, step.Value);
        group._members.Add(new GroupMember(firstMemberId, nowUtc));
        return Result.Success(group);
    }

    public bool IsHostedBy(UserId userId) => _hostId == userId;

    public bool IsActiveMember(UserId userId) =>
        _members.Any(member => member.IsActive && member.IsUser(userId));

    /// <summary>State at <paramref name="nowUtc"/>. A host absence that is not recorded yet is reported
    /// (end = <see cref="GroupRules.HostAbsentAt"/>) without modifying the group.</summary>
    public GroupSessionState StateAt(DateTime nowUtc)
    {
        if (EndedAtUtc is not null)
            return new GroupSessionState(GroupPhase.Ended, CurrentStep, EndedAtUtc, EndReason);

        var phase = GroupRules.PhaseAt(CurrentStep.EndsAtUtc, null, nowUtc);
        return phase == GroupPhase.Ended
            ? new GroupSessionState(phase, CurrentStep, GroupRules.HostAbsentAt(CurrentStep.EndsAtUtc), GroupSessionEndReason.HostAbsent)
            : new GroupSessionState(phase, CurrentStep, null, null);
    }

    /// <summary>Adds a member, or brings back one who left. Only possible while a step is running.</summary>
    public Result Join(UserId userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(userId);

        switch (StateAt(nowUtc).Phase)
        {
            case GroupPhase.Ended:
                return Result.Failure(DomainErrors.Community.GroupEnded);
            case GroupPhase.WaitingForHost:
                return Result.Failure(DomainErrors.Community.GroupWaitingForHost);
        }

        if (IsHostedBy(userId))
            return Result.Failure(DomainErrors.Community.CannotJoinOwnSprint);

        if (IsClosedToJoin)
            return Result.Failure(DomainErrors.Community.GroupClosed);

        var member = _members.Find(m => m.IsUser(userId));
        if (member is null)
            _members.Add(new GroupMember(userId, nowUtc));
        else if (member.IsActive)
            return Result.Failure(DomainErrors.Community.AlreadyInGroup);
        else
            member.Return(nowUtc);

        return Result.Success();
    }

    /// <summary>Idempotent for a member who already left.</summary>
    public Result Leave(UserId userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var member = _members.Find(m => m.IsUser(userId));
        if (member is null)
            return Result.Failure(DomainErrors.Community.NotInGroup);

        if (member.IsActive)
            member.Leave(nowUtc);

        return Result.Success();
    }

    /// <summary>Launches the next step. Only possible once the current one is over and the host is not absent.</summary>
    public Result LaunchStep(GroupStepType type, DateTime startedAtUtc, DateTime endsAtUtc, DateTime nowUtc)
    {
        switch (StateAt(nowUtc).Phase)
        {
            case GroupPhase.Ended:
                return Result.Failure(DomainErrors.Community.GroupEnded);
            case GroupPhase.Running:
                return Result.Failure(DomainErrors.Community.GroupStepRunning);
        }

        var step = GroupStep.Create(CurrentStep.Number + 1, type, startedAtUtc, endsAtUtc);
        if (step.IsFailure)
            return Result.Failure(step.Error);

        CurrentStep = step.Value;
        if (type == GroupStepType.Sprint)
            SprintCount++;
        Version++;
        return Result.Success();
    }

    /// <summary>Ends the group on the host's decision. <paramref name="reason"/> is
    /// <see cref="GroupSessionEndReason.Stopped"/> or <see cref="GroupSessionEndReason.HostLeftCommunity"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Any other reason: a programmer error.</exception>
    public Result Stop(GroupSessionEndReason reason, DateTime nowUtc)
    {
        if (reason is not (GroupSessionEndReason.Stopped or GroupSessionEndReason.HostLeftCommunity))
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Only Stopped and HostLeftCommunity can be requested.");

        if (StateAt(nowUtc).Phase == GroupPhase.Ended)
            return Result.Failure(DomainErrors.Community.GroupEnded);

        End(reason, nowUtc);
        return Result.Success();
    }

    /// <summary>Records the end of the group when the host launched nothing within
    /// <see cref="GroupRules.HostWaitLimit"/>. Returns false when there is nothing to record.</summary>
    public bool EndIfHostAbsent(DateTime nowUtc)
    {
        if (EndedAtUtc is not null || GroupRules.PhaseAt(CurrentStep.EndsAtUtc, null, nowUtc) != GroupPhase.Ended)
            return false;

        End(GroupSessionEndReason.HostAbsent, GroupRules.HostAbsentAt(CurrentStep.EndsAtUtc));
        return true;
    }

    /// <summary>Ends the group when the host waits for the next step and every member has left.</summary>
    public Result EndAsHostAlone(DateTime nowUtc)
    {
        switch (StateAt(nowUtc).Phase)
        {
            case GroupPhase.Ended:
                return Result.Failure(DomainErrors.Community.GroupEnded);
            case GroupPhase.Running:
                return Result.Failure(DomainErrors.Community.GroupStepRunning);
        }

        if (ActiveMemberCount > 0)
            return Result.Failure(DomainErrors.Community.GroupNotAlone);

        End(GroupSessionEndReason.HostAlone, nowUtc);
        return Result.Success();
    }

    /// <summary>Stops new members from joining. Idempotent.</summary>
    public Result Close() => SetClosedToJoin(true);

    /// <summary>Lets new members join again. Idempotent.</summary>
    public Result Reopen() => SetClosedToJoin(false);

    private Result SetClosedToJoin(bool isClosed)
    {
        if (EndedAtUtc is not null)
            return Result.Failure(DomainErrors.Community.GroupEnded);

        // Nothing to write when the value does not change: no version bump, no needless conflict.
        if (IsClosedToJoin != isClosed)
        {
            IsClosedToJoin = isClosed;
            Version++;
        }

        return Result.Success();
    }

    private void End(GroupSessionEndReason reason, DateTime endedAtUtc)
    {
        EndedAtUtc = endedAtUtc;
        EndReason = reason;
        Version++;
    }
}
