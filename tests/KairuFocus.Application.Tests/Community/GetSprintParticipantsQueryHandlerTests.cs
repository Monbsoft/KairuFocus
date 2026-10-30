using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetSprintParticipants;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetSprintParticipantsQueryHandlerTests
{
    private const string VisitorName = "tomtom";

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly UserId Visitor = FakeCurrentUserService.TestUserId;

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly GetSprintParticipantsQueryHandler _sut;

    public GetSprintParticipantsQueryHandlerTests()
    {
        _sut = new GetSprintParticipantsQueryHandler(
            _profiles,
            _openSprints,
            _sessions,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now));
    }

    private static SprintMember Member(string name, string? avatarUrl = null) =>
        new(ProfileName.Create(name).Value, avatarUrl);

    private PomodoroSession GivenHostSession()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Visitor);
        Assert.True(session.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(session);
        return session;
    }

    private void GivenJoinedSession(Guid rootId)
    {
        var joined = PomodoroSession.CreateJoined(Visitor, PomodoroSessionId.From(rootId), NowUtc.AddMinutes(10), NowUtc).Value;
        Assert.True(joined.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(joined);
    }

    [Fact]
    public async Task Should_ReturnOthers_When_InGroup()
    {
        const string avatarUrl = "https://avatars.githubusercontent.com/u/1";
        _profiles.Seed(Visitor, VisitorName);
        var rootId = Guid.NewGuid();
        GivenJoinedSession(rootId);
        _openSprints.SeedParticipants(rootId, Member("lea_dev", avatarUrl), Member("noa_42"));

        var result = await _sut.Handle(new GetSprintParticipantsQuery());

        Assert.Equal(
            [new SprintMemberViewModel("lea_dev", avatarUrl), new SprintMemberViewModel("noa_42", null)],
            result.Others);
    }

    [Fact]
    public async Task Should_ReadTheVisitorOwnGroup_When_VisitorIsHost()
    {
        _profiles.Seed(Visitor, VisitorName);
        var hosted = GivenHostSession();
        _openSprints.SeedParticipants(hosted.Id.Value, Member(VisitorName), Member("lea_dev"));

        var result = await _sut.Handle(new GetSprintParticipantsQuery());

        Assert.Equal([new SprintMemberViewModel("lea_dev", null)], result.Others);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoSessionOrNotMemberOrAlone()
    {
        var rootId = Guid.NewGuid();
        _openSprints.SeedParticipants(rootId, Member(VisitorName), Member("lea_dev"));

        // Not a member, even with a session in a group: the sprint continues in private.
        GivenJoinedSession(rootId);
        var notMember = await _sut.Handle(new GetSprintParticipantsQuery());
        _sessions.Sessions.Clear();

        // A member without an active session.
        _profiles.Seed(Visitor, VisitorName);
        var noSession = await _sut.Handle(new GetSprintParticipantsQuery());

        // Alone in the group.
        var hosted = GivenHostSession();
        _openSprints.SeedParticipants(hosted.Id.Value, Member(VisitorName));
        var alone = await _sut.Handle(new GetSprintParticipantsQuery());

        Assert.Empty(notMember.Others);
        Assert.Empty(noSession.Others);
        Assert.Empty(alone.Others);
    }

    // The read knows no user: the visitor is recognised by the key of the name, whatever its case.
    [Fact]
    public async Task Should_ExcludeVisitor_When_InGroup()
    {
        _profiles.Seed(Visitor, VisitorName);
        var rootId = Guid.NewGuid();
        GivenJoinedSession(rootId);
        _openSprints.SeedParticipants(rootId, Member("TomTom"), Member("lea_dev"));

        var result = await _sut.Handle(new GetSprintParticipantsQuery());

        Assert.Equal("lea_dev", Assert.Single(result.Others).Name);
    }
}
