using KairuFocus.Application.Pomodoro.Common;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class SessionStartIdempotencyTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0)]
    [InlineData(2_000)]
    [InlineData(5_000)]
    public void Should_BeResubmission_When_StartedWithinFiveSeconds(int ageMilliseconds)
    {
        Assert.True(SessionStartIdempotency.IsResubmission(Now.AddMilliseconds(-ageMilliseconds), Now));
    }

    [Fact]
    public void Should_NotBeResubmission_When_StartedMoreThanFiveSecondsAgo()
    {
        Assert.False(SessionStartIdempotency.IsResubmission(Now.AddMilliseconds(-5_001), Now));
    }

    // With clock drift between instances, a start in the future must not look like a re-submission forever.
    [Fact]
    public void Should_NotBeResubmission_When_StartedInTheFuture()
    {
        Assert.False(SessionStartIdempotency.IsResubmission(Now.AddMilliseconds(1), Now));
    }
}
