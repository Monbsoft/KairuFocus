using KairuFocus.Application.Community.Queries.GetOpenSprints;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetOpenSprintsQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly UserId Viewer = FakeCurrentUserService.TestUserId;

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly GetOpenSprintsQueryHandler _sut;

    public GetOpenSprintsQueryHandlerTests()
    {
        _sut = new GetOpenSprintsQueryHandler(
            _openSprints,
            _profiles,
            _sessions,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now));
    }

    private static OpenSprintGroup Group(Guid rootId, string leadName, int participants = 1, string? avatarUrl = null) =>
        new(
            rootId,
            new SprintMember(ProfileName.Create(leadName).Value, avatarUrl),
            NowUtc.AddMinutes(10),
            participants);

    [Fact]
    public async Task Should_MapGroups_When_Queried()
    {
        const string avatarUrl = "https://avatars.githubusercontent.com/u/1";
        _openSprints.SeedGroup(Group(Guid.NewGuid(), "lea_dev", participants: 3, avatarUrl));

        var result = await _sut.Handle(new GetOpenSprintsQuery());

        var row = Assert.Single(result.Groups);
        Assert.Equal("lea_dev", row.Name);
        Assert.Equal(avatarUrl, row.AvatarUrl);
        Assert.Equal(new DateTimeOffset(NowUtc.AddMinutes(10), TimeSpan.Zero), row.EndsAt);
        Assert.Equal(3, row.ParticipantCount);
        Assert.False(row.IsMine);
    }

    [Fact]
    public async Task Should_FlagMine_When_MyRootIsListed()
    {
        var mine = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Viewer);
        Assert.True(mine.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(mine);
        _profiles.Seed(Viewer, "tomtom");
        _openSprints.SeedGroup(Group(mine.Id.Value, "tomtom"));
        _openSprints.SeedGroup(Group(Guid.NewGuid(), "lea_dev"));

        var result = await _sut.Handle(new GetOpenSprintsQuery());

        Assert.Equal([true, false], result.Groups.Select(g => g.IsMine));
    }

    [Fact]
    public async Task Should_FlagMine_When_MySessionJoinedTheGroup()
    {
        var rootId = Guid.NewGuid();
        var joined = PomodoroSession.CreateJoined(Viewer, PomodoroSessionId.From(rootId), NowUtc.AddMinutes(10), NowUtc).Value;
        Assert.True(joined.Start(NowUtc).IsSuccess);
        _sessions.Sessions.Add(joined);
        _profiles.Seed(Viewer, "tomtom");
        _openSprints.SeedGroup(Group(rootId, "lea_dev", participants: 2));

        var result = await _sut.Handle(new GetOpenSprintsQuery());

        Assert.True(Assert.Single(result.Groups).IsMine);
    }

    [Fact]
    public async Task Should_NotFlagMine_When_ViewerHasNoActiveSession()
    {
        _profiles.Seed(Viewer, "tomtom");
        _openSprints.SeedGroup(Group(Guid.NewGuid(), "lea_dev"));

        var result = await _sut.Handle(new GetOpenSprintsQuery());

        Assert.False(Assert.Single(result.Groups).IsMine);
    }

    [Fact]
    public async Task Should_RequestTenGroups_When_Queried()
    {
        await _sut.Handle(new GetOpenSprintsQuery());

        Assert.Equal(10, _openSprints.LastMaxGroups);
    }

    [Fact]
    public async Task Should_ReportViewerNotMember_When_NoProfile()
    {
        _openSprints.SeedGroup(Group(Guid.NewGuid(), "lea_dev"));

        var result = await _sut.Handle(new GetOpenSprintsQuery());

        Assert.False(result.ViewerIsMember);
        Assert.Single(result.Groups);
    }

    [Fact]
    public async Task Should_ReportViewerMember_When_ProfileExists()
    {
        _profiles.Seed(Viewer, "tomtom");

        var result = await _sut.Handle(new GetOpenSprintsQuery());

        Assert.True(result.ViewerIsMember);
        Assert.Empty(result.Groups);
    }

    // RootSessionId stays on the server side: no property of the row can carry it.
    [Fact]
    public void Should_ExposeNoIdentifier_When_RowIsSerialized()
    {
        var properties = typeof(OpenSprintRowViewModel).GetProperties().Select(p => p.Name);

        Assert.Equal(["Name", "AvatarUrl", "EndsAt", "ParticipantCount", "IsMine"], properties);
    }
}
