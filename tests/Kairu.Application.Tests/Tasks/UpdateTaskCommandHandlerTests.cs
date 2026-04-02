using Kairu.Application.Tasks.Commands.UpdateTask;
using Kairu.Application.Tests.Common;
using Kairu.Domain.Identity;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kairu.Application.Tests.Tasks;

public sealed class UpdateTaskCommandHandlerTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly UpdateTaskCommandHandler _sut;

    public UpdateTaskCommandHandlerTests() =>
        _sut = new UpdateTaskCommandHandler(_repository, new FakeCurrentUserService(), NullLogger<UpdateTaskCommandHandler>.Instance);

    private DeveloperTask CreateStoredTask(string title = "Initial title")
    {
        var titleResult = TaskTitle.Create(title);
        var task = DeveloperTask.Create(titleResult.Value, null, DateTime.UtcNow, FakeCurrentUserService.TestUserId);
        _repository.Tasks.Add(task);
        return task;
    }

    [Fact]
    public async Task Should_UpdateTags_When_TagsProvided()
    {
        // Arrange
        var task = CreateStoredTask();
        var command = new UpdateTaskCommand(task.Id.Value, "Initial title", null, ["backend", "priority"]);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Task);
        Assert.Equal(2, result.Task.Tags.Count);
        Assert.Contains("backend", result.Task.Tags);
        Assert.Contains("priority", result.Task.Tags);
    }

    [Fact]
    public async Task Should_ClearTags_When_EmptyTagsList()
    {
        // Arrange
        var existingTags = new[] { TaskTag.Create("old-tag").Value };
        var titleResult = TaskTitle.Create("Tagged task");
        var task = DeveloperTask.Create(titleResult.Value, null, DateTime.UtcNow, FakeCurrentUserService.TestUserId, existingTags);
        _repository.Tasks.Add(task);

        var command = new UpdateTaskCommand(task.Id.Value, "Tagged task", null, []);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Task);
        Assert.Empty(result.Task.Tags);
    }

    [Fact]
    public async Task Should_Fail_When_TooManyTags()
    {
        // Arrange
        var task = CreateStoredTask();
        var tooManyTags = Enumerable.Range(1, DeveloperTask.MaxTags + 1)
            .Select(i => $"tag{i}")
            .ToList();
        var command = new UpdateTaskCommand(task.Id.Value, "Initial title", null, tooManyTags);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Tasks.TooManyTags, result.Error);
    }

    [Fact]
    public async Task Should_Fail_When_DuplicateTag()
    {
        // Arrange
        var task = CreateStoredTask();
        var command = new UpdateTaskCommand(task.Id.Value, "Initial title", null, ["bug", "Bug"]);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Tasks.DuplicateTag, result.Error);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TaskDoesNotExist()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), "Title", null, ["bug"]);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }
}
