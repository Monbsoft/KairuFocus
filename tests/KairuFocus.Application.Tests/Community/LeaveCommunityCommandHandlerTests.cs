using KairuFocus.Application.Community.Commands.LeaveCommunity;
using KairuFocus.Application.Community.Commands.LeaveOrStopGroupSession;
using KairuFocus.Application.Pomodoro.Commands.InterruptSession;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class LeaveCommunityCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakeGroupSessionRepository _groups = new();
    private readonly FakeJournalEntryRepository _journal = new();
    private readonly UserId _me = UserId.New();
    private readonly LeaveCommunityCommandHandler _sut;

    public LeaveCommunityCommandHandlerTests()
    {
        var currentUser = new MembershipCurrentUser(_me);
        var interrupt = new InterruptSessionCommandHandler(
            _sessions,
            new FakeMediator(_journal),
            currentUser,
            NullLogger<InterruptSessionCommandHandler>.Instance);
        var leaveOrStop = new LeaveOrStopGroupSessionCommandHandler(
            _profiles,
            _groups,
            _sessions,
            new FakeMediator(_journal, interruptSession: interrupt),
            currentUser,
            new FixedTimeProvider(Now),
            NullLogger<LeaveOrStopGroupSessionCommandHandler>.Instance);

        _sut = new LeaveCommunityCommandHandler(
            _profiles,
            new FakeMediator(_journal, leaveOrStop: leaveOrStop),
            currentUser,
            NullLogger<LeaveCommunityCommandHandler>.Instance);
    }

    private GroupSession GivenGroup(UserId host, UserId member, Guid? id = null)
    {
        var end = NowUtc.AddMinutes(15);
        var group = GroupSession.Start(id ?? Guid.NewGuid(), host, end.AddMinutes(-25), end, member, end.AddMinutes(-20));
        Assert.True(group.IsSuccess);
        _groups.Seed(group.Value);
        return group.Value;
    }

    [Fact]
    public async Task Should_RemoveProfile_When_Member()
    {
        _profiles.Seed(_me, "bobby");
        _profiles.Seed(UserId.New(), "lea");

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal("lea", Assert.Single(_profiles.Stored).Name.Value);
        Assert.Equal(1, _profiles.TryRemoveCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_NoProfile()
    {
        _profiles.Seed(UserId.New(), "lea");

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Equal(0, _profiles.TryRemoveCallCount);
        Assert.Single(_profiles.Stored);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_RemovalRaced()
    {
        _profiles.Seed(_me, "bobby");
        _profiles.OnTryRemove = _ => _profiles.Delete(_me);

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Equal(1, _profiles.TryRemoveCallCount);
    }

    [Fact]
    public async Task Should_StopGroupWithHostLeftCommunity_When_HostLeavesCommunity()
    {
        _profiles.Seed(_me, "bobby");
        var root = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, _me);
        Assert.True(root.Start(NowUtc.AddMinutes(-10)).IsSuccess);
        _sessions.Sessions.Add(root);
        var group = GivenGroup(_me, UserId.New(), root.Id.Value);

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(GroupSessionEndReason.HostLeftCommunity, group.EndReason);
        Assert.Equal(PomodoroSessionStatus.Interrupted, root.Status);
        Assert.Empty(_profiles.Stored);
    }

    [Fact]
    public async Task Should_LeaveGroup_When_ParticipantLeavesCommunity()
    {
        _profiles.Seed(_me, "bobby");
        var group = GivenGroup(UserId.New(), _me);

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.True(result.IsSuccess);
        Assert.False(group.IsActiveMember(_me));
        Assert.Null(group.EndedAtUtc);
        Assert.Empty(_profiles.Stored);
    }
}
