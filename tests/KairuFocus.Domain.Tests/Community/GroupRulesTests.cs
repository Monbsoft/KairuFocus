using KairuFocus.Domain.Community;

namespace KairuFocus.Domain.Tests.Community;

public sealed class GroupRulesTests
{
    private static readonly DateTime StepEnd = new(2026, 11, 8, 10, 15, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_BeRunning_When_BeforeStepEnd()
    {
        var phase = GroupRules.PhaseAt(StepEnd, endedAtUtc: null, StepEnd.AddSeconds(-1));

        Assert.Equal(GroupPhase.Running, phase);
    }

    [Fact]
    public void Should_WaitForHost_When_StepJustEnded()
    {
        var phase = GroupRules.PhaseAt(StepEnd, endedAtUtc: null, StepEnd);

        Assert.Equal(GroupPhase.WaitingForHost, phase);
    }

    [Fact]
    public void Should_WaitForHost_When_Less15MinutesAfterStepEnd()
    {
        var phase = GroupRules.PhaseAt(StepEnd, endedAtUtc: null, new DateTime(2026, 11, 8, 10, 29, 59, DateTimeKind.Utc));

        Assert.Equal(GroupPhase.WaitingForHost, phase);
    }

    [Fact]
    public void Should_End_When_15MinutesAfterStepEnd()
    {
        var phase = GroupRules.PhaseAt(StepEnd, endedAtUtc: null, new DateTime(2026, 11, 8, 10, 30, 0, DateTimeKind.Utc));

        Assert.Equal(GroupPhase.Ended, phase);
    }

    [Fact]
    public void Should_End_When_EndedAtSet()
    {
        var phase = GroupRules.PhaseAt(StepEnd, endedAtUtc: StepEnd.AddMinutes(-5), StepEnd.AddMinutes(-10));

        Assert.Equal(GroupPhase.Ended, phase);
    }

    [Fact]
    public void Should_ReturnStepEndPlus15Minutes_When_HostAbsentAt()
    {
        Assert.Equal(new DateTime(2026, 11, 8, 10, 30, 0, DateTimeKind.Utc), GroupRules.HostAbsentAt(StepEnd));
        Assert.Equal(TimeSpan.FromMinutes(15), GroupRules.HostWaitLimit);
    }
}
