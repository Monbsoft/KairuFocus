using Kairu.Application.Pomodoro.Queries.GetSuggestedSessionType;
using Kairu.Application.Tests.Common;
using Kairu.Domain.Identity;
using Kairu.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kairu.Application.Tests.Pomodoro;

public sealed class GetSuggestedSessionTypeQueryHandlerTests
{
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly GetSuggestedSessionTypeQueryHandler _sut;

    public GetSuggestedSessionTypeQueryHandlerTests()
    {
        _sut = new GetSuggestedSessionTypeQueryHandler(
            _sessionRepository,
            _settingsRepository,
            new FakeCurrentUserService(),
            NullLogger<GetSuggestedSessionTypeQueryHandler>.Instance);
    }

    private void AddCompleted(PomodoroSessionType type, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            var s = PomodoroSession.Create(type, 25, FakeCurrentUserService.TestUserId);
            s.Start(DateTime.UtcNow);
            s.Complete(DateTime.UtcNow);
            _sessionRepository.Sessions.Add(s);
        }
    }

    [Fact]
    public async Task Should_SuggestSprint_When_NoSessionsToday()
    {
        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestShortBreak_When_LastWasFirstSprint()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.ShortBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestLongBreak_When_FourSprintsWithoutBreak()
    {
        AddCompleted(PomodoroSessionType.Sprint, 4);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.LongBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestSprint_When_LastWasABreak()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.Sprint, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestShortBreak_When_ThreeSprintsOneBreak()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.ShortBreak, result.SuggestedType);
    }

    [Fact]
    public async Task Should_SuggestLongBreak_When_FourSprintsThreeBreaks()
    {
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);
        AddCompleted(PomodoroSessionType.ShortBreak, 1);
        AddCompleted(PomodoroSessionType.Sprint, 1);

        var result = await _sut.Handle(new GetSuggestedSessionTypeQuery());

        Assert.Equal(PomodoroSessionType.LongBreak, result.SuggestedType);
    }
}
