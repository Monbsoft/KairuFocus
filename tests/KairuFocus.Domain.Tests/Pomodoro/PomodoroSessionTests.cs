using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Domain.Tasks;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Domain.Tests.Pomodoro;

public sealed class PomodoroSessionTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly UserId OwnerId = UserId.New();

    private static PomodoroSession CreatePlanned(int minutes = 25) =>
        PomodoroSession.Create(PomodoroSessionType.Sprint, minutes, OwnerId);

    private static PomodoroSession CreateActive(int minutes = 25)
    {
        var s = PomodoroSession.Create(PomodoroSessionType.Sprint, minutes, OwnerId);
        s.Start(Now);
        return s;
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public void Should_BeInPlannedStatus_When_Created()
    {
        var session = CreatePlanned();

        Assert.Equal(PomodoroSessionStatus.Planned, session.Status);
        Assert.Null(session.StartedAt);
        Assert.Null(session.EndedAt);
        Assert.Empty(session.LinkedTaskIds);
    }

    [Fact]
    public void Should_HaveUniqueId_When_TwoSessionsCreated()
    {
        var s1 = CreatePlanned();
        var s2 = CreatePlanned();

        Assert.NotEqual(s1.Id, s2.Id);
    }

    // ── Start ──────────────────────────────────────────────────────────────

    [Fact]
    public void Should_BeInActiveStatus_When_Started()
    {
        var session = CreatePlanned();

        var result = session.Start(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Active, session.Status);
        Assert.Equal(Now, session.StartedAt);
    }

    [Fact]
    public void Should_ReturnFailure_When_StartingAlreadyActiveSession()
    {
        var session = CreateActive();

        var result = session.Start(Now.AddMinutes(5));

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.InvalidTransition, result.Error);
    }

    [Fact]
    public void Should_ReturnFailure_When_StartingCompletedSession()
    {
        var session = CreateActive();
        session.Complete(Now.AddMinutes(25));

        var result = session.Start(Now.AddMinutes(30));

        Assert.True(result.IsFailure);
    }

    // ── Complete ───────────────────────────────────────────────────────────

    [Fact]
    public void Should_BeInCompletedStatus_When_Completed()
    {
        var session = CreateActive();
        var endTime = Now.AddMinutes(25);

        var result = session.Complete(endTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Completed, session.Status);
        Assert.Equal(endTime, session.EndedAt);
    }

    [Fact]
    public void Should_ReturnFailure_When_CompletingNonActiveSession()
    {
        var session = CreatePlanned();

        var result = session.Complete(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
    }

    // ── Interrupt ──────────────────────────────────────────────────────────

    [Fact]
    public void Should_BeInInterruptedStatus_When_Interrupted()
    {
        var session = CreateActive();
        var endTime = Now.AddMinutes(10);

        var result = session.Interrupt(endTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Interrupted, session.Status);
        Assert.Equal(endTime, session.EndedAt);
    }

    [Fact]
    public void Should_ReturnFailure_When_InterruptingNonActiveSession()
    {
        var session = CreatePlanned();

        var result = session.Interrupt(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
    }

    // ── Bounded end time ───────────────────────────────────────────────────

    [Fact]
    public void Should_SetEndedAtToRequestTime_When_TimedSprintCompletedBeforeScheduledEnd()
    {
        var session = CreateActive(25);

        var result = session.Complete(Now.AddMinutes(22));

        Assert.True(result.IsSuccess);
        Assert.Equal(Now.AddMinutes(22), session.EndedAt);
    }

    [Fact]
    public void Should_SetEndedAtToScheduledEnd_When_TimedSprintCompletedExactlyAtScheduledEnd()
    {
        var session = CreateActive(25);

        session.Complete(Now.AddMinutes(25));

        Assert.Equal(Now.AddMinutes(25), session.EndedAt);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(10800)]
    public void Should_SetEndedAtToScheduledEnd_When_TimedSprintCompletedAfterScheduledEnd(int secondsLate)
    {
        var session = CreateActive(25);

        var result = session.Complete(Now.AddMinutes(25).AddSeconds(secondsLate));

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Completed, session.Status);
        Assert.Equal(Now.AddMinutes(25), session.EndedAt);
    }

    [Fact]
    public void Should_SetEndedAtToScheduledEndOnNextDay_When_SprintSpansMidnightAndIsCompletedLate()
    {
        var start = new DateTime(2026, 1, 1, 23, 50, 0, DateTimeKind.Utc);
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, OwnerId);
        session.Start(start);

        session.Complete(new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 1, 2, 0, 15, 0, DateTimeKind.Utc), session.EndedAt);
    }

    [Theory]
    [InlineData(PomodoroSessionType.ShortBreak, 5)]
    [InlineData(PomodoroSessionType.LongBreak, 15)]
    public void Should_SetEndedAtToScheduledEnd_When_BreakCompletedAfterScheduledEnd(
        PomodoroSessionType type, int minutes)
    {
        var session = PomodoroSession.Create(type, minutes, OwnerId);
        session.Start(Now);

        session.Complete(Now.AddMinutes(minutes + 30));

        Assert.Equal(Now.AddMinutes(minutes), session.EndedAt);
    }

    [Fact]
    public void Should_SetEndedAtToRequestTime_When_FreeSprintCompleted()
    {
        var session = CreateActive(0);
        var requestTime = Now.AddHours(5);

        session.Complete(requestTime);

        Assert.Equal(requestTime, session.EndedAt);
    }

    [Fact]
    public void Should_ReturnFailureAndKeepEndedAt_When_CompletingAlreadyCompletedSession()
    {
        var session = CreateActive(25);
        session.Complete(Now.AddMinutes(25));

        var result = session.Complete(Now.AddMinutes(10));

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
        Assert.Equal(Now.AddMinutes(25), session.EndedAt);
    }

    [Fact]
    public void Should_ReturnFailureAndKeepEndedAt_When_CompletingInterruptedSession()
    {
        var session = CreateActive(25);
        session.Interrupt(Now.AddMinutes(10));

        var result = session.Complete(Now.AddMinutes(20));

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroSessionStatus.Interrupted, session.Status);
        Assert.Equal(Now.AddMinutes(10), session.EndedAt);
    }

    [Fact]
    public void Should_SetEndedAtToRequestTime_When_TimedSprintInterruptedBeforeScheduledEnd()
    {
        var session = CreateActive(25);

        var result = session.Interrupt(Now.AddMinutes(10));

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Interrupted, session.Status);
        Assert.Equal(Now.AddMinutes(10), session.EndedAt);
    }

    [Fact]
    public void Should_StayInterruptedWithEndedAtAtScheduledEnd_When_InterruptedAfterScheduledEnd()
    {
        var session = CreateActive(25);

        var result = session.Interrupt(Now.AddMinutes(25).AddHours(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Interrupted, session.Status);
        Assert.Equal(Now.AddMinutes(25), session.EndedAt);
    }

    [Fact]
    public void Should_SetEndedAtToRequestTime_When_FreeSprintInterrupted()
    {
        var session = CreateActive(0);
        var requestTime = Now.AddHours(5);

        session.Interrupt(requestTime);

        Assert.Equal(requestTime, session.EndedAt);
    }

    [Fact]
    public void Should_ReturnFailureAndKeepEndedAt_When_InterruptingAlreadyEndedSession()
    {
        var session = CreateActive(25);
        session.Interrupt(Now.AddMinutes(10));

        var result = session.Interrupt(Now.AddMinutes(20));

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
        Assert.Equal(Now.AddMinutes(10), session.EndedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_SetEndedAtToStartedAt_When_RequestTimeIsBeforeStart(bool complete)
    {
        var session = CreateActive(25);
        var earlier = Now.AddMinutes(-3);

        if (complete)
            session.Complete(earlier);
        else
            session.Interrupt(earlier);

        Assert.Equal(Now, session.EndedAt);
    }

    // ── LinkTask ───────────────────────────────────────────────────────────

    [Fact]
    public void Should_ContainLinkedTask_When_TaskLinked()
    {
        var session = CreateActive();
        var taskId = TaskId.New();

        var result = session.LinkTask(taskId);

        Assert.True(result.IsSuccess);
        Assert.Contains(taskId, session.LinkedTaskIds);
    }

    [Fact]
    public void Should_ReturnFailure_When_LinkingAlreadyLinkedTask()
    {
        var session = CreateActive();
        var taskId = TaskId.New();
        session.LinkTask(taskId);

        var result = session.LinkTask(taskId);

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.TaskAlreadyLinked, result.Error);
    }

    [Fact]
    public void Should_LinkMultipleTasks()
    {
        var session = CreateActive();
        var id1 = TaskId.New();
        var id2 = TaskId.New();

        session.LinkTask(id1);
        session.LinkTask(id2);

        Assert.Equal(2, session.LinkedTaskIds.Count);
    }

    [Fact]
    public void Should_ReturnFailure_When_LinkingTaskOnPlannedSession()
    {
        var session = CreatePlanned();

        var result = session.LinkTask(TaskId.New());

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
    }

    [Fact]
    public void Should_ReturnFailure_When_LinkingTaskOnShortBreakSession()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.ShortBreak, 5, OwnerId);
        session.Start(Now);

        var result = session.LinkTask(TaskId.New());

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.TaskLinkingNotAllowedForBreak, result.Error);
    }

    [Fact]
    public void Should_ReturnFailure_When_LinkingTaskOnLongBreakSession()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.LongBreak, 15, OwnerId);
        session.Start(Now);

        var result = session.LinkTask(TaskId.New());

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.TaskLinkingNotAllowedForBreak, result.Error);
    }

    // ── UnlinkTask ─────────────────────────────────────────────────────────

    [Fact]
    public void Should_RemoveTask_When_UnlinkingLinkedTask()
    {
        var session = CreateActive();
        var taskId = TaskId.New();
        session.LinkTask(taskId);

        var result = session.UnlinkTask(taskId);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(taskId, session.LinkedTaskIds);
    }

    [Fact]
    public void Should_ReturnFailure_When_UnlinkingUnknownTask()
    {
        var session = CreateActive();

        var result = session.UnlinkTask(TaskId.New());

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.TaskNotLinked, result.Error);
    }

    [Fact]
    public void Should_ReturnFailure_When_UnlinkingOnNonActiveSession()
    {
        var session = CreatePlanned();

        var result = session.UnlinkTask(TaskId.New());

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
    }

    [Fact]
    public void Should_ReturnFailure_When_UnlinkingOnCompletedSession()
    {
        var session = CreateActive();
        var taskId = TaskId.New();
        session.LinkTask(taskId);
        session.Complete(Now.AddMinutes(25));

        var result = session.UnlinkTask(taskId);

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
    }

    [Fact]
    public void Should_NotBoundEndedAt_When_PlannedDurationIsNegative()
    {
        var session = CreateActive(-5);
        var now = Now.AddMinutes(40);

        session.Complete(now);

        Assert.Equal(now, session.EndedAt);
    }
}
