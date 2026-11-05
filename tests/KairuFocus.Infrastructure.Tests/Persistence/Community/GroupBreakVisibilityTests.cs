using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Repositories;
using KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

namespace KairuFocus.Infrastructure.Tests.Persistence.Community;

/// <summary>
/// A group break (UC-CO-10) carries the <c>SharedSprintId</c> of its sprint, like the sessions that joined it.
/// Every read across users that follows that link, or that counts sessions, must still ignore it: a break is
/// not in "En sprint maintenant", not a participant, not a sprint, not "fait ensemble".
/// </summary>
public sealed class GroupBreakVisibilityTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Since = new(2025, 10, 25, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DayStart = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);

    private static readonly ProfileName LeaName = ProfileName.Create("lea_dev").Value;
    private static readonly ProfileName TomName = ProfileName.Create("tomtom").Value;

    private readonly InMemoryDatabase _database = new();
    private readonly UserId _lea = UserId.New();
    private readonly UserId _tom = UserId.New();

    // --- Open sprints ---------------------------------------------------------------------------

    [Fact]
    public async Task Should_NotListOrCountBreak_When_ReadingOpenGroups()
    {
        var root = ActiveRoot(_lea);
        await SeedAsync(root, ActiveGroupBreak(_tom, root));

        var groups = await OpenSprintsAsync(r => r.GetOpenGroupsAsync(Now, 10));

        var group = Assert.Single(groups);
        Assert.Equal(root.Id.Value, group.RootSessionId);
        Assert.Equal("lea_dev", group.Lead.Name.Value);
        Assert.Equal(1, group.ParticipantCount);
    }

    [Fact]
    public async Task Should_ListNoGroup_When_OnlyBreaksAreActive()
    {
        var root = CompletedRoot(_lea);
        await SeedAsync(root, ActiveGroupBreak(_lea, root), ActiveGroupBreak(_tom, root));

        var groups = await OpenSprintsAsync(r => r.GetOpenGroupsAsync(Now, 10));

        Assert.Empty(groups);
    }

    [Fact]
    public async Task Should_NotReturnBreakOwner_When_ReadingParticipants()
    {
        var root = ActiveRoot(_lea);
        await SeedAsync(root, ActiveGroupBreak(_tom, root));

        var participants = await OpenSprintsAsync(r => r.GetParticipantsAsync(root.Id.Value, Now));

        Assert.Equal(["lea_dev"], participants.Select(p => p.Name.Value));
    }

    [Fact]
    public async Task Should_ReturnNoParticipant_When_OnlyBreaksReferenceTheRoot()
    {
        var root = CompletedRoot(_lea);
        await SeedAsync(root, ActiveGroupBreak(_lea, root), ActiveGroupBreak(_tom, root));

        var participants = await OpenSprintsAsync(r => r.GetParticipantsAsync(root.Id.Value, Now));

        Assert.Empty(participants);
    }

    [Fact]
    public async Task Should_NotFindSprintOfBreakOwner_When_Joining()
    {
        var root = ActiveRoot(_lea);
        await SeedAsync(root, ActiveGroupBreak(_tom, root));

        var ofBreakOwner = await OpenSprintsAsync(r => r.FindJoinableAsync(TomName, Now));
        var ofHost = await OpenSprintsAsync(r => r.FindJoinableAsync(LeaName, Now));

        Assert.Null(ofBreakOwner);
        Assert.NotNull(ofHost);
    }

    // --- Profile --------------------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_NotCountTogether_When_OnlyBreaksReferenceTheSprint(bool breakIsCompleted)
    {
        var root = CompletedRoot(_lea);
        var groupBreak = ActiveGroupBreak(_tom, root);
        if (breakIsCompleted)
            Assert.True(groupBreak.Complete(Now.AddMinutes(5)).IsSuccess);
        await SeedAsync(root, groupBreak);

        var host = await ActivityOfAsync(LeaName);

        Assert.Equal(1, host.CompletedSprints);
        Assert.Equal(0, host.SprintsTogether);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_ShowNoActivity_When_MemberOnlyTookTheBreak(bool breakIsCompleted)
    {
        var root = CompletedRoot(_lea);
        var groupBreak = ActiveGroupBreak(_tom, root);
        if (breakIsCompleted)
            Assert.True(groupBreak.Complete(Now.AddMinutes(5)).IsSuccess);
        await SeedAsync(root, groupBreak);

        var activity = await ActivityOfAsync(TomName);

        Assert.Equal(0, activity.CompletedSprints);
        Assert.Equal(TimeSpan.Zero, activity.Focus);
        Assert.Equal(0, activity.SprintsTogether);
        Assert.Empty(activity.Days);
    }

    // --- Presence and totals --------------------------------------------------------------------

    [Fact]
    public async Task Should_NotCountBreakAsSprintInProgress_When_ReadingPresence()
    {
        var root = CompletedRoot(_lea);
        await SeedAsync(root, ActiveGroupBreak(_tom, root));

        var inProgress = await CommunityActivityAsync(r => r.CountSprintsInProgressAsync(Now));

        Assert.Equal(0, inProgress);
    }

    [Fact]
    public async Task Should_NotCountBreakAsSprint_When_AggregatingCommunity()
    {
        var root = CompletedRoot(_lea);
        var groupBreak = ActiveGroupBreak(_tom, root);
        Assert.True(groupBreak.Complete(Now.AddMinutes(5)).IsSuccess);
        await SeedAsync(root, groupBreak);

        var activeMembers = await CommunityActivityAsync(r => r.CountActiveMembersAsync(DayStart, DayStart.AddDays(1)));
        var totals = await CommunityActivityAsync(r => r.GetCompletedSprintTotalsAsync(DayStart, DayStart.AddDays(1)));

        // Only the sprint of the host: the break of the other member is neither a sprint nor focus.
        Assert.Equal(1, activeMembers);
        Assert.Equal(1, totals.Sprints);
        Assert.Equal(TimeSpan.FromMinutes(25), totals.Focus);
    }

    // --- Helpers --------------------------------------------------------------------------------

    private Task SeedAsync(params PomodoroSession[] sessions) =>
        _database.SeedAsync(
        [
            MemberProfile.Join(_lea, LeaName, ProfileNameSource.Chosen, avatarUrl: null, new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
            MemberProfile.Join(_tom, TomName, ProfileNameSource.Chosen, avatarUrl: null, new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
            .. sessions,
        ]);

    private async Task<T> OpenSprintsAsync<T>(Func<EfCoreOpenSprintReadRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreOpenSprintReadRepository(context));
    }

    private async Task<MemberActivity> ActivityOfAsync(ProfileName name)
    {
        await using var context = _database.CreateContext();
        return await new EfCoreMemberDirectoryReadRepository(context).GetActivityAsync(name, Since);
    }

    private async Task<T> CommunityActivityAsync<T>(Func<EfCoreCommunityActivityRepository, Task<T>> query)
    {
        await using var context = _database.CreateContext();
        return await query(new EfCoreCommunityActivityRepository(context));
    }

    /// <summary>A sprint of 25 minutes, still running: it ends in ten minutes.</summary>
    private static PomodoroSession ActiveRoot(UserId owner)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, owner);
        Assert.True(session.Start(Now.AddMinutes(-15)).IsSuccess);
        return session;
    }

    /// <summary>A sprint of 25 minutes that ended at <see cref="Now"/>.</summary>
    private static PomodoroSession CompletedRoot(UserId owner)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, owner);
        Assert.True(session.Start(Now.AddMinutes(-25)).IsSuccess);
        Assert.True(session.Complete(Now).IsSuccess);
        return session;
    }

    /// <summary>A break of the group of <paramref name="root"/>, started at <see cref="Now"/> and lasting five minutes.</summary>
    private static PomodoroSession ActiveGroupBreak(UserId owner, PomodoroSession root)
    {
        var created = PomodoroSession.CreateJoinedBreak(owner, root.Id, Now.AddMinutes(5), Now);
        Assert.True(created.IsSuccess);
        Assert.True(created.Value.Start(Now).IsSuccess);
        return created.Value;
    }
}
