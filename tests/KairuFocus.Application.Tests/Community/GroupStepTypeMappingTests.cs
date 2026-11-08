using KairuFocus.Application.Community.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Community;

public sealed class GroupStepTypeMappingTests
{
    [Theory]
    [InlineData(GroupStepType.Sprint, PomodoroSessionType.Sprint)]
    [InlineData(GroupStepType.ShortBreak, PomodoroSessionType.ShortBreak)]
    [InlineData(GroupStepType.LongBreak, PomodoroSessionType.LongBreak)]
    public void Should_MapBothWays_When_TypeIsKnown(GroupStepType stepType, PomodoroSessionType sessionType)
    {
        Assert.Equal(sessionType, stepType.ToSessionType());
        Assert.Equal(stepType, sessionType.ToStepType());
    }

    [Fact]
    public void Should_Throw_When_StepTypeIsUnknown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((GroupStepType)42).ToSessionType());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PomodoroSessionType)42).ToStepType());
    }
}
