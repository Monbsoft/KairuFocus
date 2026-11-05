using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

public sealed class EfCoreMemberDirectoryReadRepositoryTests
{
    private static readonly DateTime Since = new(2025, 10, 25, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Start = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime From = new(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = From.AddDays(1);
    private static readonly DateTime JoinedAtUtc = new(2026, 3, 17, 8, 0, 0, DateTimeKind.Utc);

    private static readonly ProfileName LeaName = ProfileName.Create("Lea_Dev").Value;
    private static readonly ProfileName TomName = ProfileName.Create("tomtom").Value;

    private readonly InMemoryDatabase _database = new();
    private readonly UserId _lea = UserId.New();
    private readonly UserId _tom = UserId.New();
    private readonly UserId _outsider = UserId.New();

    // --- FindByName -----------------------------------------------------------------------------

    [Fact]
    public async Task Should_FindMember_When_NameCaseDiffers()
    {
        await _database.SeedAsync(MemberProfile.Join(
            _lea, LeaName, ProfileNameSource.GitHub, "https://avatars.githubusercontent.com/u/1", JoinedAtUtc));

        var found = await QueryAsync(r => r.FindByNameAsync(ProfileName.Create("LEA_DEV").Value));

        Assert.NotNull(found);
        Assert.Equal("Lea_Dev", found.Name.Value);
        Assert.Equal(ProfileNameSource.GitHub, found.NameSource);
        Assert.Equal("https://avatars.githubusercontent.com/u/1", found.AvatarUrl);
        Assert.Equal(JoinedAtUtc, found.JoinedAtUtc);
    }

    [Fact]
    public async Task Should_ReturnNull_When_NotMember()
    {
        await _database.SeedAsync(Member(_tom, TomName));

        var found = await QueryAsync(r => r.FindByNameAsync(LeaName));

        Assert.Null(found);
    }

    // --- Sprints done together ------------------------------------------------------------------

    [Fact]
    public async Task Should_CountTogether_When_RootHasJoinedSession()
    {
        var root = CompletedRoot(_lea);
        await SeedMembersAsync(root, CompletedJoined(_tom, root));

        var host = await ActivityOfAsync(LeaName);
        var joiner = await ActivityOfAsync(TomName);

        Assert.Equal(1, host.SprintsTogether);
        Assert.Equal(1, joiner.SprintsTogether);
    }

    [Fact]
    public async Task Should_CountTogether_When_OtherParticipantInterrupted()
    {
        var root = CompletedRoot(_lea);
        await SeedMembersAsync(root, InterruptedJoined(_tom, root));

        var host = await ActivityOfAsync(LeaName);

        Assert.Equal(1, host.CompletedSprints);
        Assert.Equal(1, host.SprintsTogether);
    }

    [Fact]
    public async Task Should_CountTogether_When_HostInterruptedAndJoinerCompleted()
    {
        var root = InterruptedRoot(_lea);
        await SeedMembersAsync(root, CompletedJoined(_tom, root));

        var joiner = await ActivityOfAsync(TomName);

        Assert.Equal(1, joiner.CompletedSprints);
        Assert.Equal(1, joiner.SprintsTogether);
    }

    [Fact]
    public async Task Should_NotCountTogether_When_SprintWasSolo()
    {
        await SeedMembersAsync(CompletedRoot(_lea));

        var activity = await ActivityOfAsync(LeaName);

        Assert.Equal(1, activity.CompletedSprints);
        Assert.Equal(0, activity.SprintsTogether);
    }

    [Fact]
    public async Task Should_NotCountTogether_When_OwnSessionInterrupted()
    {
        var root = InterruptedRoot(_lea);
        await SeedMembersAsync(root, CompletedJoined(_tom, root));

        var host = await ActivityOfAsync(LeaName);

        Assert.Equal(0, host.CompletedSprints);
        Assert.Equal(0, host.SprintsTogether);
    }

    // --- Totals and heatmap ---------------------------------------------------------------------

    [Fact]
    public async Task Should_CapFocusAtFourHours_When_SprintIsLonger()
    {
        await SeedMembersAsync(
            CompletedFree(_lea, Start, Start.AddHours(5)),
            CompletedFree(_lea, Start.AddHours(6), Start.AddHours(6).AddMinutes(25)));

        var activity = await ActivityOfAsync(LeaName);

        Assert.Equal(2, activity.CompletedSprints);
        Assert.Equal(TimeSpan.FromHours(4) + TimeSpan.FromMinutes(25), activity.Focus);
    }

    [Fact]
    public async Task Should_BucketDaysInUtc_When_SprintEndsNearMidnight()
    {
        var midnight = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);
        await SeedMembersAsync(
            CompletedFree(_lea, midnight.AddMinutes(-30), midnight.AddSeconds(-1)),
            CompletedFree(_lea, midnight.AddMinutes(-25), midnight),
            CompletedFree(_lea, midnight.AddMinutes(-20), midnight.AddSeconds(1)));

        var activity = await ActivityOfAsync(LeaName);

        Assert.Equal(
            [new MemberActivityDay(new DateOnly(2026, 10, 22), 1), new MemberActivityDay(new DateOnly(2026, 10, 23), 2)],
            activity.Days);
    }

    [Fact]
    public async Task Should_OrderDaysByDate_When_SprintsWereSeededOutOfOrder()
    {
        await SeedMembersAsync(
            CompletedFree(_lea, Start.AddDays(2), Start.AddDays(2).AddMinutes(25)),
            CompletedFree(_lea, Start, Start.AddMinutes(25)),
            CompletedFree(_lea, Start.AddDays(1), Start.AddDays(1).AddMinutes(25)));

        var activity = await ActivityOfAsync(LeaName);

        Assert.Equal(
            [new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 21), new DateOnly(2026, 10, 22)],
            activity.Days.Select(d => d.DateUtc));
    }

