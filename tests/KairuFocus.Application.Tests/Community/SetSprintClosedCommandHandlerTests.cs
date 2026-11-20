using KairuFocus.Application.Community.Commands.SetSprintClosed;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class SetSprintClosedCommandHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc);
    private static readonly UserId Host = FakeCurrentUserService.TestUserId;

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakeGroupSessionRepository _groups = new();
    private readonly SetSprintClosedCommandHandler _sut;

    public SetSprintClosedCommandHandlerTests()
    {
        _sut = new SetSprintClosedCommandHandler(
            _profiles,
            _sessions,
            _groups,
            new FakeCurrentUserService(),
            new FixedTimeProvider(new DateTimeOffset(NowUtc)),
            NullLogger<SetSprintClosedCommandHandler>.Instance);
    }

    private PomodoroSession GivenActiveSession(PomodoroSessionType type = PomodoroSessionType.Sprint, int plannedMinutes = 25)
    {
        var session = PomodoroSession.Create(type, plannedMinutes, Host);
        Assert.True(session.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(session);
        return session;
    }

    private PomodoroSession GivenJoinedSession()
    {
        var joined = PomodoroSession.CreateJoinedStep(PomodoroSessionType.Sprint, Host, PomodoroSessionId.New(), NowUtc.AddMinutes(10), NowUtc);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(joined.Value);
        return joined.Value;
    }

    [Fact]
    public async Task Should_Close_When_HostIsMember()
    {
        _profiles.Seed(Host, "lea_dev");
        var session = GivenActiveSession();

        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.True(session.IsClosedToJoin);
        Assert.Equal(1, _sessions.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_Reopen_When_Closed()
    {
        _profiles.Seed(Host, "lea_dev");
        var session = GivenActiveSession();
        Assert.True(session.CloseToJoin().IsSuccess);

        var result = await _sut.Handle(new SetSprintClosedCommand(false));

        Assert.True(result.IsSuccess);
        Assert.False(session.IsClosedToJoin);
        Assert.Equal(1, _sessions.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotHost_When_SessionWasJoined()
    {
        _profiles.Seed(Host, "lea_dev");
        var session = GivenJoinedSession();

        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotSprintHost, result.Error);
        Assert.False(session.IsClosedToJoin);
        Assert.Equal(0, _sessions.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotHost_When_HostIsNotMember()
    {
        var session = GivenActiveSession();

        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotSprintHost, result.Error);
        Assert.False(session.IsClosedToJoin);
        Assert.Equal(0, _sessions.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_ReturnNoActiveSprint_When_NoSessionOrBreakOrFreeSprint()
    {
        _profiles.Seed(Host, "lea_dev");

        var noSession = await _sut.Handle(new SetSprintClosedCommand(true));

        var shortBreak = GivenActiveSession(PomodoroSessionType.ShortBreak, 5);
        var onBreak = await _sut.Handle(new SetSprintClosedCommand(true));
        _sessions.Sessions.Remove(shortBreak);

        GivenActiveSession(PomodoroSessionType.Sprint, plannedMinutes: 0);
        var onFreeSprint = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.All([noSession, onBreak, onFreeSprint], result =>
        {
            Assert.False(result.IsSuccess);
            Assert.Equal(CommunityErrors.Community.NoActiveSprint, result.Error);
        });
        Assert.Equal(0, _sessions.UpdateAsyncCallCount);
    }

    // The session is checked first: without a sprint to close, being a member or not changes nothing.
    [Fact]
    public async Task Should_ReturnNoActiveSprint_When_NotMemberHasNoSession()
    {
        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.Equal(CommunityErrors.Community.NoActiveSprint, result.Error);
    }

    // --- Group sessions ------------------------------------------------------------------------

    private static readonly UserId OtherUser = UserId.From(new Guid("30000000-0000-0000-0000-000000000003"));

    private GroupSession GivenGroup(UserId host, UserId member, DateTime? stepEndsAtUtc = null)
    {
        var end = stepEndsAtUtc ?? NowUtc.AddMinutes(15);
        var group = GroupSession.Start(Guid.NewGuid(), host, end.AddMinutes(-25), end, member, end.AddMinutes(-20));
        Assert.True(group.IsSuccess);
        _groups.Seed(group.Value);
        return group.Value;
    }

    [Fact]
    public async Task Should_CloseGroup_When_HostOfGroup()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup(Host, OtherUser);
        var session = GivenActiveSession();

        var closed = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.True(closed.IsSuccess);
        Assert.True(group.IsClosedToJoin);
        Assert.Equal(1, _groups.UpdateCallCount);
        Assert.False(session.IsClosedToJoin);
        Assert.Equal(0, _sessions.UpdateAsyncCallCount);

        var reopened = await _sut.Handle(new SetSprintClosedCommand(false));

        Assert.True(reopened.IsSuccess);
        Assert.False(group.IsClosedToJoin);
        Assert.Equal(2, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_CloseGroup_When_HostWaitsForNextStep()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup(Host, OtherUser, stepEndsAtUtc: NowUtc.AddMinutes(-2));

        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.True(result.IsSuccess);
        Assert.True(group.IsClosedToJoin);
    }

    [Fact]
    public async Task Should_RefuseClose_When_Participant()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup(OtherUser, Host);

        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotSprintHost, result.Error);
        Assert.False(group.IsClosedToJoin);
        Assert.Equal(0, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_UseRootSession_When_GroupIsOver()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup(Host, OtherUser);
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, NowUtc).IsSuccess);
        var session = GivenActiveSession();

        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.True(result.IsSuccess);
        Assert.True(session.IsClosedToJoin);
        Assert.Equal(0, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_UseRootSession_When_ParticipantMovedOnToOwnSprint()
    {
        _profiles.Seed(Host, "lea_dev");
        GivenGroup(OtherUser, Host);
        var session = GivenActiveSession();

        var result = await _sut.Handle(new SetSprintClosedCommand(true));

        Assert.True(result.IsSuccess);
        Assert.True(session.IsClosedToJoin);
        Assert.Equal(0, _groups.UpdateCallCount);
    }
}
