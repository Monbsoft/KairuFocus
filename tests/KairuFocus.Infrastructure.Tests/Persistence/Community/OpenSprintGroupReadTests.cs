using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// The group sessions in the reads across users (#46): a group is read from <c>GroupSessions</c>, led by its
/// host, visible during its steps (sprints and breaks) and while it waits for the host, and gone once the
/// host wait limit has passed. Its members come from <c>GroupSessionMembers</c>.
/// </summary>
public sealed class OpenSprintGroupReadTests
{
    private const int MaxGroups = 10;

    private static readonly DateTime Now = new(2026, 11, 8, 14, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryDatabase _database = new();
    private readonly UserId _lea = UserId.New();
    private readonly UserId _tom = UserId.New();

    // --- Open groups ----------------------------------------------------------------------------

    [Fact]
    public async Task Should_ListGroupAsBreak_When_HostLaunchedShortBreak()
    {
        var group = GroupInShortBreak();
        await SeedAsync(group);

        var listed = Assert.Single(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));

        Assert.Equal(group.Id, listed.RootSessionId);
        Assert.Equal("lea_dev", listed.Lead.Name.Value);
        Assert.Equal("https://avatars.githubusercontent.com/u/1", listed.Lead.AvatarUrl);
        Assert.Equal(Now.AddMinutes(2), listed.EndsAtUtc);
        Assert.Equal(DateTimeKind.Utc, listed.EndsAtUtc.Kind);
        Assert.Equal(2, listed.ParticipantCount);
        Assert.Equal(GroupStepType.ShortBreak, listed.StepType);
        Assert.Equal(GroupPhase.Running, listed.Phase);
    }

    [Fact]
    public async Task Should_ListGroupAsWaiting_When_StepEndedWithin15Minutes()
    {
        // One second before the host wait limit: still waiting.
        var stepEnd = Now - GroupRules.HostWaitLimit + TimeSpan.FromSeconds(1);
        var group = Group(_lea, stepEnd.AddMinutes(-25), stepEnd, _tom);
        await SeedAsync(group);

        var listed = Assert.Single(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));

