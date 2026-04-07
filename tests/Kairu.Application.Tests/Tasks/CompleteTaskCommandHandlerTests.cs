using Kairu.Application.Tasks.Commands.CompleteTask;
using Kairu.Application.Tests.Common;
using Kairu.Application.Tests.Journal;
using Kairu.Domain.Identity;
using Kairu.Domain.Journal;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using DomainTaskStatus = Kairu.Domain.Tasks.TaskStatus;

namespace Kairu.Application.Tests.Tasks;

public sealed class CompleteTaskCommandHandlerTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly FakeJournalEntryRepository _journalRepository = new();
    private readonly CompleteTaskCommandHandler _sut;

    public CompleteTaskCommandHandlerTests()
    {
        var fakeMediator = new FakeMediator(_journalRepository);
        _sut = new CompleteTaskCommandHandler(_repository, fakeMediator, new FakeCurrentUserService(), NullLogger<CompleteTaskCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TaskExistsAndIsPending()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new CompleteTaskCommand(task.Id.Value);

        // Act
        var result = await _sut.Handle(command);

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
        var result = await _sut.Handle(command);

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
        var result = await _sut.Handle(command);

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
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.TaskCompleted, _journalRepository.Entries[0].EventType);
    }

    private DeveloperTask CreateAndAddTask()
    {
        var task = DeveloperTask.Create(
            TaskTitle.Create("Task to complete").Value,
            null,
            DateTime.UtcNow,
            UserId.From("test-github-id-123"));
        _repository.Tasks.Add(task);
        return task;
    }
}
