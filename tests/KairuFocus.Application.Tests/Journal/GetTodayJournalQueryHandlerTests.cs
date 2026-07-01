using KairuFocus.Application.Journal.Queries.GetTodayJournal;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Application.Tests.Tasks;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Journal;

public sealed class GetTodayJournalQueryHandlerTests
{
    private static readonly UserId OwnerId = FakeCurrentUserService.TestUserId;

    private readonly FakeJournalEntryRepository _journalRepo = new();
    private readonly FakePomodoroSessionRepository _sessionRepo = new();
    private readonly FakeTaskRepository _taskRepo = new();
    private readonly GetTodayJournalQueryHandler _sut;

    public GetTodayJournalQueryHandlerTests() =>
        _sut = new GetTodayJournalQueryHandler(
            _journalRepo,
            _sessionRepo,
            _taskRepo,
            new FakeCurrentUserService(),
            NullLogger<GetTodayJournalQueryHandler>.Instance);

    [Fact]
    public async Task Should_ReturnTodayEntry_When_EntryOccurredToday()
    {
        // Arrange
        _journalRepo.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow, OwnerId));

        // Act
        var result = await _sut.Handle(new GetTodayJournalQuery());

        // Assert
        Assert.Single(result.Entries);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoEntryToday()
    {
        // Act
        var result = await _sut.Handle(new GetTodayJournalQuery());

        // Assert
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task Should_NotReturnYesterdayEntry_When_FilteringByToday()
    {
        // Arrange
        _journalRepo.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow, OwnerId));
        _journalRepo.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), OwnerId));

        // Act
        var result = await _sut.Handle(new GetTodayJournalQuery());

        // Assert
        Assert.Single(result.Entries);
    }

    [Fact]
    public async Task Should_NotReturnOtherUserEntry_When_FilteringByCurrentUser()
    {
        // Arrange
        var otherUser = UserId.New();
        _journalRepo.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow, OwnerId));
        _journalRepo.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow, otherUser));

        // Act
        var result = await _sut.Handle(new GetTodayJournalQuery());

        // Assert
        Assert.Single(result.Entries);
    }

    [Fact]
    public async Task Should_ReturnMultipleEntries_When_SeveralOccurredToday()
    {
        // Arrange
        _journalRepo.Entries.Add(JournalEntry.Create(JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow, OwnerId));
        _journalRepo.Entries.Add(JournalEntry.Create(JournalEventType.SprintCompleted, Guid.NewGuid(), DateTime.UtcNow, OwnerId));
        _journalRepo.Entries.Add(JournalEntry.Create(JournalEventType.BreakCompleted, Guid.NewGuid(), DateTime.UtcNow, OwnerId));

        // Act
        var result = await _sut.Handle(new GetTodayJournalQuery());

        // Assert
        Assert.Equal(3, result.Entries.Count);
    }

    [Fact]
    public async Task Should_MapEventType_When_EntryExists()
    {
        // Arrange
        _journalRepo.Entries.Add(JournalEntry.Create(
            JournalEventType.SprintCompleted, Guid.NewGuid(), DateTime.UtcNow, OwnerId));

        // Act
        var result = await _sut.Handle(new GetTodayJournalQuery());

        // Assert
        Assert.Equal(nameof(JournalEventType.SprintCompleted), result.Entries[0].EventType);
    }
}
