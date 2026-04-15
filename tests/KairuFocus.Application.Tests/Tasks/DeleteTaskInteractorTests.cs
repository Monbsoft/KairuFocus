using KairuFocus.Application.Tasks.DeleteTask;
using KairuFocus.Domain.Tasks;

namespace KairuFocus.Application.Tests.Tasks;

public sealed class DeleteTaskInteractorTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly FakeDeleteTaskPresenter _presenter = new();
    private readonly DeleteTaskInteractor _sut;

    public DeleteTaskInteractorTests() =>
        _sut = new DeleteTaskInteractor(_repository, _presenter);

    [Fact]
    public async Task Should_PresentSuccess_When_TaskExists()
    {
        var task = CreateAndAddTask();

        await _sut.Execute(new DeleteTaskRequest(task.Id.Value));

        Assert.True(_presenter.IsSuccess);
        Assert.Empty(_repository.Tasks);
    }

    [Fact]
    public async Task Should_PresentNotFound_When_TaskDoesNotExist()
    {
        await _sut.Execute(new DeleteTaskRequest(Guid.NewGuid()));

        Assert.True(_presenter.IsNotFound);
        Assert.False(_presenter.IsSuccess);
    }

    private DeveloperTask CreateAndAddTask()
    {
        var task = DeveloperTask.Create(TaskTitle.Create("Task to delete").Value, null, DateTime.UtcNow);
        _repository.Tasks.Add(task);
        return task;
    }

    private sealed class FakeDeleteTaskPresenter : IDeleteTaskPresenter
    {
        public bool IsSuccess { get; private set; }
        public bool IsNotFound { get; private set; }
        public string? FailureReason { get; private set; }

        public void PresentSuccess() => IsSuccess = true;
        public void PresentNotFound() => IsNotFound = true;
        public void PresentFailure(string reason) => FailureReason = reason;
    }
}
