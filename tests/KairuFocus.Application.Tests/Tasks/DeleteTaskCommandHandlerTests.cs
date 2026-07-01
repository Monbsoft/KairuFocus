using KairuFocus.Application.Tasks.Commands.DeleteTask;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Tasks;

public sealed class DeleteTaskCommandHandlerTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly DeleteTaskCommandHandler _sut;

    public DeleteTaskCommandHandlerTests() =>
        _sut = new DeleteTaskCommandHandler(
            _repository,
            new FakeCurrentUserService(),
            NullLogger<DeleteTaskCommandHandler>.Instance);

    [Fact]
    public async Task Should_ReturnSuccess_When_TaskExists()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new DeleteTaskCommand(task.Id.Value);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsNotFound);
    }

    [Fact]
    public async Task Should_RemoveTaskFromRepository_When_TaskExists()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new DeleteTaskCommand(task.Id.Value);

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TaskDoesNotExist()
    {
        // Arrange
        var command = new DeleteTaskCommand(Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_NotRemoveOtherTasks_When_TaskDoesNotExist()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new DeleteTaskCommand(Guid.NewGuid());

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Single(_repository.Tasks);
        Assert.Equal(task.Id, _repository.Tasks[0].Id);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TaskBelongsToAnotherUser()
    {
        // Arrange
        var otherUser = UserId.New();
        var task = DeveloperTask.Create(
            TaskTitle.Create("Someone else's task").Value,
            null,
            DateTime.UtcNow,
            otherUser);
        _repository.Tasks.Add(task);
        var command = new DeleteTaskCommand(task.Id.Value);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
        Assert.Single(_repository.Tasks);
        Assert.Equal(task.Id, _repository.Tasks[0].Id);
    }

    private DeveloperTask CreateAndAddTask()
    {
        var task = DeveloperTask.Create(
            TaskTitle.Create("Task to delete").Value,
            null,
            DateTime.UtcNow,
            FakeCurrentUserService.TestUserId);
        _repository.Tasks.Add(task);
        return task;
    }
}
