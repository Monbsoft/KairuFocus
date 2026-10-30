using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Domain.Tests.Pomodoro;

public sealed class PomodoroSessionSharedSprintTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);
    private static readonly UserId OwnerId = UserId.New();
    private static readonly PomodoroSessionId RootId = PomodoroSessionId.New();

    /// <summary>End of the group, 18:24 after <see cref="Now"/>.</summary>
    private static readonly DateTime SharedEnd = Now.AddMinutes(18).AddSeconds(24);

    private static PomodoroSession CreateJoinedAndStarted()
    {
        var session = PomodoroSession.CreateJoined(OwnerId, RootId, SharedEnd, Now).Value;
        session.Start(Now);
        return session;
    }

    private static PomodoroSession CreateActiveRoot(int minutes = 25)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, minutes, OwnerId);
        session.Start(Now);
        return session;
    }

    // ── CreateJoined ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(18, 24, 19)]
    [InlineData(5, 0, 5)]
    [InlineData(0, 1, 1)]
    public void Should_RoundPlannedMinutesUp_When_Joined(int remainingMinutes, int remainingSeconds, int expected)
    {
        var sharedEnd = Now.AddMinutes(remainingMinutes).AddSeconds(remainingSeconds);

        var result = PomodoroSession.CreateJoined(OwnerId, RootId, sharedEnd, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.PlannedDurationMinutes);
    }

    [Fact]
    public void Should_SetRootAndEnd_When_Joined()
    {
        var result = PomodoroSession.CreateJoined(OwnerId, RootId, SharedEnd, Now);

        Assert.True(result.IsSuccess);
        var session = result.Value;
        Assert.Equal(RootId, session.SharedSprintId);
        Assert.Equal(SharedEnd, session.SharedEndAt);
        Assert.Equal(OwnerId, session.OwnerId);
        Assert.NotEqual(RootId, session.Id);
    }

    [Fact]
    public void Should_BePlannedSprintNotStarted_When_Joined()
    {
        var session = PomodoroSession.CreateJoined(OwnerId, RootId, SharedEnd, Now).Value;

        Assert.Equal(PomodoroSessionType.Sprint, session.SessionType);
        Assert.Equal(PomodoroSessionStatus.Planned, session.Status);
        Assert.Null(session.StartedAt);
        Assert.False(session.IsClosedToJoin);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Should_Fail_When_JoinedAtOrAfterSharedEnd(int secondsAfterEnd)
    {
        var result = PomodoroSession.CreateJoined(OwnerId, RootId, SharedEnd, SharedEnd.AddSeconds(secondsAfterEnd));

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SharedSprintEnded, result.Error);
    }

    [Fact]
    public void Should_LeaveSharedFieldsEmpty_When_CreatedAsHost()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, OwnerId);

        Assert.Null(session.SharedSprintId);
        Assert.Null(session.SharedEndAt);
        Assert.False(session.IsClosedToJoin);
    }

    // ── Start ──────────────────────────────────────────────────────────────

    [Fact]
    public void Should_Start_When_JoinedBeforeSharedEnd()
    {
        var session = PomodoroSession.CreateJoined(OwnerId, RootId, SharedEnd, Now).Value;

        var result = session.Start(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Active, session.Status);
    }

    [Fact]
    public void Should_FailToStart_When_SharedEndHasPassed()
    {
        var session = PomodoroSession.CreateJoined(OwnerId, RootId, SharedEnd, Now).Value;

        var result = session.Start(SharedEnd);

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SharedSprintEnded, result.Error);
        Assert.Equal(PomodoroSessionStatus.Planned, session.Status);
        Assert.Null(session.StartedAt);
    }

    // ── Bounded end time ───────────────────────────────────────────────────

    [Fact]
    public void Should_BoundEndedAtOnSharedEnd_When_CompletedLate()
    {
        var session = CreateJoinedAndStarted();

        var result = session.Complete(SharedEnd.AddSeconds(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(SharedEnd, session.EndedAt);
    }

    [Fact]
    public void Should_BoundEndedAtOnSharedEnd_When_InterruptedLate()
    {
        var session = CreateJoinedAndStarted();

        var result = session.Interrupt(SharedEnd.AddSeconds(30));

        Assert.True(result.IsSuccess);
        Assert.Equal(SharedEnd, session.EndedAt);
    }

    [Fact]
    public void Should_KeepEndedAtNow_When_CompletedBeforeSharedEnd()
    {
        var session = CreateJoinedAndStarted();
        var beforeEnd = SharedEnd.AddSeconds(-5);

        session.Complete(beforeEnd);

        Assert.Equal(beforeEnd, session.EndedAt);
    }

    [Fact]
    public void Should_PreferSharedEndOverRoundedDuration_When_CompletedWithinRoundedMinute()
    {
        // The planned duration is rounded up to 19 minutes, the group still ends at 18:24.
        var session = CreateJoinedAndStarted();

        session.Complete(Now.AddMinutes(19));

        Assert.Equal(SharedEnd, session.EndedAt);
    }

    // ── CloseToJoin / ReopenToJoin ─────────────────────────────────────────

    [Fact]
    public void Should_CloseAndReopen_When_RootIsActive()
    {
        var session = CreateActiveRoot();

        var closed = session.CloseToJoin();

        Assert.True(closed.IsSuccess);
        Assert.True(session.IsClosedToJoin);

        var reopened = session.ReopenToJoin();

        Assert.True(reopened.IsSuccess);
        Assert.False(session.IsClosedToJoin);
    }

    [Fact]
    public void Should_StayClosed_When_ClosedTwice()
    {
        var session = CreateActiveRoot();
        session.CloseToJoin();

        var result = session.CloseToJoin();

        Assert.True(result.IsSuccess);
        Assert.True(session.IsClosedToJoin);
    }

    [Fact]
    public void Should_Fail_When_ClosingJoinedSession()
    {
        var session = CreateJoinedAndStarted();

        var result = session.CloseToJoin();

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.NotSharedSprintRoot, result.Error);
        Assert.False(session.IsClosedToJoin);
    }

    [Fact]
    public void Should_Fail_When_ClosingFreeSprint()
    {
        var session = CreateActiveRoot(0);

        var result = session.CloseToJoin();

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.NotSharedSprintRoot, result.Error);
        Assert.False(session.IsClosedToJoin);
    }

    [Theory]
    [InlineData(PomodoroSessionType.ShortBreak, 5)]
    [InlineData(PomodoroSessionType.LongBreak, 15)]
    public void Should_Fail_When_ClosingBreak(PomodoroSessionType type, int minutes)
    {
        var session = PomodoroSession.Create(type, minutes, OwnerId);
        session.Start(Now);

        var result = session.CloseToJoin();

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.NotSharedSprintRoot, result.Error);
    }

    [Fact]
    public void Should_Fail_When_ClosingSessionNotActive()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, OwnerId);

        var result = session.CloseToJoin();

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
    }

    [Fact]
    public void Should_Fail_When_ReopeningJoinedSession()
    {
        var session = CreateJoinedAndStarted();

        var result = session.ReopenToJoin();

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.NotSharedSprintRoot, result.Error);
    }

    [Fact]
    public void Should_Fail_When_ReopeningEndedSession()
    {
        var session = CreateActiveRoot();
        session.CloseToJoin();
        session.Complete(Now.AddMinutes(25));

        var result = session.ReopenToJoin();

        Assert.True(result.IsFailure);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionNotActive, result.Error);
        Assert.True(session.IsClosedToJoin);
    }
}