    // The window bounds the heatmap only: the totals of a profile are all-time.
    [Fact]
    public async Task Should_LeaveOutOfDaysButKeepInTotals_When_SprintEndedBeforeWindow()
    {
        var oldRoot = CompletedRoot(_lea, Since.AddDays(-30));
        await SeedMembersAsync(
            oldRoot,
            CompletedJoined(_tom, oldRoot),
            CompletedFree(_lea, Since.AddMinutes(-25), Since.AddTicks(-1)),
            CompletedFree(_lea, Since.AddMinutes(-25), Since));

        var activity = await ActivityOfAsync(LeaName);

        Assert.Equal(3, activity.CompletedSprints);
        Assert.Equal(1, activity.SprintsTogether);
        Assert.Equal([new MemberActivityDay(DateOnly.FromDateTime(Since), 1)], activity.Days);
    }

    [Fact]
    public async Task Should_IgnoreBreaks_When_ComputingActivity()
    {
        var shortBreak = PomodoroSession.Create(PomodoroSessionType.ShortBreak, 5, _lea);
        Assert.True(shortBreak.Start(Start).IsSuccess);
        Assert.True(shortBreak.Complete(Start.AddMinutes(5)).IsSuccess);
        await SeedMembersAsync(shortBreak);

        var activity = await ActivityOfAsync(LeaName);

        AssertEmpty(activity);
    }

    [Fact]
    public async Task Should_IgnoreActiveSprint_When_ComputingActivity()
    {
        var active = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, _lea);
        Assert.True(active.Start(Start).IsSuccess);
        await SeedMembersAsync(active);

        var activity = await ActivityOfAsync(LeaName);

