using KairuFocus.Web.Helpers;
using KairuFocus.Web.Services;

namespace KairuFocus.Web.Tests.Helpers;

public sealed class SessionClockTests
{
    private static readonly DateTime T0 = new(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);

    private static PomodoroSessionDto Session(int planned, DateTime? startedAt, DateTime? sharedEndAt = null) =>
        new(Guid.NewGuid(), "Sprint", "InProgress", planned, startedAt, null, [], null, sharedEndAt, false);

    [Fact]
    public void Should_UseSharedEnd_When_SprintWasJoined() =>
        Assert.Equal(T0.AddMinutes(18), SessionClock.DeadlineUtc(Session(25, T0, T0.AddMinutes(18))));

    [Fact]
    public void Should_UseStartPlusPlanned_When_NoSharedEnd() =>
        Assert.Equal(T0.AddMinutes(25), SessionClock.DeadlineUtc(Session(25, T0)));

    [Fact]
    public void Should_ReturnNoDeadline_When_FreeSprint() =>
        Assert.Null(SessionClock.DeadlineUtc(Session(0, T0)));

    [Fact]
    public void Should_ReturnNoDeadline_When_NotStarted() =>
        Assert.Null(SessionClock.DeadlineUtc(Session(25, null)));

    [Fact]
    public void Should_TreatUnspecifiedAsUtc_When_DateHasNoKind()
    {
        var unspecified = DateTime.SpecifyKind(T0, DateTimeKind.Unspecified);
        Assert.Equal(DateTimeKind.Utc, SessionClock.DeadlineUtc(Session(25, unspecified))!.Value.Kind);
    }

    [Fact]
    public void Should_ReturnRemainingFromDeadline_When_ClockJumped() =>
        Assert.Equal(0, SessionClock.RemainingSeconds(T0.AddMinutes(25), T0.AddHours(3)));

    [Fact]
    public void Should_RoundUp_When_PartialSecondRemains() =>
        Assert.Equal(60, SessionClock.RemainingSeconds(T0.AddSeconds(60), T0.AddMilliseconds(200)));

    [Fact]
    public void Should_RoundDown_When_PartialSecondElapsed() =>
        Assert.Equal(5, SessionClock.ElapsedSeconds(T0, T0.AddMilliseconds(5900)));

    [Fact]
    public void Should_ClampElapsedToZero_When_NowBeforeStart() =>
        Assert.Equal(0, SessionClock.ElapsedSeconds(T0, T0.AddSeconds(-3)));

    [Fact]
    public void Should_FormatHours_When_OverOneHour() => Assert.Equal("1:02:05", SessionClock.Format(3725));

    [Fact]
    public void Should_FormatMinutes_When_UnderOneHour() => Assert.Equal("04:09", SessionClock.Format(249));
}
