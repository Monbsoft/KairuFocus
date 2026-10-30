using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class PomodoroSessionViewModelTests
{
    private static readonly DateTime NowUtc = new(2026, 10, 30, 14, 0, 0, DateTimeKind.Utc);

    private static PomodoroSession Started(PomodoroSessionType type, int plannedMinutes)
    {
        var session = PomodoroSession.Create(type, plannedMinutes, FakeCurrentUserService.TestUserId);
        Assert.True(session.Start(NowUtc).IsSuccess);
        return session;
    }

    [Fact]
    public void Should_ExposeSharedEndAndHostFlags_When_SessionIsRoot()
    {
        var session = Started(PomodoroSessionType.Sprint, 25);
        Assert.True(session.CloseToJoin().IsSuccess);

        var viewModel = PomodoroSessionViewModel.From(session);

        Assert.Null(viewModel.SharedEndAt);
        Assert.True(viewModel.IsGroupHost);
        Assert.True(viewModel.IsClosedToJoin);
    }

    [Fact]
    public void Should_ExposeSharedEndAndHostFlags_When_SessionWasJoined()
    {
        var sharedEndAt = NowUtc.AddMinutes(18).AddSeconds(24);
        var session = PomodoroSession.CreateJoined(
            FakeCurrentUserService.TestUserId,
            PomodoroSessionId.New(),
            sharedEndAt,
            NowUtc).Value;
        Assert.True(session.Start(NowUtc).IsSuccess);

        var viewModel = PomodoroSessionViewModel.From(session);

        Assert.Equal(sharedEndAt, viewModel.SharedEndAt);
        Assert.False(viewModel.IsGroupHost);
        Assert.False(viewModel.IsClosedToJoin);
    }

    [Theory]
    [InlineData(PomodoroSessionType.Sprint, 0)]
    [InlineData(PomodoroSessionType.ShortBreak, 5)]
    [InlineData(PomodoroSessionType.LongBreak, 15)]
    public void Should_NotBeHost_When_SessionIsFreeSprintOrBreak(PomodoroSessionType type, int plannedMinutes)
    {
        var viewModel = PomodoroSessionViewModel.From(Started(type, plannedMinutes));

        Assert.False(viewModel.IsGroupHost);
        Assert.False(viewModel.IsClosedToJoin);
    }
}
