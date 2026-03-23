using Kairudev.Application.Pomodoro.Commands.StartSession;
using Kairudev.Application.Tests.Common;
using Kairudev.Application.Tests.Journal;
using Kairudev.Domain.Identity;
using Kairudev.Domain.Journal;
using Kairudev.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kairudev.Application.Tests.Pomodoro;

public sealed class StartSessionCommandHandlerTests
{
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly FakeJournalEntryRepository _journalRepository = new();
    private readonly StartSessionCommandHandler _sut;

    public StartSessionCommandHandlerTests()
    {
        var fakeMediator = new FakeMediator(_journalRepository);
        _sut = new StartSessionCommandHandler(
            _sessionRepository,
            _settingsRepository,
            fakeMediator,
            new FakeCurrentUserService(),
            NullLogger<StartSessionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_ASessionIsAlreadyActive()
    {
        var existing = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, FakeCurrentUserService.TestUserId);
        existing.Start(DateTime.UtcNow);
        _sessionRepository.Sessions.Add(existing);

        var result = await _sut.Handle(new StartSessionCommand(null));

        Assert.False(result.IsSuccess);
        Assert.Equal("A session is already active", result.Error);
    }

    [Fact]
    public async Task Should_LogSprintStarted_When_SprintSessionStarted()
    {
        var result = await _sut.Handle(new StartSessionCommand("Sprint"));

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.SprintStarted, _journalRepository.Entries[0].EventType);
    }

    [Fact]
    public async Task Should_LogBreakStarted_When_ShortBreakSessionStarted()
    {
        var result = await _sut.Handle(new StartSessionCommand("ShortBreak"));

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.BreakStarted, _journalRepository.Entries[0].EventType);
    }

    [Fact]
    public async Task Should_LogBreakStarted_When_LongBreakSessionStarted()
    {
        var result = await _sut.Handle(new StartSessionCommand("LongBreak"));

        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.BreakStarted, _journalRepository.Entries[0].EventType);
    }
}
