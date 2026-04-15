using KairuFocus.Application.Tasks.Queries.GetTaskById;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Tasks;

public sealed class GetTaskByIdQueryHandlerTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly GetTaskByIdQueryHandler _sut;

    public GetTaskByIdQueryHandlerTests() =>
        _sut = new GetTaskByIdQueryHandler(_repository, new FakeCurrentUserService(), NullLogger<GetTaskByIdQueryHandler>.Instance);

    [Fact]
    public async Task Should_ReturnTask_When_TaskExistsForCurrentUser()
    {
        // Arrange
        var title = TaskTitle.Create("Implement feature").Value;
        var task = DeveloperTask.Create(title, null, DateTime.UtcNow, FakeCurrentUserService.TestUserId);
        _repository.Tasks.Add(task);

        // Act
        var result = await _sut.Handle(new GetTaskByIdQuery(task.Id.Value));

        // Assert
        Assert.NotNull(result.Task);
        Assert.Equal(task.Id.Value, result.Task.Id);
        Assert.Equal("Implement feature", result.Task.Title);
    }

    [Fact]
    public async Task Should_ReturnNullTask_When_TaskDoesNotExist()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var result = await _sut.Handle(new GetTaskByIdQuery(unknownId));

        // Assert
        Assert.Null(result.Task);
    }

    [Fact]
    public async Task Should_ReturnNullTask_When_TaskBelongsToAnotherUser()
    {
        // Arrange
        var otherUserId = Domain.Identity.UserId.From(Guid.NewGuid());
        var title = TaskTitle.Create("Other user task").Value;
        var task = DeveloperTask.Create(title, null, DateTime.UtcNow, otherUserId);
        _repository.Tasks.Add(task);

        // Act
        var result = await _sut.Handle(new GetTaskByIdQuery(task.Id.Value));

        // Assert
        Assert.Null(result.Task);
    }
}
