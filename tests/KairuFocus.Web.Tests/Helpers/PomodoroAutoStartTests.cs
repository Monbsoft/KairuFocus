using KairuFocus.Web.Helpers;

namespace KairuFocus.Web.Tests.Helpers;

public sealed class PomodoroAutoStartTests
{
    [Fact]
    public void Should_Start_When_SprintRequestedAndIdle()
    {
        Assert.True(PomodoroAutoStart.ShouldStart("sprint", false, false));
        Assert.True(PomodoroAutoStart.ShouldStart("SPRINT", false, false));
    }

    [Fact]
    public void Should_NotStart_When_SessionOrGroupOrOtherValue()
    {
        Assert.False(PomodoroAutoStart.ShouldStart("sprint", true, false));
        Assert.False(PomodoroAutoStart.ShouldStart("sprint", false, true));
        Assert.False(PomodoroAutoStart.ShouldStart("pause", false, false));
        Assert.False(PomodoroAutoStart.ShouldStart(null, false, false));
    }
}
