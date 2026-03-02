using Kairudev.Application.Journal.Commands.CreateEntry;
using Kairudev.Application.Tasks.Commands.CompleteTask;
using Kairudev.Domain.Journal;
using Kairudev.Domain.Tasks;
using DomainTaskStatus = Kairudev.Domain.Tasks.TaskStatus;

namespace Kairudev.Application.Tests.Tasks;

public sealed class CompleteTaskCommandHandlerTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly FakeCreateEntryHandler _journalHandler = new();
    private readonly CompleteTaskCommandHandler _sut;

    public CompleteTaskCommandHandlerTests() =>
        _sut = new CompleteTaskCommandHandler(_repository, _journalHandler);

    [Fact]
    public async Task Should_ReturnSuccess_When_TaskExistsAndIsPending()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new CompleteTaskCommand(task.Id.Value);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DomainTaskStatus.Done, _repository.Tasks[0].Status);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TaskDoesNotExist()
    {
        // Arrange
        var command = new CompleteTaskCommand(Guid.NewGuid());

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_TaskAlreadyCompleted()
    {
        // Arrange
        var task = CreateAndAddTask();
        task.Complete();
        var command = new CompleteTaskCommand(task.Id.Value);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Should_CreateJournalEntry_When_TaskCompleted()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new CompleteTaskCommand(task.Id.Value);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(_journalHandler.Entries);
        Assert.Equal(JournalEventType.TaskCompleted, _journalHandler.Entries[0].EventType);
    }

    private DeveloperTask CreateAndAddTask()
    {
        var task = DeveloperTask.Create(
            TaskTitle.Create("Task to complete").Value, 
            null, 
            DateTime.UtcNow);
        _repository.Tasks.Add(task);
        return task;
    }

    private sealed class FakeCreateEntryHandler : CreateEntryCommandHandler
    {
        public List<(JournalEventType EventType, Guid ResourceId, DateTime OccurredAt)> Entries { get; } = new();

        public FakeCreateEntryHandler() : base(new InMemoryJournalRepository()) { }

        public new async Task<CreateEntryResult> HandleAsync(CreateEntryCommand command, CancellationToken ct = default)
        {
            Entries.Add((command.EventType, command.ResourceId, command.OccurredAt));
            return await base.HandleAsync(command, ct);
        }
    }

    private sealed class InMemoryJournalRepository : IJournalEntryRepository
    {
        private readonly List<JournalEntry> _entries = new();

        public Task AddAsync(JournalEntry entry, CancellationToken ct = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<JournalEntry?> GetByIdAsync(JournalEntryId id, CancellationToken ct = default) =>
            Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<JournalEntry>> GetEntriesByDateAsync(DateOnly date, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<JournalEntry>>(_entries);

        public Task UpdateAsync(JournalEntry entry, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
