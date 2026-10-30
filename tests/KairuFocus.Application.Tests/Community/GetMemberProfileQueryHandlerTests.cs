using System.Text.Json;
using System.Text.RegularExpressions;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetMemberProfile;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Gamification;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetMemberProfileQueryHandlerTests
{
    private const string Target = "Lea_Dev";
    private const string TargetAvatar = "https://avatars.githubusercontent.com/u/1";
    private const string StandingsZone = "standings";
    private const string ActivityZone = "activity";
    private const string OpenSprintZone = "open-sprint";

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime JoinedAtUtc = new(2026, 3, 17, 8, 0, 0, DateTimeKind.Utc);

    private static readonly Regex AnyGuid = new(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.CultureInvariant);

    private readonly FakeMemberDirectoryReadRepository _directory = new();
    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeLeaderboardStandingsRepository _standings = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();

    private GetMemberProfileQueryHandler CreateSut(
        IMemberDirectoryReadRepository? directory = null,
        ILeaderboardStandingsRepository? standings = null) =>
        new(
            directory ?? _directory,
            _profiles,
            standings ?? _standings,
            _openSprints,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now),
            NullLogger<GetMemberProfileQueryHandler>.Instance);

    private Task<GetMemberProfileResult> Query(
        string name = Target,
        IMemberDirectoryReadRepository? directory = null,
        ILeaderboardStandingsRepository? standings = null) =>
        CreateSut(directory, standings).Handle(new GetMemberProfileQuery(name));

    private static PublicMemberProfile PublicProfile(
        string name = Target,
        ProfileNameSource source = ProfileNameSource.GitHub,
        string? avatarUrl = TargetAvatar) =>
        new(ProfileName.Create(name).Value, source, avatarUrl, JoinedAtUtc);

    private static SprintMember Participant(string name, string? avatarUrl = null) =>
        new(ProfileName.Create(name).Value, avatarUrl);

    // --- Not found ------------------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnNotFound_When_NameIsUnknown()
    {
        var result = await Query("inconnu");

        Assert.Equal(new GetMemberProfileResult(false, null), result);
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("moi")]
    public async Task Should_ReturnNotFound_When_NameIsMalformedOrReserved(string name)
    {
        var result = await Query(name);

        Assert.Equal(new GetMemberProfileResult(false, null), result);
    }

    // --- Composition ----------------------------------------------------------------------------

    [Fact]
    public async Task Should_ComposeZones_When_MemberExists()
    {
        var rootId = Guid.NewGuid();
        var endsAt = Now.UtcDateTime.AddMinutes(18);
        _directory.Seed(
            PublicProfile(),
            new MemberActivity(
                CompletedSprints: 12,
                Focus: TimeSpan.FromMinutes(300),
                SprintsTogether: 4,
                Days: [new MemberActivityDay(new DateOnly(2026, 10, 28), 2), new MemberActivityDay(new DateOnly(2026, 10, 29), 1)]));
        _standings.Add("first", 200);
        _standings.Add(Target, 100);
        _standings.Add("third", 50);
        _openSprints.SeedJoinable(Target, new JoinableSprint(rootId, endsAt));
        _openSprints.SeedParticipants(rootId, Participant(Target, TargetAvatar), Participant("tomtom", "https://avatars.githubusercontent.com/u/2"));
        _profiles.Seed(FakeCurrentUserService.TestUserId, "viewer");

        var result = await Query();

        Assert.True(result.Found);
        var profile = Assert.IsType<MemberProfileViewModel>(result.Profile);
        Assert.Equal(Target, profile.Name);
        Assert.True(profile.IsGitHub);
        Assert.Equal(TargetAvatar, profile.AvatarUrl);
        Assert.Equal(new DateOnly(2026, 3, 1), profile.JoinedMonth);
        Assert.False(profile.IsMe);
        Assert.True(profile.ViewerIsMember);

        Assert.Equal(new MemberStandingViewModel(100, 2), profile.Standing);
        Assert.False(profile.StandingFailed);

        var activity = Assert.IsType<MemberActivityViewModel>(profile.Activity);
        Assert.Equal(12, activity.Sprints);
        Assert.Equal(300, activity.FocusMinutes);
        Assert.Equal(4, activity.SprintsTogether);
        Assert.Equal(
            [new ActivityDayViewModel(new DateOnly(2026, 10, 28), 2), new ActivityDayViewModel(new DateOnly(2026, 10, 29), 1)],
            activity.Days);
        Assert.False(profile.ActivityFailed);

        var sprint = Assert.IsType<OpenSprintStatusViewModel>(profile.OpenSprint);
        Assert.Equal(new DateTimeOffset(endsAt), sprint.EndsAt);
        Assert.Equal([new SprintMemberViewModel("tomtom", "https://avatars.githubusercontent.com/u/2")], sprint.Others);
        Assert.False(profile.OpenSprintFailed);
    }

    // 371 days including today, as the local heatmap of the Statistics page.
    [Fact]
    public async Task Should_ReadHeatmapOver371UtcDays_When_MemberExists()
    {
        _directory.Seed(PublicProfile());

        await Query();

        Assert.Equal(new DateTime(2025, 10, 25, 0, 0, 0, DateTimeKind.Utc), _directory.LastHeatmapSinceUtc);
    }

    [Fact]
    public async Task Should_QueryAllTimeStandings_When_ComputingRank()
    {
        _directory.Seed(PublicProfile());

        await Query();

        Assert.Equal(1, _standings.AllTimeCallCount);
        Assert.Empty(_standings.BetweenWindows);
    }

    // The URL may differ in case from the stored name: the row of the member and the target excluded from
    // the participants are found on the stored name.
    [Fact]
    public async Task Should_FindStandingAndExcludeTarget_When_NameCaseDiffers()
    {
        var rootId = Guid.NewGuid();
        _directory.Seed(PublicProfile());
        _standings.Add(Target, 100);
        _openSprints.SeedJoinable(Target, new JoinableSprint(rootId, Now.UtcDateTime.AddMinutes(18)));
        _openSprints.SeedParticipants(rootId, Participant(Target), Participant("tomtom"));

        var result = await Query("LEA_DEV");

        var profile = Assert.IsType<MemberProfileViewModel>(result.Profile);
        Assert.Equal(Target, profile.Name);
        Assert.Equal(new MemberStandingViewModel(100, 1), profile.Standing);
        Assert.Equal(["tomtom"], profile.OpenSprint!.Others.Select(o => o.Name));
    }

    [Fact]
    public async Task Should_ReturnNullRankAndEmptyActivity_When_MemberHasNoXpNorSprint()
    {
        _directory.Seed(PublicProfile());
        _standings.Add("first", 200);

        var profile = (await Query()).Profile!;

        Assert.Equal(new MemberStandingViewModel(0, null), profile.Standing);
        Assert.False(profile.StandingFailed);
        var activity = Assert.IsType<MemberActivityViewModel>(profile.Activity);
        Assert.Equal(0, activity.Sprints);
        Assert.Equal(0, activity.FocusMinutes);
        Assert.Equal(0, activity.SprintsTogether);
        Assert.Empty(activity.Days);
        Assert.False(profile.ActivityFailed);
        Assert.Null(profile.OpenSprint);
        Assert.False(profile.OpenSprintFailed);
    }

    [Fact]
    public async Task Should_ExcludeTargetFromOthers_When_SprintIsOpen()
    {
        var rootId = Guid.NewGuid();
        _directory.Seed(PublicProfile());
        _openSprints.SeedJoinable(Target, new JoinableSprint(rootId, Now.UtcDateTime.AddMinutes(18)));
        _openSprints.SeedParticipants(rootId, Participant("tomtom"), Participant(Target), Participant("zoe_ux"));

        var profile = (await Query()).Profile!;

        var sprint = Assert.IsType<OpenSprintStatusViewModel>(profile.OpenSprint);
        Assert.Equal(["tomtom", "zoe_ux"], sprint.Others.Select(o => o.Name));
    }

    [Fact]
    public async Task Should_ReturnEmptyOthers_When_TargetIsAloneInSprint()
    {
        var rootId = Guid.NewGuid();
        _directory.Seed(PublicProfile());
        _openSprints.SeedJoinable(Target, new JoinableSprint(rootId, Now.UtcDateTime.AddMinutes(18)));
        _openSprints.SeedParticipants(rootId, Participant(Target));

        var profile = (await Query()).Profile!;

        Assert.Empty(profile.OpenSprint!.Others);
    }

    // EF Core reads datetime2 as DateTimeKind.Unspecified: it must not be interpreted as local time.
    [Fact]
    public async Task Should_ExposeEndAsUtcInstant_When_StoredKindIsUnspecified()
    {
        var rootId = Guid.NewGuid();
        var stored = DateTime.SpecifyKind(Now.UtcDateTime.AddMinutes(18), DateTimeKind.Unspecified);
        _directory.Seed(PublicProfile());
        _openSprints.SeedJoinable(Target, new JoinableSprint(rootId, stored));

        var profile = (await Query()).Profile!;

        var endsAt = profile.OpenSprint!.EndsAt;
        Assert.Equal(TimeSpan.Zero, endsAt.Offset);
        Assert.Equal(Now.AddMinutes(18), endsAt);
    }

    [Fact]
    public async Task Should_ReportChosenName_When_NameIsNotFromGitHub()
    {
        _directory.Seed(PublicProfile(source: ProfileNameSource.Chosen, avatarUrl: null));

        var profile = (await Query()).Profile!;

        Assert.False(profile.IsGitHub);
        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public async Task Should_RoundFocusToMinutes_When_FocusHasSeconds()
    {
        _directory.Seed(PublicProfile(), new MemberActivity(2, TimeSpan.FromSeconds(160), 0, []));

        var profile = (await Query()).Profile!;

        Assert.Equal(3, profile.Activity!.FocusMinutes);
    }

    [Fact]
    public async Task Should_NotExposeAnyIdentifier_When_ProfileIsSerialized()
    {
        var rootId = Guid.NewGuid();
        _directory.Seed(PublicProfile());
        _standings.Add(Target, 100);
        _openSprints.SeedJoinable(Target, new JoinableSprint(rootId, Now.UtcDateTime.AddMinutes(18)));
        _openSprints.SeedParticipants(rootId, Participant(Target), Participant("tomtom"));
        _profiles.Seed(FakeCurrentUserService.TestUserId, "viewer");

        var json = JsonSerializer.Serialize(await Query());

        Assert.DoesNotMatch(AnyGuid, json);
    }

    // --- Viewer ---------------------------------------------------------------------------------

    [Fact]
    public async Task Should_FlagIsMe_When_ViewerIsTarget()
    {
        _directory.Seed(PublicProfile());
        _profiles.Seed(FakeCurrentUserService.TestUserId, Target);

        var profile = (await Query("lea_dev")).Profile!;

        Assert.True(profile.IsMe);
        Assert.True(profile.ViewerIsMember);
    }

    [Fact]
    public async Task Should_ReportViewerNotMember_When_ViewerHasNoProfile()
    {
        _directory.Seed(PublicProfile());
        _profiles.Seed(UserId.New(), "someone_else");

        var result = await Query();

        Assert.True(result.Found);
        Assert.False(result.Profile!.ViewerIsMember);
        Assert.False(result.Profile.IsMe);
    }

    // --- Zones ----------------------------------------------------------------------------------

    [Fact]
    public async Task Should_FlagOnlyFailedZone_When_StandingsReadFails()
    {
        SeedAllZones();

        var profile = (await Query(standings: new ThrowingStandings(new InvalidOperationException("boom")))).Profile!;

        Assert.True(profile.StandingFailed);
        Assert.Null(profile.Standing);
        Assert.False(profile.ActivityFailed);
        Assert.NotNull(profile.Activity);
        Assert.False(profile.OpenSprintFailed);
        Assert.NotNull(profile.OpenSprint);
    }

    [Fact]
    public async Task Should_FlagOnlyFailedZone_When_ActivityReadFails()
    {
        SeedAllZones();

        var profile = (await Query(directory: new ThrowingActivity(_directory, new InvalidOperationException("boom")))).Profile!;

        Assert.True(profile.ActivityFailed);
        Assert.Null(profile.Activity);
        Assert.False(profile.StandingFailed);
        Assert.NotNull(profile.Standing);
        Assert.False(profile.OpenSprintFailed);
        Assert.NotNull(profile.OpenSprint);
    }

    [Fact]
    public async Task Should_FlagOnlyFailedZone_When_OpenSprintReadFails()
    {
        SeedAllZones();
        _openSprints.ThrowOnRead = new InvalidOperationException("boom");

        var profile = (await Query()).Profile!;

        Assert.True(profile.OpenSprintFailed);
        Assert.Null(profile.OpenSprint);
        Assert.False(profile.StandingFailed);
        Assert.NotNull(profile.Standing);
        Assert.False(profile.ActivityFailed);
        Assert.NotNull(profile.Activity);
    }

    [Theory]
    [InlineData(StandingsZone)]
    [InlineData(ActivityZone)]
    [InlineData(OpenSprintZone)]
    public async Task Should_Rethrow_When_Cancelled(string zone)
    {
        SeedAllZones();
        var cancelled = new OperationCanceledException();
        if (zone == OpenSprintZone)
            _openSprints.ThrowOnRead = cancelled;

        var query = Query(
            directory: zone == ActivityZone ? new ThrowingActivity(_directory, cancelled) : null,
            standings: zone == StandingsZone ? new ThrowingStandings(cancelled) : null);

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(() => query);
        Assert.Same(cancelled, thrown);
    }

    private void SeedAllZones()
    {
        var rootId = Guid.NewGuid();
        _directory.Seed(PublicProfile(), new MemberActivity(1, TimeSpan.FromMinutes(25), 0, []));
        _standings.Add(Target, 100);
        _openSprints.SeedJoinable(Target, new JoinableSprint(rootId, Now.UtcDateTime.AddMinutes(18)));
        _openSprints.SeedParticipants(rootId, Participant(Target));
    }

    /// <summary>Standings whose reads fail: <see cref="FakeLeaderboardStandingsRepository"/> cannot fail.</summary>
    private sealed class ThrowingStandings(Exception failure) : ILeaderboardStandingsRepository
    {
        public Task<IReadOnlyList<XpStanding>> GetStandingsBetweenAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) =>
            throw failure;

        public Task<IReadOnlyList<XpStanding>> GetAllTimeStandingsAsync(CancellationToken cancellationToken = default) =>
            throw failure;
    }

    /// <summary>
    /// Finds the member but fails on the activity only: <see cref="FakeMemberDirectoryReadRepository.ThrowOnRead"/>
    /// fails both reads, and a failed lookup is not a zone.
    /// </summary>
    private sealed class ThrowingActivity(IMemberDirectoryReadRepository inner, Exception failure) : IMemberDirectoryReadRepository
    {
        public Task<PublicMemberProfile?> FindByNameAsync(ProfileName name, CancellationToken ct = default) =>
            inner.FindByNameAsync(name, ct);

        public Task<MemberActivity> GetActivityAsync(ProfileName name, DateTime heatmapSinceUtc, CancellationToken ct = default) =>
            throw failure;
    }
}
