using KairuFocus.Application.Gamification.Queries.GetCommunityPresence;
using KairuFocus.Application.Tests.Common;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class GetCommunityPresenceQueryHandlerTests
{
    private readonly FakeCommunityActivityRepository _activityRepository = new();

    private GetCommunityPresenceQueryHandler CreateSut(DateTimeOffset now) =>
        new(_activityRepository, new FixedTimeProvider(now));

    [Fact]
    public async Task Should_PassCurrentInstant_When_CountingSprintsInProgress()
    {
        var now = new DateTimeOffset(2026, 10, 22, 14, 7, 30, TimeSpan.Zero);
        _activityRepository.SprintsInProgress = 12;

        var result = await CreateSut(now).Handle(new GetCommunityPresenceQuery());

        Assert.Equal(now.UtcDateTime, Assert.Single(_activityRepository.InProgressInstants));
        Assert.Equal(12, result.InSprintNow);
    }

    [Fact]
    public async Task Should_UseUtcDay_When_CountingMembersWhoSprintedToday()
    {
        // 2026-10-23 01:30 in Paris (UTC+2) is still 2026-10-22 23:30 UTC.
        var now = new DateTimeOffset(2026, 10, 23, 1, 30, 0, TimeSpan.FromHours(2));
        _activityRepository.ActiveMembers = 37;

        var result = await CreateSut(now).Handle(new GetCommunityPresenceQuery());

        Assert.Equal(
            (new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc)),
            Assert.Single(_activityRepository.ActiveMembersWindows));
        Assert.Equal(37, result.SprintedToday);
    }
}
