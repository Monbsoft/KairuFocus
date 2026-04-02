using Kairu.Application.Tasks.Commands.AddTask;
using Kairu.Application.Tests.Common;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kairu.Application.Tests.Tasks;

public sealed class AddTaskWithTagsCommandHandlerTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly AddTaskCommandHandler _sut;

    public AddTaskWithTagsCommandHandlerTests() =>
        _sut = new AddTaskCommandHandler(_repository, new FakeCurrentUserService(), NullLogger<AddTaskCommandHandler>.Instance);

    [Fact]
    public async Task Should_CreateTaskWithTags_When_TagsProvided()
    {
        // Arrange
        var command = new AddTaskCommand("Fix login", null, ["bug", "urgent"]);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Task);
        Assert.Equal(2, result.Task.Tags.Count);
        Assert.Contains("bug", result.Task.Tags);
        Assert.Contains("urgent", result.Task.Tags);
    }

    [Fact]
    public async Task Should_CreateTaskWithoutTags_When_NoTagsProvided()
    {
        // Arrange
        var command = new AddTaskCommand("Write docs", null);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Task);
        Assert.Empty(result.Task.Tags);
    }

    [Fact]
    public async Task Should_Fail_When_TagIsEmpty()
    {
        // Arrange
        var command = new AddTaskCommand("Fix login", null, [""]);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Tasks.TagEmpty, result.Error);
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public async Task Should_Fail_When_TagTooLong()
    {
        // Arrange
        var longTag = new string('a', TaskTag.MaxLength + 1);
        var command = new AddTaskCommand("Fix login", null, [longTag]);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Tasks.TagTooLong, result.Error);
        Assert.Empty(_repository.Tasks);
    }
}
