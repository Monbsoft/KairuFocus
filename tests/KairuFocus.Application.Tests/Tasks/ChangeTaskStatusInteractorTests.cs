using KairuFocus.Application.Journal.CreateJournalEntry;
using KairuFocus.Application.Tasks.ChangeTaskStatus;
using KairuFocus.Application.Tasks.Common;
using KairuFocus.Domain.Tasks;
using DomainTaskStatus = KairuFocus.Domain.Tasks.TaskStatus;

namespace KairuFocus.Application.Tests.Tasks;

public sealed class ChangeTaskStatusInteractorTests
{
    private readonly TrackingTaskRepository _repository = new();
    private readonly FakeChangeTaskStatusPresenter _presenter = new();
    private readonly NoOpCreateJournalEntryUseCase _noOpJournal = new();
    private readonly ChangeTaskStatusInteractor _sut;

    public ChangeTaskStatusInteractorTests() =>
        _sut = new ChangeTaskStatusInteractor(_repository, _presenter, _noOpJournal);

    [Fact]
    public async Task Should_PresentSuccess_When_TaskExistsAndStatusChanges()
    {
        var task = CreateAndAddPendingTask();

        await _sut.Execute(new ChangeTaskStatusRequest(task.Id.Value, "InProgress"));

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.ViewModel);
        Assert.Equal("InProgress", _presenter.ViewModel!.Status);
    }

    [Fact]
    public async Task Should_PresentSuccess_WithUpdatedTaskViewModel_When_StatusChangesToDone()
    {
        var task = CreateAndAddPendingTask();

        await _sut.Execute(new ChangeTaskStatusRequest(task.Id.Value, "Done"));

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.ViewModel);
        Assert.Equal("Done", _presenter.ViewModel!.Status);
        Assert.NotNull(_presenter.ViewModel.CompletedAt);
    }

    [Fact]
    public async Task Should_PresentNotFound_When_TaskDoesNotExist()
    {
        await _sut.Execute(new ChangeTaskStatusRequest(Guid.NewGuid(), "InProgress"));

        Assert.True(_presenter.IsNotFound);
        Assert.False(_presenter.IsSuccess);
    }

    [Fact]
    public async Task Should_PresentValidationError_When_StatusValueIsUnknown()
    {
        var task = CreateAndAddPendingTask();

        await _sut.Execute(new ChangeTaskStatusRequest(task.Id.Value, "InvalidStatus"));

        Assert.True(_presenter.IsValidationError);
        Assert.False(_presenter.IsSuccess);
    }

    [Fact]
    public async Task Should_PresentFailure_When_StatusIsAlreadyTheSame()
    {
        var task = CreateAndAddPendingTask();

        await _sut.Execute(new ChangeTaskStatusRequest(task.Id.Value, "Pending"));

        Assert.True(_presenter.IsFailure);
        Assert.Equal(DomainErrors.Tasks.SameStatus, _presenter.FailureReason);
    }

    [Fact]
    public async Task Should_CallUpdateAsync_When_StatusChanges()
    {
        var task = CreateAndAddPendingTask();

        await _sut.Execute(new ChangeTaskStatusRequest(task.Id.Value, "Done"));

        Assert.Equal(1, _repository.UpdateCallCount);
    }

    [Fact]
    public async Task Should_NotCallUpdateAsync_When_TaskNotFound()
    {
        await _sut.Execute(new ChangeTaskStatusRequest(Guid.NewGuid(), "InProgress"));

        Assert.Equal(0, _repository.UpdateCallCount);
    }

    [Fact]
    public async Task Should_NotCallUpdateAsync_When_StatusIsUnknown()
    {
        var task = CreateAndAddPendingTask();

        await _sut.Execute(new ChangeTaskStatusRequest(task.Id.Value, "Bogus"));

        Assert.Equal(0, _repository.UpdateCallCount);
    }

    private DeveloperTask CreateAndAddPendingTask()
    {
        var task = DeveloperTask.Create(TaskTitle.Create("A pending task").Value, null, DateTime.UtcNow);
        _repository.Tasks.Add(task);
        return task;
    }

    private sealed class NoOpCreateJournalEntryUseCase : ICreateJournalEntryUseCase
    {
        public Task Execute(CreateJournalEntryRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class TrackingTaskRepository : ITaskRepository
    {
        public List<DeveloperTask> Tasks { get; } = [];
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(DeveloperTask task, CancellationToken cancellationToken = default)
        {
            Tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task<DeveloperTask?> GetByIdAsync(TaskId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tasks.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<DeveloperTask>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DeveloperTask>>(Tasks.AsReadOnly());

        public Task UpdateAsync(DeveloperTask task, CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TaskId id, CancellationToken cancellationToken = default)
        {
            var task = Tasks.FirstOrDefault(t => t.Id == id);
            if (task is not null) Tasks.Remove(task);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeChangeTaskStatusPresenter : IChangeTaskStatusPresenter
    {
        public bool IsSuccess { get; private set; }
        public bool IsNotFound { get; private set; }
        public bool IsValidationError { get; private set; }
        public bool IsFailure { get; private set; }
        public TaskViewModel? ViewModel { get; private set; }
        public string? FailureReason { get; private set; }

        public void PresentSuccess(TaskViewModel task)
        {
            IsSuccess = true;
            ViewModel = task;
        }

        public void PresentNotFound() => IsNotFound = true;

        public void PresentValidationError(string error) => IsValidationError = true;

        public void PresentFailure(string reason)
        {
            IsFailure = true;
            FailureReason = reason;
        }
    }
}
