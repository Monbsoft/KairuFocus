using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Domain.Tasks;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Domain.Tests.Pomodoro;

/// <summary>The short break that follows a shared sprint (UC-CO-10).</summary>
public sealed class PomodoroSessionJoinedBreakTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);
    private static readonly UserId OwnerId = UserId.New();
    private static readonly PomodoroSessionId RootId = PomodoroSessionId.New();

    /// <summary>End of the break, 4:30 after <see cref="Now"/>.</summary>
    private static readonly DateTime SharedEnd = Now.AddMinutes(4).AddSeconds(30);

    private static PomodoroSession CreateStartedBreak()
    {
        var session = PomodoroSession.CreateJoinedBreak(OwnerId, RootId, SharedEnd, Now).Value;
        session.Start(Now);
        return session;
    }

    [Fact]
    public void Should_BePlannedShortBreakOfTheGroup_When_Created()
    {
        var result = PomodoroSession.CreateJoinedBreak(OwnerId, RootId, SharedEnd, Now);

        Assert.True(result.IsSuccess);
        var session = result.Value;
        Assert.Equal(PomodoroSessionType.ShortBreak, session.SessionType);
        Assert.Equal(PomodoroSessionStatus.Planned, session.Status);
        Assert.Equal(OwnerId, session.OwnerId);
        Assert.Equal(RootId, session.SharedSprintId);
        Assert.Equal(SharedEnd, session.SharedEndAt);
        Assert.Equal(SharedEnd, session.ScheduledEndAt);
        Assert.Null(session.StartedAt);
        Assert.False(session.IsClosedToJoin);
    }

    [Theory]
    [InlineData(4, 30, 5)]
    [InlineData(5, 0, 5)]
    [InlineData(0, 1, 1)]
    public void Should_RoundPlannedMinutesUp_When_Created(int remainingMinutes, int remainingSeconds, int expected)
    {
        var sharedEnd = Now.AddMinutes(remainingMinutes).AddSeconds(remainingSeconds);

        var result = PomodoroSession.CreateJoinedBreak(OwnerId, RootId, sharedEnd, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.PlannedDurationMinutes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Should_Fail_When_CreatedAtOrAfterSharedEnd(int secondsAfterEnd)
    {
        var result = PomodoroSession.CreateJoinedBreak(OwnerId, RootId, SharedEnd, SharedEnd.AddSeconds(secondsAfterEnd));

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SharedSprintEnded, result.Error);
    }

    [Fact]
    public void Should_BoundEndedAtOnSharedEnd_When_CompletedLate()
    {
        var session = CreateStartedBreak();

        var result = session.Complete(SharedEnd.AddSeconds(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(SharedEnd, session.EndedAt);
    }

    [Fact]
    public void Should_BoundEndedAtOnSharedEnd_When_InterruptedLate()
    {
        var session = CreateStartedBreak();

        var result = session.Interrupt(SharedEnd.AddSeconds(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(SharedEnd, session.EndedAt);
    }

    [Fact]
    public void Should_KeepEndedAtNow_When_InterruptedBeforeSharedEnd()
    {
        var session = CreateStartedBreak();
        var beforeEnd = SharedEnd.AddSeconds(-20);

        session.Interrupt(beforeEnd);

        Assert.Equal(PomodoroSessionStatus.Interrupted, session.Status);
        Assert.Equal(beforeEnd, session.EndedAt);
    }

    [Fact]
    public void Should_Fail_When_ClosingJoinedBreak()
    {
        var session = CreateStartedBreak();

        var result = session.CloseToJoin();

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.NotSharedSprintRoot, result.Error);
    }

    [Fact]
    public void Should_Fail_When_LinkingTaskToJoinedBreak()
    {
        var session = CreateStartedBreak();

        var result = session.LinkTask(TaskId.New());

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.TaskLinkingNotAllowedForBreak, result.Error);
    }

    // A break that lasts at least five minutes is not a sprint: it earns nothing.
    [Fact]
    public void Should_EarnNoXp_When_JoinedBreakIsCompleted()
    {
        var sharedEnd = Now.AddMinutes(10);
        var session = PomodoroSession.CreateJoinedBreak(OwnerId, RootId, sharedEnd, Now).Value;
        session.Start(Now);
        session.Complete(sharedEnd);

        var amount = SprintXpPolicy.Evaluate(session.SessionType, session.Status, session.StartedAt, session.EndedAt);

        Assert.Equal(PomodoroSessionStatus.Completed, session.Status);
        Assert.Equal(0, amount);
    }

    [Fact]
    public void Should_ExposeStartPlusDuration_When_ScheduledEndOfHostSprint()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, OwnerId);
        Assert.Null(session.ScheduledEndAt);

        session.Start(Now);

        Assert.Equal(Now.AddMinutes(25), session.ScheduledEndAt);
    }

    [Fact]
    public void Should_ExposeNoScheduledEnd_When_SprintIsFree()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 0, OwnerId);
        session.Start(Now);

        Assert.Null(session.ScheduledEndAt);
    }
}