        AssertEmpty(activity);
    }

    [Fact]
    public async Task Should_CountOnlyTheNamedMember_When_SeveralMembersHaveSprints()
    {
        await SeedMembersAsync(
            CompletedFree(_lea, Start, Start.AddMinutes(25)),
            CompletedFree(_tom, Start, Start.AddMinutes(50)),
            CompletedFree(_tom, Start.AddHours(1), Start.AddHours(1).AddMinutes(50)));

        var activity = await ActivityOfAsync(LeaName);

        Assert.Equal(1, activity.CompletedSprints);
        Assert.Equal(TimeSpan.FromMinutes(25), activity.Focus);
    }

    // A session owner without a profile is invisible in any cross-user read (spec §9).
    [Fact]
    public async Task Should_ReturnEmptyActivity_When_OwnerIsNotMember()
    {
        await _database.SeedAsync(CompletedFree(_lea, Start, Start.AddMinutes(25)));

        var activity = await ActivityOfAsync(LeaName);

        AssertEmpty(activity);
    }

    // --- Active members today -------------------------------------------------------------------

    [Fact]
    public async Task Should_ListMember_When_SprintCompletedInWindow()
    {
        await SeedMembersAsync(EndedAt(_lea, From.AddHours(10)));

        var active = await ActiveAsync();

        Assert.Equal(1, active.Total);
        var member = Assert.Single(active.Members);
        Assert.Equal("Lea_Dev", member.Name.Value);
    }

    [Fact]
    public async Task Should_CountSprint_When_EndedAtWindowStart()
    {
        await SeedMembersAsync(EndedAt(_lea, From));

        var active = await ActiveAsync();

        Assert.Equal(1, active.Total);
        Assert.Single(active.Members);
    }

    [Fact]
    public async Task Should_IgnoreSprint_When_EndedJustBeforeWindow()
    {
        await SeedMembersAsync(EndedAt(_lea, From.AddSeconds(-1)));

        var active = await ActiveAsync();

        AssertNoActiveMember(active);
    }

    [Fact]
    public async Task Should_IgnoreSprint_When_EndedAtWindowEnd()
    {
        await SeedMembersAsync(EndedAt(_lea, To));

        var active = await ActiveAsync();

        AssertNoActiveMember(active);
    }

    // A session owner without a profile is invisible in any cross-user read (spec §9).
    [Fact]
    public async Task Should_IgnoreNonMember_When_SprintCompletedInWindow()
    {
        await SeedMembersAsync(EndedAt(_outsider, From.AddHours(10)));

        var active = await ActiveAsync();

        AssertNoActiveMember(active);
    }

    [Fact]
    public async Task Should_IgnoreBreaksInterruptedAndActive_When_InWindow()
    {
        var shortBreak = PomodoroSession.Create(PomodoroSessionType.ShortBreak, 5, _lea);
        Assert.True(shortBreak.Start(From.AddHours(9)).IsSuccess);
        Assert.True(shortBreak.Complete(From.AddHours(9).AddMinutes(5)).IsSuccess);

        var interrupted = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, _lea);
        Assert.True(interrupted.Start(From.AddHours(10)).IsSuccess);
        Assert.True(interrupted.Interrupt(From.AddHours(10).AddMinutes(10)).IsSuccess);

        var active = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, _lea);
        Assert.True(active.Start(From.AddHours(11)).IsSuccess);

        await SeedMembersAsync(shortBreak, interrupted, active);

        var result = await ActiveAsync();

        AssertNoActiveMember(result);
    }

    [Fact]
    public async Task Should_CountMemberOnce_When_SeveralSprints()
    {
        await SeedMembersAsync(
            EndedAt(_lea, From.AddHours(9)),
            EndedAt(_lea, From.AddHours(11)),
            EndedAt(_lea, From.AddHours(13)));

        var active = await ActiveAsync();

        Assert.Equal(1, active.Total);
        Assert.Single(active.Members);
    }

    // The order follows the LAST end of each member, ties are broken by the ordinal name.
    [Fact]
    public async Task Should_OrderByLastSprintEnd_When_SeveralMembers()
    {
        var zoe = UserId.New();
        var ada = UserId.New();
        await _database.SeedAsync(
            Member(_lea, LeaName),
            Member(_tom, TomName),
            Member(zoe, ProfileName.Create("Zoe_Dev").Value),
            Member(ada, ProfileName.Create("ada_dev").Value),
            EndedAt(_lea, From.AddHours(8)),
            EndedAt(_lea, From.AddHours(15)),
            EndedAt(_tom, From.AddHours(12)),
            EndedAt(zoe, From.AddHours(10)),
            EndedAt(ada, From.AddHours(10)),
            EndedAt(ada, From.AddHours(7)));

        var active = await ActiveAsync();

        Assert.Equal(4, active.Total);
        // Zoe_Dev before ada_dev: ordinal ('Z' < 'a'), not culture-sensitive.
        Assert.Equal(["Lea_Dev", "tomtom", "Zoe_Dev", "ada_dev"], active.Members.Select(m => m.Name.Value));
    }

    [Fact]
    public async Task Should_KeepTotal_When_MoreMembersThanMax()
    {
        var entities = new List<object>();
        for (var i = 1; i <= 7; i++)
        {
            var id = UserId.New();
            entities.Add(Member(id, ProfileName.Create($"Member{i:00}").Value));
            entities.Add(EndedAt(id, From.AddHours(i)));
        }

        await _database.SeedAsync([.. entities]);

        var active = await QueryAsync(r => r.GetActiveMembersAsync(From, To, 5));

        Assert.Equal(7, active.Total);
        Assert.Equal(["Member07", "Member06", "Member05", "Member04", "Member03"], active.Members.Select(m => m.Name.Value));
    }

    [Fact]
    public async Task Should_ReturnAvatar_When_Present()
    {
        await _database.SeedAsync(
            MemberProfile.Join(_lea, LeaName, ProfileNameSource.GitHub, "https://avatars.githubusercontent.com/u/1", JoinedAtUtc),
            Member(_tom, TomName),
            EndedAt(_lea, From.AddHours(10)),
            EndedAt(_tom, From.AddHours(9)));

        var active = await ActiveAsync();

        Assert.Equal("https://avatars.githubusercontent.com/u/1", active.Members.Single(m => m.Name.Value == "Lea_Dev").AvatarUrl);
        Assert.Null(active.Members.Single(m => m.Name.Value == "tomtom").AvatarUrl);
    }

    private Task<ActiveMembers> ActiveAsync() =>
        QueryAsync(r => r.GetActiveMembersAsync(From, To, 10));

    private static void AssertNoActiveMember(ActiveMembers active)
    {
        Assert.Equal(0, active.Total);
        Assert.Empty(active.Members);
    }

    private static PomodoroSession EndedAt(UserId owner, DateTime endedAt) =>
        CompletedFree(owner, endedAt.AddMinutes(-25), endedAt);

    private Task<MemberActivity> ActivityOfAsync(ProfileName name) =>
        QueryAsync(r => r.GetActivityAsync(name, Since));

    private async Task<T> QueryAsync<T>(Func<EfCoreMemberDirectoryReadRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreMemberDirectoryReadRepository(context));
    }

    private Task SeedMembersAsync(params PomodoroSession[] sessions) =>
        _database.SeedAsync([Member(_lea, LeaName), Member(_tom, TomName), .. sessions]);

    private static void AssertEmpty(MemberActivity activity)
    {
        Assert.Equal(0, activity.CompletedSprints);
        Assert.Equal(TimeSpan.Zero, activity.Focus);
        Assert.Equal(0, activity.SprintsTogether);
        Assert.Empty(activity.Days);
    }

    private static MemberProfile Member(UserId id, ProfileName name) =>
        MemberProfile.Join(id, name, ProfileNameSource.Chosen, avatarUrl: null, JoinedAtUtc);

    private static PomodoroSession StartedRoot(UserId owner, DateTime? startedAt)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, owner);
        Assert.True(session.Start(startedAt ?? Start).IsSuccess);
        return session;
    }

    private static PomodoroSession CompletedRoot(UserId owner, DateTime? startedAt = null)
    {
        var session = StartedRoot(owner, startedAt);
        Assert.True(session.Complete(session.StartedAt!.Value.AddMinutes(25)).IsSuccess);
        return session;
    }

    private static PomodoroSession InterruptedRoot(UserId owner)
    {
        var session = StartedRoot(owner, Start);
        Assert.True(session.Interrupt(Start.AddMinutes(10)).IsSuccess);
        return session;
    }

    private static PomodoroSession StartedJoined(UserId owner, PomodoroSession root)
    {
        var startedAt = root.StartedAt!.Value.AddMinutes(5);
        var session = PomodoroSession
            .CreateJoined(owner, root.Id, root.StartedAt.Value.AddMinutes(25), startedAt)
            .Value;
        Assert.True(session.Start(startedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession CompletedJoined(UserId owner, PomodoroSession root)
    {
        var session = StartedJoined(owner, root);
        Assert.True(session.Complete(root.StartedAt!.Value.AddMinutes(25)).IsSuccess);
        return session;
    }

    private static PomodoroSession InterruptedJoined(UserId owner, PomodoroSession root)
    {
        var session = StartedJoined(owner, root);
        Assert.True(session.Interrupt(root.StartedAt!.Value.AddMinutes(10)).IsSuccess);
        return session;
    }

    private static PomodoroSession CompletedFree(UserId owner, DateTime startedAt, DateTime endedAt)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, owner);
        Assert.True(session.Start(startedAt).IsSuccess);
        Assert.True(session.Complete(endedAt).IsSuccess);
        return session;
    }
}
