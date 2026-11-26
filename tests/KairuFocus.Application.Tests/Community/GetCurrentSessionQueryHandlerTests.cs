using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetCurrentSession;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetCurrentSessionQueryHandlerTests
{
    private const string VisitorName = "tomtom";

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly UserId Visitor = FakeCurrentUserService.TestUserId;

    // Test data only: never part of AmbianceCatalogue.Default.
    private static readonly AmbianceCatalogue Catalogue = new(
    [
        new Ambiance("test1", "Test 1", AmbianceCategory.Lofi, "https://www.youtube.com/watch?v=test1"),
        new Ambiance("test2", "Test 2", AmbianceCategory.Classical, "https://www.youtube.com/watch?v=test2"),
    ]);

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeGroupSessionRepository _groups = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly GetCurrentSessionQueryHandler _sut;

    public GetCurrentSessionQueryHandlerTests()
    {
        _sut = new GetCurrentSessionQueryHandler(
            _profiles,
            _groups,
            _openSprints,
            _sessions,
            Catalogue,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now));
    }

    private static SprintMember Member(string name, string? avatarUrl = null) =>
        new(ProfileName.Create(name).Value, avatarUrl);

    private PomodoroSession GivenActiveSession(PomodoroSessionType type, int minutes)
    {
        var session = PomodoroSession.Create(type, minutes, Visitor);
        Assert.True(session.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(session);
        return session;
    }

    // The visitor joined the group of another host; the step ended `endedMinutesAgo` minutes ago
    // (negative: it ends in the future).
    private GroupSession GivenGroupAsParticipant(int endedMinutesAgo)
    {
        var group = GroupSession.Start(
            Guid.NewGuid(), UserId.New(), NowUtc.AddMinutes(-endedMinutesAgo - 25), NowUtc.AddMinutes(-endedMinutesAgo),
            Visitor, NowUtc.AddMinutes(-endedMinutesAgo - 20)).Value;
        _groups.Seed(group);
        return group;
    }

    private GroupSession GivenGroupAsHost(int endedMinutesAgo)
    {
        var group = GroupSession.Start(
            Guid.NewGuid(), Visitor, NowUtc.AddMinutes(-endedMinutesAgo - 25), NowUtc.AddMinutes(-endedMinutesAgo),
            UserId.New(), NowUtc.AddMinutes(-endedMinutesAgo - 20)).Value;
        _groups.Seed(group);
        return group;
    }

    private void GivenJoinedSession(Guid rootId)
    {
        var joined = PomodoroSession.CreateJoinedStep(
            PomodoroSessionType.Sprint, Visitor, PomodoroSessionId.From(rootId), NowUtc.AddMinutes(10), NowUtc).Value;
        Assert.True(joined.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(joined);
    }

    [Fact]
    public async Task Should_ReturnNull_When_NotMember()
    {
        GivenActiveSession(PomodoroSessionType.Sprint, 25);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_DescribeSoloOpenSprint_When_NobodyJoined()
    {
        _profiles.Seed(Visitor, VisitorName);
        var session = GivenActiveSession(PomodoroSessionType.Sprint, 25);
        Assert.True(session.CloseToJoin().IsSuccess);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.False(result.IsGroup);
        Assert.Equal("Host", result.Role);
        Assert.Equal("Running", result.Phase);
        Assert.Equal(1, result.StepNumber);
        Assert.Equal(1, result.SprintNumber);
        Assert.Equal("Sprint", result.StepType);
        Assert.Equal(new DateTimeOffset(NowUtc.AddMinutes(25), TimeSpan.Zero), result.StepEndsAt);
        Assert.Null(result.WaitLimitAt);
        Assert.Null(result.EndReason);
        Assert.Empty(result.Others);
        Assert.True(result.IsClosedToJoin);
    }

    [Fact]
    public async Task Should_ReturnNull_When_ActiveSessionIsNotAnOpenSprint()
    {
        _profiles.Seed(Visitor, VisitorName);
        GivenActiveSession(PomodoroSessionType.Sprint, 0);

        Assert.Null(await _sut.Handle(new GetCurrentSessionQuery()));
    }

    [Fact]
    public async Task Should_DescribeWaiting_When_StepEnded()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsParticipant(endedMinutesAgo: 2);
        GivenJoinedSession(group.Id);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.True(result.IsGroup);
        Assert.Equal("Participant", result.Role);
        Assert.Equal("WaitingForHost", result.Phase);
        Assert.Equal(1, result.StepNumber);
        Assert.Equal(1, result.SprintNumber);
        Assert.Equal(new DateTimeOffset(NowUtc.AddMinutes(-2), TimeSpan.Zero), result.StepEndsAt);
        Assert.Equal(new DateTimeOffset(NowUtc.AddMinutes(13), TimeSpan.Zero), result.WaitLimitAt);
        Assert.Null(result.EndReason);
        Assert.Equal(0, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_DescribeRunningStep_When_StepIsRunning()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsParticipant(endedMinutesAgo: -10);
        GivenJoinedSession(group.Id);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.Equal("Running", result.Phase);
        Assert.Null(result.WaitLimitAt);
    }

    [Fact]
    public async Task Should_RecordHostAbsence_When_LimitPassed()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsParticipant(endedMinutesAgo: 20);
        GivenJoinedSession(group.Id);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.Equal("Ended", result.Phase);
        Assert.Equal("HostAbsent", result.EndReason);
        Assert.Null(result.WaitLimitAt);
        Assert.Equal(1, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_ExcludeViewer_When_ListingOthers()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsParticipant(endedMinutesAgo: -10);
        GivenJoinedSession(group.Id);
        _openSprints.SeedGroupMembers(group.Id, Member("lea_dev", "https://a/1"), Member("TomTom"), Member("noa_42"));

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.Equal(
            [new SprintMemberViewModel("lea_dev", "https://a/1"), new SprintMemberViewModel("noa_42", null)],
            result.Others);
    }

    [Fact]
    public async Task Should_ReportStopped_When_HostStopped()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsParticipant(endedMinutesAgo: -10);
        GivenJoinedSession(group.Id);
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, NowUtc.AddMinutes(-1)).IsSuccess);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.Equal("Participant", result.Role);
        Assert.Equal("Ended", result.Phase);
        Assert.Equal("Stopped", result.EndReason);
    }

    [Fact]
    public async Task Should_RecordDepartureAndReturnNull_When_ParticipantRunsSoloSession()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsParticipant(endedMinutesAgo: -10);
        GivenActiveSession(PomodoroSessionType.ShortBreak, 5);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.Null(result);
        Assert.False(group.IsActiveMember(Visitor));
        Assert.Equal(1, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_DescribeSoloOpenSprint_When_ParticipantLeftForSoloSprint()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsParticipant(endedMinutesAgo: -10);
        GivenActiveSession(PomodoroSessionType.Sprint, 25);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.False(result.IsGroup);
        Assert.False(group.IsActiveMember(Visitor));
        Assert.Equal(1, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_DescribeSoloOpenSprint_When_HostOfEndedGroupRunsSoloSprint()
    {
        _profiles.Seed(Visitor, VisitorName);
        var group = GivenGroupAsHost(endedMinutesAgo: 20);
        Assert.True(group.EndIfHostAbsent(NowUtc));
        GivenActiveSession(PomodoroSessionType.Sprint, 25);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.False(result.IsGroup);
        Assert.Equal("Running", result.Phase);
        Assert.Equal("Host", result.Role);
    }

    // ── Ambiance ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnHostAmbiance_When_ParticipantInGroup()
    {
        // The visitor's own choice does not apply in someone else's group.
        _profiles.Seed(Visitor, VisitorName, ambianceCode: "test2");
        var group = GivenGroupAsParticipant(endedMinutesAgo: -10);
        GivenJoinedSession(group.Id);
        _openSprints.SeedHostAmbiance(group.Id, "test1");

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.True(result.IsGroup);
        Assert.Equal(new AmbianceViewModel("test1", "Test 1", "Lofi", "https://www.youtube.com/watch?v=test1"), result.Ambiance);
    }

    [Fact]
    public async Task Should_ReturnOwnAmbiance_When_HostOfSoloOpenSprint()
    {
        _profiles.Seed(Visitor, VisitorName, ambianceCode: "test2");
        GivenActiveSession(PomodoroSessionType.Sprint, 25);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.False(result.IsGroup);
        Assert.Equal(new AmbianceViewModel("test2", "Test 2", "Classical", "https://www.youtube.com/watch?v=test2"), result.Ambiance);
    }

    [Fact]
    public async Task Should_ReturnNoAmbiance_When_HostChoseNone()
    {
        _profiles.Seed(Visitor, VisitorName, ambianceCode: "test2");
        var group = GivenGroupAsParticipant(endedMinutesAgo: -10);
        GivenJoinedSession(group.Id);
        _openSprints.SeedHostAmbiance(group.Id, null);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.Null(result.Ambiance);
    }

    [Fact]
    public async Task Should_ReturnNoAmbiance_When_OwnCodeNoLongerInCatalogue()
    {
        _profiles.Seed(Visitor, VisitorName, ambianceCode: "removed");
        GivenActiveSession(PomodoroSessionType.Sprint, 25);

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.Null(result.Ambiance);
    }

    [Fact]
    public async Task Should_ReturnHostAmbiance_When_ViewerHostsGroup()
    {
        _profiles.Seed(Visitor, VisitorName, ambianceCode: "test1");
        var group = GivenGroupAsHost(endedMinutesAgo: -10);
        _openSprints.SeedHostAmbiance(group.Id, "test1");

        var result = await _sut.Handle(new GetCurrentSessionQuery());

        Assert.NotNull(result);
        Assert.Equal("Host", result.Role);
        Assert.Equal("test1", result.Ambiance?.Code);
    }
}
