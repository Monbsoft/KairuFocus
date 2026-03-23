using Kairudev.Application.Sprint.Commands.RecordSprint;
using Kairudev.Application.Tests.Common;
using Kairudev.Application.Tests.Journal;
using Kairudev.Domain.Journal;
using Kairudev.Domain.Sprint;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kairudev.Application.Tests.Sprint;

public sealed class RecordSprintCommandHandlerTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 3, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndedAt = new(2026, 3, 18, 9, 30, 0, TimeSpan.Zero);

    private static (RecordSprintCommandHandler Handler, FakeSprintSessionRepository SprintRepo, FakeJournalEntryRepository JournalRepo)
        BuildHandler()
    {
        var sprintRepo = new FakeSprintSessionRepository();
        var journalRepo = new FakeJournalEntryRepository();
        var fakeMediator = new FakeMediator(journalRepo);
        var currentUser = new FakeCurrentUserService();
        var handler = new RecordSprintCommandHandler(sprintRepo, fakeMediator, currentUser, NullLogger<RecordSprintCommandHandler>.Instance);
        return (handler, sprintRepo, journalRepo);
    }

    [Fact]
    public async Task Should_PersistSession_When_CommandIsValid()
    {
        var (handler, sprintRepo, _) = BuildHandler();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Completed", null, 1);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Single(sprintRepo.Sessions);
        Assert.Equal("Focus", sprintRepo.Sessions[0].Name.Value);
    }

    [Fact]
    public async Task Should_UseDefaultName_When_NameIsEmpty()
    {
        var (handler, sprintRepo, _) = BuildHandler();
        var command = new RecordSprintCommand("", StartedAt, EndedAt, "Completed", null, 3);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sprint #3", sprintRepo.Sessions[0].Name.Value);
        Assert.Equal("Sprint #3", result.Session!.Name);
    }

    [Fact]
    public async Task Should_ReturnViewModel_When_SessionIsRecorded()
    {
        var (handler, _, _) = BuildHandler();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Completed", null, 1);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        var session = result.Session!;
        Assert.Equal("Focus", session.Name);
        Assert.Equal(StartedAt, session.StartedAt);
        Assert.Equal(EndedAt, session.EndedAt);
        Assert.Equal("Completed", session.Outcome);
        Assert.Equal(30 * 60, session.DurationSeconds);
        Assert.Null(session.LinkedTaskId);
    }

    [Fact]
    public async Task Should_LinkTask_When_LinkedTaskIdIsProvided()
    {
        var (handler, sprintRepo, _) = BuildHandler();
        var taskId = Guid.NewGuid();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Completed", taskId, 1);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(taskId, sprintRepo.Sessions[0].LinkedTaskId!.Value);
        Assert.Equal(taskId, result.Session!.LinkedTaskId);
    }

    [Fact]
    public async Task Should_RecordInterruptedOutcome_When_OutcomeIsInterrupted()
    {
        var (handler, sprintRepo, _) = BuildHandler();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Interrupted", null, 1);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(SprintOutcome.Interrupted, sprintRepo.Sessions[0].Outcome);
        Assert.Equal("Interrupted", result.Session!.Outcome);
    }

    [Fact]
    public async Task Should_CreateTwoJournalEntries_When_SessionIsCompleted()
    {
        var (handler, _, journalRepo) = BuildHandler();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Completed", null, 1);

        await handler.Handle(command);

        Assert.Equal(2, journalRepo.Entries.Count);
        Assert.Contains(journalRepo.Entries, e => e.EventType == JournalEventType.SprintStarted);
        Assert.Contains(journalRepo.Entries, e => e.EventType == JournalEventType.SprintCompleted);
    }

    [Fact]
    public async Task Should_CreateTwoJournalEntries_When_SessionIsInterrupted()
    {
        var (handler, _, journalRepo) = BuildHandler();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Interrupted", null, 1);

        await handler.Handle(command);

        Assert.Equal(2, journalRepo.Entries.Count);
        Assert.Contains(journalRepo.Entries, e => e.EventType == JournalEventType.SprintStarted);
        Assert.Contains(journalRepo.Entries, e => e.EventType == JournalEventType.SprintInterrupted);
    }

    [Fact]
    public async Task Should_CreateJournalEntriesWithRetroactiveTimestamps_When_SessionIsRecorded()
    {
        var (handler, _, journalRepo) = BuildHandler();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Completed", null, 1);

        await handler.Handle(command);

        var startEntry = journalRepo.Entries.Single(e => e.EventType == JournalEventType.SprintStarted);
        var endEntry   = journalRepo.Entries.Single(e => e.EventType == JournalEventType.SprintCompleted);

        Assert.Equal(StartedAt.UtcDateTime, startEntry.OccurredAt);
        Assert.Equal(EndedAt.UtcDateTime, endEntry.OccurredAt);
    }

    [Fact]
    public async Task Should_Fail_When_EndedAtIsBeforeStartedAt()
    {
        var (handler, _, _) = BuildHandler();
        var command = new RecordSprintCommand("Focus", EndedAt, StartedAt, "Completed", null, 1);

        var result = await handler.Handle(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(SprintDomainErrors.Sprint.EndedAtBeforeStartedAt, result.Error);
    }

    [Fact]
    public async Task Should_Fail_When_OutcomeIsInvalid()
    {
        var (handler, _, _) = BuildHandler();
        var command = new RecordSprintCommand("Focus", StartedAt, EndedAt, "Unknown", null, 1);

        var result = await handler.Handle(command);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Should_Fail_When_NameExceedsMaxLength()
    {
        var (handler, _, _) = BuildHandler();
        var longName = new string('x', 201);
        var command = new RecordSprintCommand(longName, StartedAt, EndedAt, "Completed", null, 1);

        var result = await handler.Handle(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(SprintDomainErrors.Sprint.NameTooLong, result.Error);
    }
}
