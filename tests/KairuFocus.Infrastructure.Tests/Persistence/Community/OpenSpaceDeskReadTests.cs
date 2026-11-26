using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// The desks of the open space (#50): the same rows as the open groups, each with the members seated at it and
/// the ambiance of its host; a desk read by its root, closed to joins or not; the ambiance of a host.
/// </summary>
public sealed class OpenSpaceDeskReadTests
{
    private const int MaxDesks = 10;
    private const string Lofi = "lofi-girl";
    private const string Jazz = "cafe-jazz";

    private static readonly DateTime Now = new(2026, 11, 26, 14, 0, 0, DateTimeKind.Utc);

    private static readonly AmbianceCatalogue Catalogue = new(
    [
        new Ambiance(Lofi, "Lofi Girl", AmbianceCategory.Lofi, AmbianceCatalogue.YouTubeVideoPrefix + "lofi"),
        new Ambiance(Jazz, "Cafe Jazz", AmbianceCategory.JazzBlues, AmbianceCatalogue.YouTubeVideoPrefix + "jazz"),
    ]);

    private readonly InMemoryDatabase _database = new();
    private readonly UserId _lea = UserId.New();
    private readonly UserId _tom = UserId.New();

    // --- Desks of the open space ----------------------------------------------------------------

    [Fact]
    public async Task Should_SeatHostFirstThenMembers_When_ReadingDesks()
    {
        var (first, second, departed, withoutProfile) = (UserId.New(), UserId.New(), UserId.New(), UserId.New());
        var group = Group(_lea, Now.AddMinutes(-10), Now.AddMinutes(15), first, joinedAt: Now.AddMinutes(-8));
        Assert.True(group.Join(second, Now.AddMinutes(-6)).IsSuccess);
        Assert.True(group.Join(withoutProfile, Now.AddMinutes(-6)).IsSuccess);
        Assert.True(group.Join(departed, Now.AddMinutes(-5)).IsSuccess);
        Assert.True(group.Leave(departed, Now.AddMinutes(-4)).IsSuccess);
        var solo = Sprint(_tom, Now.AddMinutes(-5), plannedMinutes: 25);
        await SeedAsync(
            group,
            solo,
            // Names in the reverse order of arrival: the order is the arrival, not the name.
            Profile(first, "zzz_first", "https://avatars.githubusercontent.com/u/3"),
            Profile(second, "aaa_second"),
            Profile(departed, "noa_42"));

        var desks = await QueryAsync(r => r.GetDesksAsync(Now, MaxDesks));

        Assert.Equal(2, desks.Count);
        var ofGroup = desks.Single(d => d.RootSessionId == group.Id);
        Assert.Equal(
            [
                new SprintMember(Name("lea_dev"), "https://avatars.githubusercontent.com/u/1"),
                new SprintMember(Name("zzz_first"), "https://avatars.githubusercontent.com/u/3"),
                new SprintMember(Name("aaa_second"), null),
            ],
            ofGroup.Seated);
        var ofSolo = desks.Single(d => d.RootSessionId == solo.Id.Value);
        Assert.Equal([new SprintMember(Name("tomtom"), null)], ofSolo.Seated);
    }

    // Arrival ties are broken by the name, ordinal (uppercase before lowercase).
    [Fact]
    public async Task Should_SeatMembersByName_When_TheyArrivedTogether()
    {
        var (lower, upper) = (UserId.New(), UserId.New());
        var group = Group(_lea, Now.AddMinutes(-10), Now.AddMinutes(15), lower, joinedAt: Now.AddMinutes(-8));
        Assert.True(group.Join(upper, Now.AddMinutes(-8)).IsSuccess);
        await SeedAsync(group, Profile(lower, "abc_dev"), Profile(upper, "Zed_dev"));

        var desk = Assert.Single(await QueryAsync(r => r.GetDesksAsync(Now, MaxDesks)));

        Assert.Equal(["lea_dev", "Zed_dev", "abc_dev"], desk.Seated.Select(m => m.Name.Value));
    }

