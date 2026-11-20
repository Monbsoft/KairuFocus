using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Domain.Tests.Community;

public sealed class GroupSessionTests
{
    private static readonly UserId H = UserId.New();
    private static readonly UserId M = UserId.New();
    private static readonly UserId N = UserId.New();

    private static readonly DateTime SprintStart = At(9, 50);
    private static readonly DateTime SprintEnd = At(10, 15);

    private static DateTime At(int hour, int minute, int second = 0) =>
        new(2026, 11, 8, hour, minute, second, DateTimeKind.Utc);

    /// <summary>Group started by M at 09:50, first sprint until 10:15, host H.</summary>
    private static GroupSession NewGroup() =>
        GroupSession.Start(Guid.NewGuid(), H, SprintStart, SprintEnd, M, SprintStart).Value;

    // ── Start ──────────────────────────────────────────────────────────────

    [Fact]
    public void Should_StartWithFirstSprint_When_MemberJoins()
    {
        var result = GroupSession.Start(Guid.NewGuid(), H, SprintStart, SprintEnd, M, SprintStart);

        Assert.True(result.IsSuccess);
        var group = result.Value;
        Assert.Equal(1, group.CurrentStep.Number);
        Assert.Equal(GroupStepType.Sprint, group.CurrentStep.Type);
        Assert.Equal(SprintStart, group.CurrentStep.StartedAtUtc);
        Assert.Equal(SprintEnd, group.CurrentStep.EndsAtUtc);
        Assert.Equal(1, group.SprintCount);
        Assert.True(group.IsHostedBy(H));
        Assert.True(group.IsActiveMember(M));
        Assert.False(group.IsActiveMember(H));
        Assert.Equal(1, group.ActiveMemberCount);
        Assert.Null(group.EndedAtUtc);
        Assert.Null(group.EndReason);
        Assert.False(group.IsClosedToJoin);
    }

    [Fact]
    public void Should_RefuseStart_When_HostIsFirstMember()
    {
        var result = GroupSession.Start(Guid.NewGuid(), H, SprintStart, SprintEnd, H, SprintStart);

        Assert.True(result.IsFailure);
        Assert.Equal(CommunityErrors.Community.CannotJoinOwnSprint, result.Error);
    }

    [Fact]
    public void Should_RefuseStart_When_FirstSprintIsOver()
    {
        var result = GroupSession.Start(Guid.NewGuid(), H, SprintStart, SprintEnd, M, SprintEnd);

        Assert.True(result.IsFailure);
        Assert.Equal(CommunityErrors.Community.GroupEnded, result.Error);
    }

    // ── StateAt / EndIfHostAbsent ──────────────────────────────────────────

    [Fact]
    public void Should_ReportHostAbsent_When_LimitPassed()
    {
        var group = NewGroup();

        var state = group.StateAt(At(10, 30));

        Assert.Equal(GroupPhase.Ended, state.Phase);
        Assert.Equal(At(10, 30), state.EndedAtUtc);
        Assert.Equal(GroupSessionEndReason.HostAbsent, state.EndReason);
        Assert.Null(group.EndedAtUtc);
    }

    [Fact]
    public void Should_ReportRunningThenWaiting_When_StateAt()
    {
        var group = NewGroup();

        Assert.Equal(GroupPhase.Running, group.StateAt(At(10, 10)).Phase);
        var waiting = group.StateAt(At(10, 20));
        Assert.Equal(GroupPhase.WaitingForHost, waiting.Phase);
        Assert.Null(waiting.EndedAtUtc);
        Assert.Null(waiting.EndReason);
    }

    [Fact]
    public void Should_RecordHostAbsence_When_LimitPassed()
    {
        var group = NewGroup();

        Assert.False(group.EndIfHostAbsent(At(10, 29, 59)));
        Assert.Null(group.EndedAtUtc);

        Assert.True(group.EndIfHostAbsent(At(10, 45)));
        Assert.Equal(At(10, 30), group.EndedAtUtc);
        Assert.Equal(GroupSessionEndReason.HostAbsent, group.EndReason);
        Assert.False(group.EndIfHostAbsent(At(10, 50)));
    }

    // ── LaunchStep ─────────────────────────────────────────────────────────

