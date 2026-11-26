using KairuFocus.Domain.Community;

namespace KairuFocus.Domain.Tests.Community;

public sealed class PresenceRulesTests
{
    [Fact]
    public void Should_BePresent_When_SeenExactlyThreeMinutesAgo()
    {
        var now = new DateTime(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc);

        Assert.Equal(now.AddMinutes(-3), PresenceRules.SeenSince(now));
    }
}
