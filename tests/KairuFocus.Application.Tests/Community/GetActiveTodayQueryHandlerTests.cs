using KairuFocus.Application.Community.Queries.GetActiveToday;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetActiveTodayQueryHandlerTests
{
    private readonly FakeMemberDirectoryReadRepository _directory = new();
    private readonly FakeMemberProfileRepository _profiles = new();

    private GetActiveTodayQueryHandler CreateSut(DateTimeOffset now) =>
        new(_directory, _profiles, new FakeCurrentUserService(), new FixedTimeProvider(now));

    private static SprintMember Member(string name, string? avatarUrl = null) =>
        new(ProfileName.Create(name).Value, avatarUrl);

    private static DateTime Utc(int month, int day) => new(2026, month, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Should_AskForTheUtcDay_When_Queried()
    {
        var sut = CreateSut(new DateTimeOffset(2026, 11, 5, 23, 30, 0, TimeSpan.Zero));

        await sut.Handle(new GetActiveTodayQuery());

        Assert.Equal((Utc(11, 5), Utc(11, 6), 5), _directory.LastActiveWindow);
    }

    [Fact]
    public async Task Should_AskForTheNewDay_When_QueriedAtMidnight()
    {
        var sut = CreateSut(new DateTimeOffset(2026, 11, 6, 0, 0, 0, TimeSpan.Zero));

        await sut.Handle(new GetActiveTodayQuery());

        Assert.Equal((Utc(11, 6), Utc(11, 7), 5), _directory.LastActiveWindow);
    }

    [Fact]
    public async Task Should_FlagMe_When_ViewerIsListed()
    {
        _profiles.Seed(FakeCurrentUserService.TestUserId, "nora");
        _directory.SeedActive(new ActiveMembers(2, [Member("lea_dev"), Member("NORA")]));
        var sut = CreateSut(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        var result = await sut.Handle(new GetActiveTodayQuery());

        Assert.Equal([false, true], result.Members.Select(m => m.IsMe));
    }

    [Fact]
    public async Task Should_FlagNobody_When_ViewerIsNotMember()
    {
        _directory.SeedActive(new ActiveMembers(2, [Member("lea_dev"), Member("nora")]));
        var sut = CreateSut(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        var result = await sut.Handle(new GetActiveTodayQuery());

        Assert.All(result.Members, m => Assert.False(m.IsMe));
    }

    [Fact]
    public async Task Should_FlagNobody_When_ViewerIsMemberButNotListed()
    {
        _profiles.Seed(FakeCurrentUserService.TestUserId, "nora");
        _directory.SeedActive(new ActiveMembers(2, [Member("lea_dev"), Member("alpha")]));
        var sut = CreateSut(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        var result = await sut.Handle(new GetActiveTodayQuery());

        Assert.All(result.Members, m => Assert.False(m.IsMe));
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NobodyFinishedASprint()
    {
        _directory.SeedActive(new ActiveMembers(0, []));
        var sut = CreateSut(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        var result = await sut.Handle(new GetActiveTodayQuery());

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Members);
    }

    [Fact]
    public async Task Should_KeepTotal_When_MoreMembersThanReturned()
    {
        var members = new[] { "alpha", "bravo", "charlie", "delta", "echo" }.Select(n => Member(n)).ToList();
        _directory.SeedActive(new ActiveMembers(7, members));
        var sut = CreateSut(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        var result = await sut.Handle(new GetActiveTodayQuery());

        Assert.Equal(7, result.Total);
        Assert.Equal(["alpha", "bravo", "charlie", "delta", "echo"], result.Members.Select(m => m.Name));
    }

    [Fact]
    public async Task Should_MapNameAndAvatar_When_Queried()
    {
        const string avatarUrl = "https://avatars.githubusercontent.com/u/1";
        _directory.SeedActive(new ActiveMembers(2, [Member("lea_dev", avatarUrl), Member("nora")]));
        var sut = CreateSut(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        var result = await sut.Handle(new GetActiveTodayQuery());

        Assert.Equal(["lea_dev", "nora"], result.Members.Select(m => m.Name));
        Assert.Equal([avatarUrl, null], result.Members.Select(m => m.AvatarUrl));
    }
}