    [Fact]
    public void Should_RefuseLaunch_When_StepRunning()
    {
        var group = NewGroup();

        var result = group.LaunchStep(GroupStepType.ShortBreak, At(10, 10), At(10, 15), At(10, 10));

        Assert.True(result.IsFailure);
        Assert.Equal(CommunityErrors.Community.GroupStepRunning, result.Error);
    }

    [Fact]
    public void Should_LaunchNextStep_When_Waiting()
    {
        var group = NewGroup();

        var shortBreak = group.LaunchStep(GroupStepType.ShortBreak, At(10, 16), At(10, 21), At(10, 16));

        Assert.True(shortBreak.IsSuccess);
        Assert.Equal(2, group.CurrentStep.Number);
        Assert.Equal(GroupStepType.ShortBreak, group.CurrentStep.Type);
        Assert.Equal(At(10, 21), group.CurrentStep.EndsAtUtc);
        Assert.Equal(1, group.SprintCount);

        var sprint = group.LaunchStep(GroupStepType.Sprint, At(10, 22), At(10, 47), At(10, 22));

        Assert.True(sprint.IsSuccess);
        Assert.Equal(3, group.CurrentStep.Number);
        Assert.Equal(GroupStepType.Sprint, group.CurrentStep.Type);
        Assert.Equal(2, group.SprintCount);
    }

    [Fact]
    public void Should_RefuseLaunch_When_HostAbsent()
    {
        var group = NewGroup();

        var result = group.LaunchStep(GroupStepType.Sprint, At(10, 30), At(10, 55), At(10, 30));

        Assert.True(result.IsFailure);
        Assert.Equal(CommunityErrors.Community.GroupEnded, result.Error);
        Assert.Equal(1, group.CurrentStep.Number);
    }

    [Fact]
    public void Should_RefuseLaunch_When_StepBoundsAreInvalid()
    {
        var group = NewGroup();

        var result = group.LaunchStep(GroupStepType.Sprint, At(10, 20), At(10, 20), At(10, 20));

        Assert.True(result.IsFailure);
        Assert.Equal(CommunityErrors.Community.InvalidGroupStep, result.Error);
        Assert.Equal(1, group.CurrentStep.Number);
        Assert.Equal(0, group.Version);
    }

    [Fact]
    public void Should_RefuseGroupStep_When_NumberBelowOneOrEndNotAfterStart()
    {
        Assert.Equal(CommunityErrors.Community.InvalidGroupStep, GroupStep.Create(0, GroupStepType.Sprint, SprintStart, SprintEnd).Error);
        Assert.Equal(CommunityErrors.Community.InvalidGroupStep, GroupStep.Create(1, GroupStepType.Sprint, SprintEnd, SprintStart).Error);
        Assert.Equal(CommunityErrors.Community.InvalidGroupStep, GroupStep.Create(1, GroupStepType.Sprint, SprintStart, SprintStart).Error);
    }

    // ── Stop ───────────────────────────────────────────────────────────────