    // Same candidates, preference, order and limit as the open groups: a lead with a waiting group and a running
    // sprint shows once, the closed rows are left out, the order is the time left and the limit applies.
    [Fact]
    public async Task Should_ReturnSameDesksAsOpenGroups_When_Queried()
    {
        var (noa, zoe, ben, ugo, max) = (UserId.New(), UserId.New(), UserId.New(), UserId.New(), UserId.New());
        var closedSprint = Sprint(ugo, Now.AddMinutes(-1), plannedMinutes: 25);
        Assert.True(closedSprint.CloseToJoin().IsSuccess);
        var closedGroup = Group(max, Now.AddMinutes(-2), Now.AddMinutes(23), ben);
        Assert.True(closedGroup.Close().IsSuccess);
        await SeedAsync(
            Profile(noa, "noa_42"),
            Profile(zoe, "zoe_ux"),
            Profile(ben, "ben_dev"),
            Profile(ugo, "ugo_ops"),
            Profile(max, "max_qa"),
            // lea: a waiting group and a running sprint of her own: the sprint wins.
            Group(_lea, Now.AddMinutes(-30), Now.AddMinutes(-5), _tom),
            Sprint(_lea, Now.AddMinutes(-2), plannedMinutes: 25),
            Sprint(noa, Now.AddMinutes(-15), plannedMinutes: 25),
            Group(zoe, Now.AddMinutes(-5), Now.AddMinutes(20), ben),
            closedSprint,
            closedGroup);

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxDesks));
        var desks = await QueryAsync(r => r.GetDesksAsync(Now, MaxDesks));
        var limited = await QueryAsync(r => r.GetDesksAsync(Now, 2));

        Assert.Equal(3, desks.Count);
        Assert.Equal(groups.Select(g => g.RootSessionId), desks.Select(d => d.RootSessionId));
        Assert.Equal(groups.Select(g => (g.EndsAtUtc, g.StepType, g.Phase)), desks.Select(d => (d.EndsAtUtc, d.StepType, d.Phase)));
        Assert.Equal(groups.Select(g => g.Lead), desks.Select(d => d.Seated[0]));
        Assert.All(desks, d => Assert.False(d.IsClosedToJoin));
        Assert.Equal(desks.Take(2).Select(d => d.RootSessionId), limited.Select(d => d.RootSessionId));
    }

    [Fact]
    public async Task Should_CarryHostAmbiance_When_HostChoseOne()
    {
        var noa = UserId.New();
        var group = Group(_lea, Now.AddMinutes(-5), Now.AddMinutes(20), noa);
        var solo = Sprint(_tom, Now.AddMinutes(-5), plannedMinutes: 25);
        // A member seated at the group chose another ambiance: the desk carries the host's.
        await SeedAsync([group, solo, Profile(noa, "noa_42", ambianceCode: Jazz)], leaAmbiance: Lofi);

        var desks = await QueryAsync(r => r.GetDesksAsync(Now, MaxDesks));

        Assert.Equal(Lofi, desks.Single(d => d.RootSessionId == group.Id).HostAmbianceCode);
        Assert.Null(desks.Single(d => d.RootSessionId == solo.Id.Value).HostAmbianceCode);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoDeskIsOpen()
    {
        await SeedAsync();

        Assert.Empty(await QueryAsync(r => r.GetDesksAsync(Now, MaxDesks)));
    }

    // --- Desk read by its root ------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnClosedDesk_When_ReadByRoot()
    {
        var group = Group(_lea, Now.AddMinutes(-5), Now.AddMinutes(20), _tom);
        Assert.True(group.Close().IsSuccess);
        var noa = UserId.New();
        var solo = Sprint(noa, Now.AddMinutes(-5), plannedMinutes: 25);
        Assert.True(solo.CloseToJoin().IsSuccess);
        await SeedAsync([group, solo, Profile(noa, "noa_42", ambianceCode: Jazz)], leaAmbiance: Lofi);

        var ofGroup = await QueryAsync(r => r.GetDeskAsync(group.Id, Now));
        var ofSolo = await QueryAsync(r => r.GetDeskAsync(solo.Id.Value, Now));

        Assert.NotNull(ofGroup);
        Assert.Equal(
            new OpenSpaceDesk(group.Id, ofGroup.Seated, Lofi, Now.AddMinutes(20), GroupStepType.Sprint, GroupPhase.Running, IsClosedToJoin: true),
            ofGroup);
        Assert.Equal(["lea_dev", "tomtom"], ofGroup.Seated.Select(m => m.Name.Value));
        Assert.NotNull(ofSolo);
        Assert.Equal(
            new OpenSpaceDesk(solo.Id.Value, ofSolo.Seated, Jazz, Now.AddMinutes(20), GroupStepType.Sprint, GroupPhase.Running, IsClosedToJoin: true),
            ofSolo);
        Assert.Equal([new SprintMember(Name("noa_42"), null)], ofSolo.Seated);
    }

    [Fact]
    public async Task Should_ReturnOpenDesk_When_ReadByRoot()
    {
        var group = GroupInShortBreak();
        await SeedAsync(group);

        var desk = await QueryAsync(r => r.GetDeskAsync(group.Id, Now));

        Assert.NotNull(desk);
        Assert.Equal(Now.AddMinutes(2), desk.EndsAtUtc);
        Assert.Equal(DateTimeKind.Utc, desk.EndsAtUtc.Kind);
        Assert.Equal(GroupStepType.ShortBreak, desk.StepType);
        Assert.Equal(GroupPhase.Running, desk.Phase);
        Assert.False(desk.IsClosedToJoin);
        Assert.Null(desk.HostAmbianceCode);
    }

    [Fact]
    public async Task Should_ReturnWaitingDesk_When_HostHasNotLaunchedNextStep()
    {
        var group = Group(_lea, Now.AddMinutes(-30), Now.AddMinutes(-5), _tom);
        await SeedAsync(group);

        var desk = await QueryAsync(r => r.GetDeskAsync(group.Id, Now));

        Assert.NotNull(desk);
        Assert.Equal(GroupPhase.WaitingForHost, desk.Phase);
    }

    [Fact]
    public async Task Should_ReturnNull_When_DeskEnded()
    {
        // A group stopped while its root sprint is still active: the group row stands for the root.
        var root = Sprint(_lea, Now.AddMinutes(-5), plannedMinutes: 25);
        var stopped = GroupOn(root, _tom, Now.AddMinutes(-3));
        Assert.True(stopped.Stop(GroupSessionEndReason.Stopped, Now.AddMinutes(-1)).IsSuccess);
        // A group whose host wait limit has passed.
        var noa = UserId.New();
        var stepEnd = Now - GroupRules.HostWaitLimit;
        var abandoned = Group(noa, stepEnd.AddMinutes(-25), stepEnd, _tom);
        // A sprint over: completed, and one past its planned end.
        var (zoe, ben) = (UserId.New(), UserId.New());
        var completed = Sprint(zoe, Now.AddMinutes(-30), plannedMinutes: 25);
        Assert.True(completed.Complete(Now.AddMinutes(-5)).IsSuccess);
        var expired = Sprint(ben, Now.AddMinutes(-30), plannedMinutes: 25);
        await SeedAsync(
            root, stopped, abandoned, completed, expired,
            Profile(noa, "noa_42"), Profile(zoe, "zoe_ux"), Profile(ben, "ben_dev"));

        Assert.Null(await QueryAsync(r => r.GetDeskAsync(root.Id.Value, Now)));
        Assert.Null(await QueryAsync(r => r.GetDeskAsync(abandoned.Id, Now)));
        Assert.Null(await QueryAsync(r => r.GetDeskAsync(completed.Id.Value, Now)));
        Assert.Null(await QueryAsync(r => r.GetDeskAsync(expired.Id.Value, Now)));
        Assert.Null(await QueryAsync(r => r.GetDeskAsync(Guid.NewGuid(), Now)));
    }

    // --- Ambiance of a host ---------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnHostAmbiance_When_RootIsGroup()
    {
        var group = Group(_lea, Now.AddMinutes(-5), Now.AddMinutes(20), _tom);
        await SeedAsync([group], leaAmbiance: Lofi, tomAmbiance: Jazz);

        Assert.Equal(Lofi, await QueryAsync(r => r.GetHostAmbianceCodeAsync(group.Id)));
    }

    [Fact]
    public async Task Should_ReturnOwnerAmbiance_When_RootIsSoloSprint()
    {
        var solo = Sprint(_tom, Now.AddMinutes(-5), plannedMinutes: 25);
        await SeedAsync([solo], leaAmbiance: Lofi, tomAmbiance: Jazz);

        Assert.Equal(Jazz, await QueryAsync(r => r.GetHostAmbianceCodeAsync(solo.Id.Value)));
    }

    [Fact]
    public async Task Should_ReturnNullAmbiance_When_RootIsUnknownOrHostChoseNone()
    {
        var solo = Sprint(_tom, Now.AddMinutes(-5), plannedMinutes: 25);
        // A joined step is not a root: its owner is not the host.
        var joined = Joined(_tom, PomodoroSessionId.New(), Now.AddMinutes(20), Now.AddMinutes(-3));
        await SeedAsync([solo, joined], leaAmbiance: null, tomAmbiance: Jazz);

        Assert.Null(await QueryAsync(r => r.GetHostAmbianceCodeAsync(Guid.NewGuid())));
        Assert.Null(await QueryAsync(r => r.GetHostAmbianceCodeAsync(joined.Id.Value)));
        await using (var context = _database.CreateContext())
        {
            var tom = await context.MemberProfiles.FindAsync(_tom);
            Assert.True(tom!.ChooseAmbiance(null, Catalogue).IsSuccess);
            await context.SaveChangesAsync();
        }
        Assert.Null(await QueryAsync(r => r.GetHostAmbianceCodeAsync(solo.Id.Value)));
    }

    // --- Helpers --------------------------------------------------------------------------------

    private static ProfileName Name(string value) => ProfileName.Create(value).Value;

    /// <summary>First sprint from 30 to 5 minutes ago, then a short break launched 3 minutes ago, ending in 2.</summary>
    private GroupSession GroupInShortBreak()
    {
        var group = Group(_lea, Now.AddMinutes(-30), Now.AddMinutes(-5), _tom, joinedAt: Now.AddMinutes(-25));
        Assert.True(group.LaunchStep(GroupStepType.ShortBreak, Now.AddMinutes(-3), Now.AddMinutes(2), Now.AddMinutes(-3)).IsSuccess);
        return group;
    }

    private static GroupSession Group(UserId host, DateTime startedAt, DateTime endsAt, UserId firstMember, DateTime? joinedAt = null)
    {
        var started = GroupSession.Start(Guid.NewGuid(), host, startedAt, endsAt, firstMember, joinedAt ?? startedAt.AddMinutes(1));
        Assert.True(started.IsSuccess);
        return started.Value;
    }

    private static GroupSession GroupOn(PomodoroSession root, UserId firstMember, DateTime joinedAt)
    {
        var startedAt = root.StartedAt!.Value;
        var started = GroupSession.Start(
            root.Id.Value, root.OwnerId!, startedAt, startedAt.AddMinutes(root.PlannedDurationMinutes), firstMember, joinedAt);
        Assert.True(started.IsSuccess);
        return started.Value;
    }

    private Task SeedAsync(params object[] entities) => SeedAsync(entities, leaAmbiance: null);

    private Task SeedAsync(object[] entities, string? leaAmbiance, string? tomAmbiance = null) =>
        _database.SeedAsync(
        [
            Profile(_lea, "lea_dev", "https://avatars.githubusercontent.com/u/1", leaAmbiance),
            Profile(_tom, "tomtom", ambianceCode: tomAmbiance),
            .. entities,
        ]);

    private static MemberProfile Profile(UserId userId, string name, string? avatarUrl = null, string? ambianceCode = null)
    {
        var profile = MemberProfile.Join(
            userId,
            Name(name),
            avatarUrl is null ? ProfileNameSource.Chosen : ProfileNameSource.GitHub,
            avatarUrl,
            new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc));
        Assert.True(profile.ChooseAmbiance(ambianceCode, Catalogue).IsSuccess);
        return profile;
    }

    private static PomodoroSession Sprint(UserId owner, DateTime startedAt, int plannedMinutes)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, plannedMinutes, owner);
        Assert.True(session.Start(startedAt).IsSuccess);
        return session;
    }

    private static PomodoroSession Joined(UserId owner, PomodoroSessionId rootId, DateTime sharedEndAt, DateTime startedAt)
    {
        var joined = PomodoroSession.CreateJoinedStep(PomodoroSessionType.Sprint, owner, rootId, sharedEndAt, startedAt);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(startedAt).IsSuccess);
        return joined.Value;
    }

    private async Task<T> QueryAsync<T>(Func<EfCoreOpenSprintReadRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreOpenSprintReadRepository(context));
    }
}