        Assert.Equal(GroupPhase.WaitingForHost, listed.Phase);
        Assert.Equal(GroupStepType.Sprint, listed.StepType);
        Assert.Equal(stepEnd, listed.EndsAtUtc);
        Assert.Equal("lea_dev", listed.Lead.Name.Value);
    }

    // The absence of the host is computed: the group is over at end + 15 min, even though nothing recorded it.
    [Fact]
    public async Task Should_HideGroup_When_HostAbsentLimitPassed()
    {
        var stepEnd = Now - GroupRules.HostWaitLimit;
        await SeedAsync(Group(_lea, stepEnd.AddMinutes(-25), stepEnd, _tom));

        Assert.Empty(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Lea, Now)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Tom, Now)));
    }

    [Fact]
    public async Task Should_HideGroup_When_Ended()
    {
        var group = Group(_lea, Now.AddMinutes(-5), Now.AddMinutes(20), _tom);
        Assert.True(group.Stop(GroupSessionEndReason.Stopped, Now.AddMinutes(-1)).IsSuccess);
        await SeedAsync(group);

        Assert.Empty(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Tom, Now)));
    }

    // Closing acts on the group: the host's first sprint, root of the group, is not closed itself and must
    // not come back as a sprint without group.
    [Fact]
    public async Task Should_HideGroup_When_Closed()
    {
        var root = Sprint(_lea, Now.AddMinutes(-5), plannedMinutes: 25);
        var group = GroupOn(root, _tom, Now.AddMinutes(-3));
        Assert.True(group.Close().IsSuccess);
        await SeedAsync(root, Joined(_tom, root.Id, Now.AddMinutes(20), Now.AddMinutes(-3)), group);

        Assert.Empty(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Lea, Now)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Tom, Now)));
    }

    [Fact]
    public async Task Should_ListRootOnce_When_GroupExists()
    {
        var root = Sprint(_lea, Now.AddMinutes(-5), plannedMinutes: 25);
        var group = GroupOn(root, _tom, Now.AddMinutes(-3));
        await SeedAsync(root, Joined(_tom, root.Id, Now.AddMinutes(20), Now.AddMinutes(-3)), group);

        var listed = Assert.Single(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));

        Assert.Equal(root.Id.Value, listed.RootSessionId);
        Assert.Equal("lea_dev", listed.Lead.Name.Value);
        Assert.Equal(Now.AddMinutes(20), listed.EndsAtUtc);
        Assert.Equal(2, listed.ParticipantCount);
        Assert.Equal(GroupStepType.Sprint, listed.StepType);
        Assert.Equal(GroupPhase.Running, listed.Phase);
    }

    // The host stays the lead even when a member joined earlier than the host's current step started.
    [Fact]
    public async Task Should_LeadWithHost_When_GroupIsListed()
    {
        var group = Group(_lea, Now.AddMinutes(-5), Now.AddMinutes(20), _tom, joinedAt: Now.AddMinutes(-4));
        await SeedAsync(group);

        var listed = Assert.Single(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));

        Assert.Equal("lea_dev", listed.Lead.Name.Value);
    }

    // Rows of users without a profile stay until the group is deleted: the reads ignore them.
    [Fact]
    public async Task Should_CountOnlyMembersWithProfile_When_MemberLeftCommunity()
    {
        var withoutProfile = UserId.New();
        var departed = UserId.New();
        var group = Group(_lea, Now.AddMinutes(-5), Now.AddMinutes(20), _tom);
        Assert.True(group.Join(withoutProfile, Now.AddMinutes(-2)).IsSuccess);
        Assert.True(group.Join(departed, Now.AddMinutes(-2)).IsSuccess);
        Assert.True(group.Leave(departed, Now.AddMinutes(-1)).IsSuccess);
        await SeedAsync([group, Profile(departed, "noa_42")]);

        var listed = Assert.Single(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));
        var members = await QueryAsync(r => r.GetGroupMembersAsync(group.Id));

        Assert.Equal(2, listed.ParticipantCount);
        Assert.Equal(["lea_dev", "tomtom"], members.Select(m => m.Name.Value));
    }

    [Fact]
    public async Task Should_HideGroup_When_HostHasNoProfile()
    {
        var stranger = UserId.New();
        await SeedAsync(Group(stranger, Now.AddMinutes(-5), Now.AddMinutes(20), _tom));

        Assert.Empty(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Tom, Now)));
    }

    // Groups and sprints without group share one order: most time left first, a waiting group last.
    [Fact]
    public async Task Should_OrderByEnd_When_GroupsAndSoloSprintsAreListed()
    {
        var (noa, zoe, ben) = (UserId.New(), UserId.New(), UserId.New());
        await SeedAsync(
        [
            Group(_lea, Now.AddMinutes(-30), Now.AddMinutes(-5), _tom),
            Profile(noa, "noa_42"),
            Profile(zoe, "zoe_ux"),
            Profile(ben, "ben_dev"),
            Sprint(noa, Now.AddMinutes(-15), plannedMinutes: 25),
            Group(zoe, Now.AddMinutes(-5), Now.AddMinutes(20), ben),
        ]);

        var groups = await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups));

        Assert.Equal(["zoe_ux", "noa_42", "lea_dev"], groups.Select(g => g.Lead.Name.Value));
        Assert.Equal([2, 1, 2], groups.Select(g => g.ParticipantCount));
    }

    // An older client lets a host start a sprint of its own while its group waits: one row per lead, the one
    // with the most time left.
    [Fact]
    public async Task Should_ListLeadOnce_When_HostRunsSoloSprintWhileGroupWaits()
    {
        var solo = Sprint(_lea, Now.AddMinutes(-2), plannedMinutes: 25);
        await SeedAsync(Group(_lea, Now.AddMinutes(-30), Now.AddMinutes(-5), _tom), solo);

        var listed = Assert.Single(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));

        Assert.Equal(solo.Id.Value, listed.RootSessionId);
        Assert.Equal(GroupPhase.Running, listed.Phase);
    }

    [Fact]
    public async Task Should_ListSoloSprintAsRunningSprint_When_NobodyJoined()
    {
        var root = Sprint(_lea, Now.AddMinutes(-5), plannedMinutes: 25);
        await SeedAsync(root);

        var listed = Assert.Single(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));

        Assert.Equal(root.Id.Value, listed.RootSessionId);
        Assert.Equal(Now.AddMinutes(20), listed.EndsAtUtc);
        Assert.Equal(1, listed.ParticipantCount);
        Assert.Equal(GroupStepType.Sprint, listed.StepType);
        Assert.Equal(GroupPhase.Running, listed.Phase);
    }

    [Fact]
    public async Task Should_NeverListSoloBreak()
    {
        await SeedAsync(
            Started(_lea, PomodoroSessionType.ShortBreak, 5, Now.AddMinutes(-1)),
            Started(_tom, PomodoroSessionType.LongBreak, 15, Now.AddMinutes(-1)));

        Assert.Empty(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Lea, Now)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Tom, Now)));
    }

    // Sessions joined before #46 have no group row: they are no longer a group of their own.
    [Fact]
    public async Task Should_NotListJoinedSession_When_GroupRowIsMissing()
    {
        await SeedAsync(Joined(_tom, PomodoroSessionId.New(), Now.AddMinutes(20), Now.AddMinutes(-3)));

        Assert.Empty(await QueryAsync(r => r.GetOpenGroupsAsync(Now, MaxGroups)));
        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Tom, Now)));
    }

    // --- Joinable step of a name ----------------------------------------------------------------

    [Fact]
    public async Task Should_FindJoinableBreak_When_TargetIsMemberDuringBreak()
    {
        var group = GroupInShortBreak();
        await SeedAsync(group);

        var ofMember = await QueryAsync(r => r.FindJoinableAsync(Tom, Now));
        var ofHost = await QueryAsync(r => r.FindJoinableAsync(ProfileName.Create("LEA_DEV").Value, Now));

        var expected = new JoinableSprint(group.Id, Now.AddMinutes(2), GroupStepType.ShortBreak, GroupPhase.Running);
        Assert.Equal(expected, ofMember);
        Assert.Equal(expected, ofHost);
    }

    [Fact]
    public async Task Should_FindWaitingGroup_When_HostHasNotLaunchedNextStep()
    {
        var group = Group(_lea, Now.AddMinutes(-30), Now.AddMinutes(-5), _tom);
        await SeedAsync(group);

        var sprint = await QueryAsync(r => r.FindJoinableAsync(Tom, Now));

        Assert.Equal(new JoinableSprint(group.Id, Now.AddMinutes(-5), GroupStepType.Sprint, GroupPhase.WaitingForHost), sprint);
    }

    // A member who left the group no longer leads to it.
    [Fact]
    public async Task Should_NotFindGroup_When_TargetLeftIt()
    {
        var group = Group(_lea, Now.AddMinutes(-5), Now.AddMinutes(20), _tom);
        Assert.True(group.Leave(_tom, Now.AddMinutes(-1)).IsSuccess);
        await SeedAsync(group);

        Assert.Null(await QueryAsync(r => r.FindJoinableAsync(Tom, Now)));
        Assert.NotNull(await QueryAsync(r => r.FindJoinableAsync(Lea, Now)));
    }

    [Fact]
    public async Task Should_FindSoloSprint_When_TargetHasNoGroup()
    {
        var root = Sprint(_lea, Now.AddMinutes(-5), plannedMinutes: 25);
        await SeedAsync(root);

        var sprint = await QueryAsync(r => r.FindJoinableAsync(Lea, Now));

        Assert.Equal(new JoinableSprint(root.Id.Value, Now.AddMinutes(20), GroupStepType.Sprint, GroupPhase.Running), sprint);
    }

    // --- Members of a group ---------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnHostThenActiveMembers_When_ReadingGroupMembers()
    {
        var (first, second, departed) = (UserId.New(), UserId.New(), UserId.New());
        var group = Group(_lea, Now.AddMinutes(-10), Now.AddMinutes(15), first, joinedAt: Now.AddMinutes(-8));
        Assert.True(group.Join(second, Now.AddMinutes(-6)).IsSuccess);
        Assert.True(group.Join(departed, Now.AddMinutes(-5)).IsSuccess);
        Assert.True(group.Leave(departed, Now.AddMinutes(-4)).IsSuccess);
        await SeedAsync(
        [
            group,
            // Names in the reverse order of arrival: the order is the arrival, not the name.
            Profile(first, "zzz_first", "https://avatars.githubusercontent.com/u/3"),
            Profile(second, "aaa_second"),
            Profile(departed, "noa_42"),
        ]);

        var members = await QueryAsync(r => r.GetGroupMembersAsync(group.Id));

        Assert.Equal(
            [
                new SprintMember(Lea, "https://avatars.githubusercontent.com/u/1"),
                new SprintMember(ProfileName.Create("zzz_first").Value, "https://avatars.githubusercontent.com/u/3"),
                new SprintMember(ProfileName.Create("aaa_second").Value, null),
            ],
            members);
    }

    // Those already present stay visible to each other after the host closes the group, and during the wait.
    [Fact]
    public async Task Should_ReturnMembers_When_GroupIsClosedAndWaiting()
    {
        var group = Group(_lea, Now.AddMinutes(-30), Now.AddMinutes(-5), _tom);
        Assert.True(group.Close().IsSuccess);
        await SeedAsync(group);

        var members = await QueryAsync(r => r.GetGroupMembersAsync(group.Id));

        Assert.Equal(["lea_dev", "tomtom"], members.Select(m => m.Name.Value));
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_GroupIsUnknown()
    {
        await SeedAsync(Sprint(_lea, Now.AddMinutes(-5), plannedMinutes: 25));

        Assert.Empty(await QueryAsync(r => r.GetGroupMembersAsync(Guid.NewGuid())));
    }

    // --- Helpers --------------------------------------------------------------------------------

    private static ProfileName Lea => ProfileName.Create("lea_dev").Value;
    private static ProfileName Tom => ProfileName.Create("tomtom").Value;

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

    private Task SeedAsync(params object[] entities) =>
        _database.SeedAsync(
        [
            Profile(_lea, "lea_dev", "https://avatars.githubusercontent.com/u/1"),
            Profile(_tom, "tomtom"),
            .. entities,
        ]);

    private static MemberProfile Profile(UserId userId, string name, string? avatarUrl = null) =>
        MemberProfile.Join(
            userId,
            ProfileName.Create(name).Value,
            avatarUrl is null ? ProfileNameSource.Chosen : ProfileNameSource.GitHub,
            avatarUrl,
            new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc));

    private static PomodoroSession Sprint(UserId owner, DateTime startedAt, int plannedMinutes) =>
        Started(owner, PomodoroSessionType.Sprint, plannedMinutes, startedAt);

    private static PomodoroSession Started(UserId owner, PomodoroSessionType type, int plannedMinutes, DateTime startedAt)
    {
        var session = PomodoroSession.Create(type, plannedMinutes, owner);
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
