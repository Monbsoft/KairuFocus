using Kairu.Application.Tasks.Common;
using Kairu.Application.Tasks.UpdateTask;
using Kairu.Domain.Tasks;

namespace Kairu.Application.Tests.Tasks;

public sealed class UpdateTaskInteractorTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly FakeUpdateTaskPresenter _presenter = new();

    [Fact]
    public async Task Should_PresentSuccess_When_TaskIsUpdated()
    {
        var existingTask = DeveloperTask.Create(
            TaskTitle.Create("Old title").Value,
            null,
            DateTime.UtcNow);
        _repository.Tasks.Add(existingTask);
        var interactor = new UpdateTaskInteractor(_repository, _presenter);

        await interactor.Execute(
            new UpdateTaskRequest(existingTask.Id.Value, "New title", "New description"));

        Assert.NotNull(_presenter.Task);
        Assert.Equal("New title", _presenter.Task.Title);
        Assert.Equal("New description", _presenter.Task.Description);
    }

    [Fact]
    public async Task Should_UpdateOnlyTitle_When_DescriptionIsNull()
    {
        var existingTask = DeveloperTask.Create(
            TaskTitle.Create("Old title").Value,
            TaskDescription.Create("Old description").Value,
            DateTime.UtcNow);
        _repository.Tasks.Add(existingTask);
        var interactor = new UpdateTaskInteractor(_repository, _presenter);

        await interactor.Execute(
            new UpdateTaskRequest(existingTask.Id.Value, "New title", null));

        Assert.NotNull(_presenter.Task);
        Assert.Equal("New title", _presenter.Task.Title);
        Assert.Null(_presenter.Task.Description);
    }

    [Fact]
    public async Task Should_PresentNotFound_When_TaskDoesNotExist()
    {
        var interactor = new UpdateTaskInteractor(_repository, _presenter);

        await interactor.Execute(new UpdateTaskRequest(Guid.NewGuid(), "Title", null));

        Assert.True(_presenter.NotFoundCalled);
    }

    [Fact]
    public async Task Should_PresentValidationError_When_TitleIsEmpty()
    {
        var existingTask = DeveloperTask.Create(
            TaskTitle.Create("Old title").Value,
            null,
            DateTime.UtcNow);
        _repository.Tasks.Add(existingTask);
        var interactor = new UpdateTaskInteractor(_repository, _presenter);

        await interactor.Execute(
            new UpdateTaskRequest(existingTask.Id.Value, "", null));

        Assert.Equal(DomainErrors.Tasks.EmptyTitle, _presenter.ValidationError);
    }

    [Fact]
    public async Task Should_PresentValidationError_When_TitleIsTooLong()
    {
        var existingTask = DeveloperTask.Create(
            TaskTitle.Create("Old title").Value,
            null,
            DateTime.UtcNow);
        _repository.Tasks.Add(existingTask);
        var interactor = new UpdateTaskInteractor(_repository, _presenter);
        var longTitle = new string('x', TaskTitle.MaxLength + 1);

        await interactor.Execute(
            new UpdateTaskRequest(existingTask.Id.Value, longTitle, null));

        Assert.Equal(DomainErrors.Tasks.TitleTooLong, _presenter.ValidationError);
    }

    [Fact]
    public async Task Should_PresentValidationError_When_DescriptionIsTooLong()
    {
        var existingTask = DeveloperTask.Create(
            TaskTitle.Create("Old title").Value,
            null,
            DateTime.UtcNow);
        _repository.Tasks.Add(existingTask);
        var interactor = new UpdateTaskInteractor(_repository, _presenter);
        var longDescription = new string('x', TaskDescription.MaxLength + 1);

        await interactor.Execute(
            new UpdateTaskRequest(existingTask.Id.Value, "Valid title", longDescription));

        Assert.Equal(DomainErrors.Tasks.DescriptionTooLong, _presenter.ValidationError);
    }

    private sealed class FakeUpdateTaskPresenter : IUpdateTaskPresenter
    {
        public TaskViewModel? Task { get; private set; }
        public string? ValidationError { get; private set; }
        public bool NotFoundCalled { get; private set; }

        public void PresentSuccess(TaskViewModel task) => Task = task;
        public void PresentValidationError(string error) => ValidationError = error;
        public void PresentNotFound() => NotFoundCalled = true;
    }
}