    [Fact]
    public void Should_Stop_When_Running()
    {
        var group = NewGroup();

        var result = group.Stop(GroupSessionEndReason.Stopped, At(10, 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(At(10, 5), group.EndedAtUtc);
        Assert.Equal(GroupSessionEndReason.Stopped, group.EndReason);
    }

    [Fact]
    public void Should_Stop_When_Waiting()
    {
        var group = NewGroup();

        var result = group.Stop(GroupSessionEndReason.HostLeftCommunity, At(10, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(At(10, 20), group.EndedAtUtc);
        Assert.Equal(GroupSessionEndReason.HostLeftCommunity, group.EndReason);
    }

    [Fact]
    public void Should_RefuseStop_When_Ended()
    {
        var group = NewGroup();
        group.Stop(GroupSessionEndReason.Stopped, At(10, 5));

        var again = group.Stop(GroupSessionEndReason.Stopped, At(10, 6));
        var absent = NewGroup().Stop(GroupSessionEndReason.Stopped, At(10, 30));

        Assert.Equal(CommunityErrors.Community.GroupEnded, again.Error);
        Assert.Equal(CommunityErrors.Community.GroupEnded, absent.Error);
        Assert.Equal(At(10, 5), group.EndedAtUtc);
    }

    [Theory]
    [InlineData(GroupSessionEndReason.HostAbsent)]
    [InlineData(GroupSessionEndReason.HostAlone)]
    public void Should_Throw_When_StopReasonIsNotAHostDecision(GroupSessionEndReason reason)
    {
        var group = NewGroup();

        Assert.Throws<ArgumentOutOfRangeException>(() => group.Stop(reason, At(10, 5)));
    }

    // ── Join ───────────────────────────────────────────────────────────────

    [Fact]
    public void Should_Join_When_Running()
    {
        var group = NewGroup();

        var result = group.Join(N, At(10, 0));

        Assert.True(result.IsSuccess);
        Assert.True(group.IsActiveMember(N));
        Assert.Equal(2, group.ActiveMemberCount);
    }

    [Fact]
    public void Should_RefuseJoin_When_WaitingForHost()
    {
        var group = NewGroup();

        var result = group.Join(N, At(10, 20));

        Assert.Equal(CommunityErrors.Community.GroupWaitingForHost, result.Error);
        Assert.False(group.IsActiveMember(N));
    }

    [Fact]
    public void Should_RefuseJoin_When_Ended()
    {
        var group = NewGroup();
        group.Stop(GroupSessionEndReason.Stopped, At(10, 5));

        var result = group.Join(N, At(10, 6));

        Assert.Equal(CommunityErrors.Community.GroupEnded, result.Error);
    }

    [Fact]
    public void Should_RefuseJoin_When_Closed()
    {
        var group = NewGroup();
        group.Close();

        var result = group.Join(N, At(10, 0));

        Assert.Equal(CommunityErrors.Community.GroupClosed, result.Error);
        Assert.False(group.IsActiveMember(N));
    }

    [Fact]
    public void Should_RefuseJoin_When_UserIsHost()
    {
        var group = NewGroup();

        var result = group.Join(H, At(10, 0));

        Assert.Equal(CommunityErrors.Community.CannotJoinOwnSprint, result.Error);
    }

    [Fact]
    public void Should_RefuseJoin_When_AlreadyActiveMember()
    {
        var group = NewGroup();

        var result = group.Join(M, At(10, 0));

        Assert.Equal(CommunityErrors.Community.AlreadyInGroup, result.Error);
    }

    [Fact]
    public void Should_Rejoin_When_MemberLeft()
    {
        var group = NewGroup();
        group.Leave(M, At(9, 55));

        var result = group.Join(M, At(10, 0));

        Assert.True(result.IsSuccess);
        Assert.True(group.IsActiveMember(M));
        Assert.Equal(1, group.ActiveMemberCount);
    }

    [Fact]
    public void Should_ResetMembership_When_MemberRejoins()
    {
        var group = NewGroup();
        group.Leave(M, At(9, 55));
        group.Join(M, At(10, 0));

        var member = Members(group).Single();

        Assert.Equal(At(10, 0), member.JoinedAtUtc);
        Assert.Null(member.LeftAtUtc);
        Assert.True(member.IsActive);
    }

    [Fact]
    public void Should_RejoinAfterClosedThenReopened_When_Reopened()
    {
        var group = NewGroup();
        group.Close();
        group.Reopen();

        Assert.True(group.Join(N, At(10, 0)).IsSuccess);
    }

    // ── Leave ──────────────────────────────────────────────────────────────

    [Fact]
    public void Should_Leave_When_Waiting()
    {
        var group = NewGroup();

        var result = group.Leave(M, At(10, 20));

        Assert.True(result.IsSuccess);
        Assert.False(group.IsActiveMember(M));
        Assert.Equal(0, group.ActiveMemberCount);
        Assert.Equal(At(10, 20), Members(group).Single().LeftAtUtc);
    }

    [Fact]
    public void Should_KeepFirstLeaveTime_When_LeavingTwice()
    {
        var group = NewGroup();
        group.Leave(M, At(10, 1));

        var result = group.Leave(M, At(10, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(At(10, 1), Members(group).Single().LeftAtUtc);
    }

    [Fact]
    public void Should_RefuseLeave_When_UnknownUser()
    {
        var group = NewGroup();

        var result = group.Leave(N, At(10, 1));

        Assert.Equal(CommunityErrors.Community.NotInGroup, result.Error);
    }

    // ── EndAsHostAlone ─────────────────────────────────────────────────────

    [Fact]
    public void Should_EndAsHostAlone_When_WaitingWithoutMember()
    {
        var group = NewGroup();
        group.Leave(M, At(10, 16));

        var result = group.EndAsHostAlone(At(10, 17));

        Assert.True(result.IsSuccess);
        Assert.Equal(At(10, 17), group.EndedAtUtc);
        Assert.Equal(GroupSessionEndReason.HostAlone, group.EndReason);
    }

    [Fact]
    public void Should_RefuseHostAlone_When_MemberRemains()
    {
        var group = NewGroup();

        var result = group.EndAsHostAlone(At(10, 17));

        Assert.Equal(CommunityErrors.Community.GroupNotAlone, result.Error);
        Assert.Null(group.EndedAtUtc);
    }

    [Fact]
    public void Should_RefuseHostAlone_When_StepRunning()
    {
        var group = NewGroup();
        group.Leave(M, At(10, 1));

        var result = group.EndAsHostAlone(At(10, 2));

        Assert.Equal(CommunityErrors.Community.GroupStepRunning, result.Error);
    }

    [Fact]
    public void Should_RefuseHostAlone_When_Ended()
    {
        var group = NewGroup();
        group.Leave(M, At(10, 1));

        var result = group.EndAsHostAlone(At(10, 30));

        Assert.Equal(CommunityErrors.Community.GroupEnded, result.Error);
    }

    // ── Close / Reopen ─────────────────────────────────────────────────────

    [Fact]
    public void Should_CloseAndReopen_When_NotEnded()
    {
        var group = NewGroup();

        Assert.True(group.Close().IsSuccess);
        Assert.True(group.IsClosedToJoin);
        Assert.True(group.Close().IsSuccess);
        Assert.True(group.IsClosedToJoin);

        Assert.True(group.Reopen().IsSuccess);
        Assert.False(group.IsClosedToJoin);
        Assert.True(group.Reopen().IsSuccess);
        Assert.False(group.IsClosedToJoin);
    }

    [Fact]
    public void Should_RefuseCloseAndReopen_When_Ended()
    {
        var group = NewGroup();
        group.Stop(GroupSessionEndReason.Stopped, At(10, 5));

        Assert.Equal(CommunityErrors.Community.GroupEnded, group.Close().Error);
        Assert.Equal(CommunityErrors.Community.GroupEnded, group.Reopen().Error);
    }

    // ── Version ────────────────────────────────────────────────────────────

    [Fact]
    public void Should_IncrementVersion_When_StepLaunched()
    {
        var group = NewGroup();
        Assert.Equal(0, group.Version);

        group.LaunchStep(GroupStepType.ShortBreak, At(10, 16), At(10, 21), At(10, 16));

        Assert.Equal(1, group.Version);
    }

    [Fact]
    public void Should_KeepVersion_When_MemberJoins()
    {
        var group = NewGroup();

        group.Join(N, At(10, 0));
        group.Leave(N, At(10, 1));
        group.Leave(M, At(10, 2));

        Assert.Equal(0, group.Version);
    }

    [Fact]
    public void Should_IncrementVersion_When_GroupRowIsWritten()
    {
        var closed = NewGroup();
        closed.Close();
        var reopened = NewGroup();
        reopened.Close();
        reopened.Reopen();
        var stopped = NewGroup();
        stopped.Stop(GroupSessionEndReason.Stopped, At(10, 5));
        var absent = NewGroup();
        absent.EndIfHostAbsent(At(10, 30));
        var alone = NewGroup();
        alone.Leave(M, At(10, 16));
        alone.EndAsHostAlone(At(10, 17));

        Assert.Equal(1, closed.Version);
        Assert.Equal(2, reopened.Version);
        Assert.Equal(1, stopped.Version);
        Assert.Equal(1, absent.Version);
        Assert.Equal(1, alone.Version);
    }

    [Fact]
    public void Should_KeepVersion_When_CloseIsRepeated()
    {
        var group = NewGroup();
        group.Close();

        group.Close();

        Assert.Equal(1, group.Version);
    }

    // The members are private (ADR-026): read them through reflection only to check the member rows.
    private static List<GroupMember> Members(GroupSession group) =>
        (List<GroupMember>)typeof(GroupSession)
            .GetField("_members", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(group)!;
}
