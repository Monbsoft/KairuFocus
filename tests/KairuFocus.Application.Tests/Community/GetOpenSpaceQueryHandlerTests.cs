using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetOpenSpace;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetOpenSpaceQueryHandlerTests
{
    private const string ViewerName = "tomtom";
    private const string AvatarUrl = "https://avatars.githubusercontent.com/u/1";

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly UserId Viewer = FakeCurrentUserService.TestUserId;

    // Test data only: never part of AmbianceCatalogue.Default.
    private static readonly AmbianceCatalogue Catalogue = new(
    [
        new Ambiance("test1", "Test 1", AmbianceCategory.Lofi, "https://www.youtube.com/watch?v=test1"),
    ]);

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();
    private readonly FakeMemberDirectoryReadRepository _directory = new();
    private readonly FakeGroupSessionRepository _groupSessions = new();
    private readonly FakePomodoroSessionRepository _sessions = new();

    private GetOpenSpaceQueryHandler CreateSut(DateTimeOffset? now = null) =>
        new(
            _profiles,
            _openSprints,
            _directory,
            _groupSessions,
            _sessions,
            Catalogue,
            new FakeCurrentUserService(),
            new FixedTimeProvider(now ?? Now));

    private Task<GetOpenSpaceResult> QueryAsync(DateTimeOffset? now = null) =>
        CreateSut(now).Handle(new GetOpenSpaceQuery());

    private void GivenViewerMember(bool consented = true) =>
        _profiles.Seed(Viewer, ViewerName, presenceConsented: consented);

    private static SprintMember Member(string name, string? avatarUrl = null) =>
        new(ProfileName.Create(name).Value, avatarUrl);

    private static SprintMember[] Members(params string[] names) => names.Select(n => Member(n)).ToArray();

    private static OpenSpaceDesk Desk(
        Guid rootId,
        SprintMember[] seated,
        string? hostAmbianceCode = null,
        bool closed = false,
        GroupStepType stepType = GroupStepType.Sprint,
        GroupPhase phase = GroupPhase.Running) =>
        new(
            rootId,
            seated,
            hostAmbianceCode,
            // A waiting group shows the end of its last step, already past.
            phase == GroupPhase.WaitingForHost ? NowUtc.AddMinutes(-2) : NowUtc.AddMinutes(10),
            stepType,
            phase,
            closed);

    /// <summary>A group whose first sprint runs until 20 minutes from now, the viewer being its member.</summary>
    private GroupSession GivenGroupWithViewerAsMember()
    {
        var group = GroupSession.Start(
            Guid.NewGuid(), UserId.New(), NowUtc.AddMinutes(-5), NowUtc.AddMinutes(20), Viewer, NowUtc.AddMinutes(-4)).Value;
        _groupSessions.Seed(group);
        return group;
    }

    private PomodoroSession GivenViewerSprint(bool closed)
    {
        var sprint = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Viewer);
        Assert.True(sprint.Start(NowUtc.AddMinutes(-1)).IsSuccess);
        if (closed)
            Assert.True(sprint.CloseToJoin().IsSuccess);
        _sessions.Sessions.Add(sprint);
        return sprint;
    }

    private static DateTime Utc(int month, int day) => new(2026, month, day, 0, 0, 0, DateTimeKind.Utc);

    // ── Access (D9, D10): nothing nominative, no read across users ────────

    [Fact]
    public async Task Should_ReturnNotMemberAndNoNames_When_ViewerHasNoProfile()
    {
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev")));
        _directory.SeedConnected(Members("noa_42"));
        _directory.SeedActive(new ActiveMembers(1, Members("noa_42")));

        var result = await QueryAsync();

        Assert.Equal("NotMember", result.Access);
        Assert.False(result.ViewerInGroup);
        Assert.Empty(result.Desks);
        Assert.Empty(result.Connected);
        Assert.Equal(0, result.ConnectedTotal);
        Assert.Null(result.ActiveToday);
        Assert.Null(_openSprints.LastMaxDesks);
        Assert.Null(_openSprints.LastDeskRoot);
        Assert.Null(_directory.LastConnectedSince);
        Assert.Null(_directory.LastActiveWindow);
    }

    [Fact]
    public async Task Should_ReturnConsentRequiredAndNoNames_When_ViewerHasNotConsented()
    {
        GivenViewerMember(consented: false);
        GivenGroupWithViewerAsMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev")));
        _directory.SeedConnected(Members("noa_42"));
        _directory.SeedActive(new ActiveMembers(1, Members("noa_42")));

        var result = await QueryAsync();

        Assert.Equal("ConsentRequired", result.Access);
        Assert.False(result.ViewerInGroup);
        Assert.Empty(result.Desks);
        Assert.Empty(result.Connected);
        Assert.Equal(0, result.ConnectedTotal);
        Assert.Null(result.ActiveToday);
        Assert.Null(_openSprints.LastMaxDesks);
        Assert.Null(_openSprints.LastDeskRoot);
        Assert.Null(_directory.LastConnectedSince);
        Assert.Null(_directory.LastActiveWindow);
    }

    [Fact]
    public async Task Should_ReturnOpen_When_ViewerHasConsented()
    {
        GivenViewerMember();

        var result = await QueryAsync();

        Assert.Equal("Open", result.Access);
        Assert.Empty(result.Desks);
        Assert.NotNull(result.ActiveToday);
    }

    // ── Desks ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_MapDesk_When_Queried()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), [Member("lea_dev", AvatarUrl)]));

        var desk = Assert.Single((await QueryAsync()).Desks);

        Assert.Equal([new MemberChipViewModel("lea_dev", AvatarUrl, false)], desk.Seated);
        Assert.Equal(new DateTimeOffset(NowUtc.AddMinutes(10), TimeSpan.Zero), desk.EndsAt);
        Assert.Equal(TimeSpan.Zero, desk.EndsAt.Offset);
        Assert.Equal("Sprint", desk.StepType);
        Assert.Equal("Running", desk.Phase);
        Assert.False(desk.IsClosedToJoin);
        Assert.Null(desk.Ambiance);
    }

    // A datetime2 column comes back with an unspecified kind: the end is still read as UTC.
    [Fact]
    public async Task Should_ReadEndAsUtc_When_KindIsUnspecified()
    {
        GivenViewerMember();
        var end = DateTime.SpecifyKind(NowUtc.AddMinutes(10), DateTimeKind.Unspecified);
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev")) with { EndsAtUtc = end });

        var desk = Assert.Single((await QueryAsync()).Desks);

        Assert.Equal(new DateTimeOffset(NowUtc.AddMinutes(10), TimeSpan.Zero), desk.EndsAt);
    }

    [Theory]
    [InlineData(GroupStepType.ShortBreak, GroupPhase.Running, "ShortBreak", "Running")]
    [InlineData(GroupStepType.LongBreak, GroupPhase.Running, "LongBreak", "Running")]
    [InlineData(GroupStepType.Sprint, GroupPhase.WaitingForHost, "Sprint", "WaitingForHost")]
    public async Task Should_NameStepTypeAndPhase_When_DeskIsListed(
        GroupStepType stepType,
        GroupPhase phase,
        string expectedStepType,
        string expectedPhase)
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev", "noa_42"), stepType: stepType, phase: phase));

        var desk = Assert.Single((await QueryAsync()).Desks);

        Assert.Equal(expectedStepType, desk.StepType);
        Assert.Equal(expectedPhase, desk.Phase);
    }

    [Fact]
    public async Task Should_SeatHostFirst_When_ListingDesks()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev", "noa_42", "alpha")));

        var desk = Assert.Single((await QueryAsync()).Desks);

        Assert.Equal(["lea_dev", "noa_42", "alpha"], desk.Seated.Select(m => m.Name));
    }

    [Fact]
    public async Task Should_FlagViewer_When_ViewerIsSeated()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev", "TomTom")));
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("noa_42")));

        var desks = (await QueryAsync()).Desks;

        Assert.Equal([false, true], desks[0].Seated.Select(m => m.IsMe));
        Assert.False(Assert.Single(desks[1].Seated).IsMe);
    }

    [Fact]
    public async Task Should_RequestTenDesks_When_Queried()
    {
        GivenViewerMember();

        await QueryAsync();

        Assert.Equal(10, _openSprints.LastMaxDesks);
    }

    [Fact]
    public async Task Should_PutOwnClosedDeskFirst_When_ViewerHostsClosedSprint()
    {
        GivenViewerMember();
        var sprint = GivenViewerSprint(closed: true);
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev")));
        _openSprints.SeedDeskByRoot(Desk(sprint.Id.Value, Members(ViewerName), closed: true));
        _directory.SeedConnected(Members("lea_dev", ViewerName));

        var result = await QueryAsync();

        Assert.Equal(sprint.Id.Value, _openSprints.LastDeskRoot);
        Assert.Equal(2, result.Desks.Count);
        Assert.True(result.Desks[0].IsClosedToJoin);
        Assert.True(Assert.Single(result.Desks[0].Seated).IsMe);
        Assert.Equal("lea_dev", Assert.Single(result.Desks[1].Seated).Name);
        // Seated at a shown desk: not listed again among the connected.
        Assert.Empty(result.Connected);
        Assert.Equal(0, result.ConnectedTotal);
    }

    [Fact]
    public async Task Should_PutClosedGroupFirst_When_ViewerIsSeatedInClosedGroup()
    {
        GivenViewerMember();
        var group = GivenGroupWithViewerAsMember();
        Assert.True(group.Close().IsSuccess);
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("noa_42")));
        _openSprints.SeedDeskByRoot(Desk(group.Id, Members("lea_dev", ViewerName), closed: true));

        var result = await QueryAsync();

        Assert.Equal(group.Id, _openSprints.LastDeskRoot);
        Assert.Equal(2, result.Desks.Count);
        Assert.True(result.Desks[0].IsClosedToJoin);
        Assert.Equal([false, true], result.Desks[0].Seated.Select(m => m.IsMe));
        Assert.Equal("noa_42", Assert.Single(result.Desks[1].Seated).Name);
    }

    [Fact]
    public async Task Should_NotReadOwnDesk_When_ViewerSitsAtNoClosedDesk()
    {
        GivenViewerMember();
        GivenViewerSprint(closed: false);
        GivenGroupWithViewerAsMember();

        await QueryAsync();

        Assert.Null(_openSprints.LastDeskRoot);
    }

    [Fact]
    public async Task Should_NotPutClosedGroupFirst_When_GroupEnded()
    {
        GivenViewerMember();
        var group = GivenGroupWithViewerAsMember();
        Assert.True(group.Close().IsSuccess);
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, NowUtc.AddMinutes(-1)).IsSuccess);
        _openSprints.SeedDeskByRoot(Desk(group.Id, Members("lea_dev", ViewerName), closed: true));

        var result = await QueryAsync();

        Assert.Null(_openSprints.LastDeskRoot);
        Assert.Empty(result.Desks);
    }

    // The host's sprint is the root of the group: the group row stands for both, one desk only.
    [Fact]
    public async Task Should_ShowGroupDeskOnce_When_ViewerHostsClosedGroupOfOwnSprint()
    {
        GivenViewerMember();
        var sprint = GivenViewerSprint(closed: true);
        var group = GroupSession.Start(
            sprint.Id.Value, Viewer, NowUtc.AddMinutes(-1), NowUtc.AddMinutes(24), UserId.New(), NowUtc).Value;
        Assert.True(group.Close().IsSuccess);
        _groupSessions.Seed(group);
        _openSprints.SeedDeskByRoot(Desk(group.Id, Members(ViewerName, "lea_dev"), closed: true));

        var result = await QueryAsync();

        Assert.Equal([true, false], Assert.Single(result.Desks).Seated.Select(m => m.IsMe));
    }

    // Reopened between the reads: the desk is already in the list, it is not repeated.
    [Fact]
    public async Task Should_NotRepeatDesk_When_OwnClosedDeskIsListedOpen()
    {
        GivenViewerMember();
        var sprint = GivenViewerSprint(closed: true);
        _openSprints.SeedDesk(Desk(sprint.Id.Value, Members(ViewerName)));
        _openSprints.SeedDeskByRoot(Desk(sprint.Id.Value, Members(ViewerName), closed: true));

        var result = await QueryAsync();

        Assert.False(Assert.Single(result.Desks).IsClosedToJoin);
        Assert.Null(_openSprints.LastDeskRoot);
    }

    // The desk may end between the reads: nothing to put first then.
    [Fact]
    public async Task Should_ListOpenDesksOnly_When_OwnClosedDeskIsOver()
    {
        GivenViewerMember();
        GivenViewerSprint(closed: true);
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev")));

        var result = await QueryAsync();

        Assert.Equal("lea_dev", Assert.Single(Assert.Single(result.Desks).Seated).Name);
    }

    // The host may run a solo open sprint while their closed group waits: one desk per host, the closed one the viewer is in.
    [Fact]
    public async Task Should_ShowOneDesk_When_ViewerHostsClosedGroupAndRunsSoloSprint()
    {
        GivenViewerMember();
        var group = GroupSession.Start(
            Guid.NewGuid(), Viewer, NowUtc.AddMinutes(-27), NowUtc.AddMinutes(-2), UserId.New(), NowUtc.AddMinutes(-20)).Value;
        Assert.True(group.Close().IsSuccess);
        _groupSessions.Seed(group);
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members(ViewerName)));
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("noa_42")));
        _openSprints.SeedDeskByRoot(Desk(group.Id, Members(ViewerName, "lea_dev"), closed: true, phase: GroupPhase.WaitingForHost));

        var desks = (await QueryAsync()).Desks;

        Assert.Equal(2, desks.Count);
        Assert.True(desks[0].IsClosedToJoin);
        Assert.Equal([true, false], desks[0].Seated.Select(m => m.IsMe));
        Assert.Equal("noa_42", Assert.Single(desks[1].Seated).Name);
    }

    // ── Ambiance of the desk ──────────────────────────────────────────────

    [Fact]
    public async Task Should_ResolveAmbiance_When_HostCodeInCatalogue()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev"), hostAmbianceCode: "test1"));

        var desk = Assert.Single((await QueryAsync()).Desks);

        Assert.Equal(new AmbianceViewModel("test1", "Test 1", "Lofi", "https://www.youtube.com/watch?v=test1"), desk.Ambiance);
    }

    [Fact]
    public async Task Should_ReturnNoAmbiance_When_CodeNoLongerInCatalogue()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev"), hostAmbianceCode: "removed"));

        var desk = Assert.Single((await QueryAsync()).Desks);

        Assert.Null(desk.Ambiance);
    }

    // ── Connected ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_AskForConnectedSinceThreeMinutes_When_Queried()
    {
        GivenViewerMember();

        await QueryAsync();

        Assert.Equal(NowUtc.AddMinutes(-3), _directory.LastConnectedSince);
        Assert.Equal(200, _directory.LastMaxConnected);
    }

    [Fact]
    public async Task Should_ExcludeSeatedMembers_When_ListingConnected()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev", "noa_42")));
        _directory.SeedConnected(Member("alpha", AvatarUrl), Member("LEA_DEV"), Member("noa_42"), Member(ViewerName));

        var result = await QueryAsync();

        Assert.Equal(
            [new MemberChipViewModel("alpha", AvatarUrl, false), new MemberChipViewModel(ViewerName, null, true)],
            result.Connected);
        Assert.Equal(2, result.ConnectedTotal);
    }

    // The viewer sits at a desk: out of the connected list, which then holds the others only.
    [Fact]
    public async Task Should_CapConnectedAt12AndCountTheRest_When_ManyConnected()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members(ViewerName)));
        var names = Enumerable.Range(1, 15).Select(i => $"member_{i:00}").ToArray();
        _directory.SeedConnected(Members(names));

        var result = await QueryAsync();

        Assert.Equal(names.Take(12), result.Connected.Select(m => m.Name));
        Assert.Equal(15, result.ConnectedTotal);
    }

    // Arrived before their first presence signal: the viewer is connected and consenting by construction.
    [Fact]
    public async Task Should_AddViewerToConnected_When_NotYetSeen()
    {
        _profiles.Seed(Viewer, ViewerName, ProfileNameSource.GitHub, AvatarUrl);
        _directory.SeedConnected(Members("alpha", "zed_dev"));

        var result = await QueryAsync();

        Assert.Equal(
            [
                new MemberChipViewModel("alpha", null, false),
                new MemberChipViewModel(ViewerName, AvatarUrl, true),
                new MemberChipViewModel("zed_dev", null, false),
            ],
            result.Connected);
        Assert.Equal(3, result.ConnectedTotal);
        Assert.Equal(0, _profiles.TryUpdateCallCount);
    }

    [Fact]
    public async Task Should_KeepViewerInShownList_When_MoreThanTwelveConnected()
    {
        GivenViewerMember();
        var names = Enumerable.Range(1, 15).Select(i => $"member_{i:00}").ToArray();
        _directory.SeedConnected([.. Members(names), Member(ViewerName)]);

        var result = await QueryAsync();

        Assert.Equal([.. names.Take(11), ViewerName], result.Connected.Select(m => m.Name));
        Assert.Equal([.. Enumerable.Repeat(false, 11), true], result.Connected.Select(m => m.IsMe));
        Assert.Equal(16, result.ConnectedTotal);
    }

    [Fact]
    public async Task Should_KeepViewerInShownList_When_NotYetSeenAndMoreThanTwelveConnected()
    {
        GivenViewerMember();
        var names = Enumerable.Range(1, 15).Select(i => $"member_{i:00}").ToArray();
        _directory.SeedConnected(Members(names));

        var result = await QueryAsync();

        Assert.Equal([.. names.Take(11), ViewerName], result.Connected.Select(m => m.Name));
        Assert.Equal(16, result.ConnectedTotal);
    }

    [Fact]
    public async Task Should_NotAddViewer_When_ViewerIsSeated()
    {
        GivenViewerMember();
        _openSprints.SeedDesk(Desk(Guid.NewGuid(), Members("lea_dev", ViewerName)));
        _directory.SeedConnected(Members("alpha"));

        var result = await QueryAsync();

        Assert.Equal([new MemberChipViewModel("alpha", null, false)], result.Connected);
        Assert.Equal(1, result.ConnectedTotal);
    }

    // ── Active today (UC-CO-13) ───────────────────────────────────────────

    [Fact]
    public async Task Should_AskForTheUtcDay_When_ReadingActiveToday()
    {
        GivenViewerMember();

        await QueryAsync(new DateTimeOffset(2026, 11, 5, 23, 30, 0, TimeSpan.Zero));

        Assert.Equal((Utc(11, 5), Utc(11, 6), 5), _directory.LastActiveWindow);
    }

    [Fact]
    public async Task Should_AskForTheNewDay_When_QueriedAtMidnight()
    {
        GivenViewerMember();

        await QueryAsync(new DateTimeOffset(2026, 11, 6, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal((Utc(11, 6), Utc(11, 7), 5), _directory.LastActiveWindow);
    }

    [Fact]
    public async Task Should_FlagMe_When_ViewerIsActiveToday()
    {
        GivenViewerMember();
        _directory.SeedActive(new ActiveMembers(2, [Member("lea_dev", AvatarUrl), Member("TOMTOM")]));

        var activeToday = (await QueryAsync()).ActiveToday;

        Assert.NotNull(activeToday);
        Assert.Equal(
            [new MemberChipViewModel("lea_dev", AvatarUrl, false), new MemberChipViewModel("TOMTOM", null, true)],
            activeToday.Members);
    }

    [Fact]
    public async Task Should_KeepTotal_When_MoreActiveMembersThanReturned()
    {
        GivenViewerMember();
        _directory.SeedActive(new ActiveMembers(7, Members("alpha", "bravo", "charlie", "delta", "echo")));

        var activeToday = (await QueryAsync()).ActiveToday;

        Assert.NotNull(activeToday);
        Assert.Equal(7, activeToday.Total);
        Assert.Equal(["alpha", "bravo", "charlie", "delta", "echo"], activeToday.Members.Select(m => m.Name));
    }

    // ── Viewer in a group ─────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReportViewerInGroup_When_ViewerHasLiveGroup()
    {
        GivenViewerMember();
        GivenGroupWithViewerAsMember();

        Assert.True((await QueryAsync()).ViewerInGroup);
    }

    // During the wait the viewer has no active session: the group alone counts.
    [Fact]
    public async Task Should_ReportViewerInGroup_When_GroupWaitsForHost()
    {
        GivenViewerMember();
        _groupSessions.Seed(GroupSession.Start(
            Guid.NewGuid(), UserId.New(), NowUtc.AddMinutes(-27), NowUtc.AddMinutes(-2), Viewer, NowUtc.AddMinutes(-20)).Value);

        Assert.True((await QueryAsync()).ViewerInGroup);
    }

    [Fact]
    public async Task Should_NotReportInGroup_When_LatestGroupEnded()
    {
        GivenViewerMember();
        var group = GivenGroupWithViewerAsMember();
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, NowUtc.AddMinutes(-1)).IsSuccess);

        Assert.False((await QueryAsync()).ViewerInGroup);
    }

    // Past the host wait limit the group is over, even though nothing recorded it yet.
    [Fact]
    public async Task Should_NotReportInGroup_When_HostAbsentLimitPassed()
    {
        GivenViewerMember();
        _groupSessions.Seed(GroupSession.Start(
            Guid.NewGuid(), UserId.New(), NowUtc.AddMinutes(-45), NowUtc.AddMinutes(-15), Viewer, NowUtc.AddMinutes(-40)).Value);

        Assert.False((await QueryAsync()).ViewerInGroup);
        Assert.Equal(0, _groupSessions.UpdateCallCount);
    }

    [Fact]
    public async Task Should_NotReportInGroup_When_ViewerBelongsToNoGroup()
    {
        GivenViewerMember();

        Assert.False((await QueryAsync()).ViewerInGroup);
    }

    // ── Contract ──────────────────────────────────────────────────────────

    // RootSessionId stays on the server side: no property of the desk can carry it.
    [Fact]
    public void Should_ExposeNoIdentifier_When_DeskIsSerialized()
    {
        var properties = typeof(DeskViewModel).GetProperties().Select(p => p.Name);

        Assert.Equal(["Seated", "EndsAt", "StepType", "Phase", "IsClosedToJoin", "Ambiance"], properties);
    }
}
