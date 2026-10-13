using KairuFocus.Application.Tasks.Commands.ChangeTaskStatus;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using DomainTaskStatus = KairuFocus.Domain.Tasks.TaskStatus;
using TaskErrors = KairuFocus.Domain.Tasks.DomainErrors;

namespace KairuFocus.Application.Tests.Tasks;

public sealed class ChangeTaskStatusCommandHandlerTests
{
    private readonly FakeTaskRepository _repository = new();
    private readonly FakeJournalEntryRepository _journalRepository = new();
    private readonly ChangeTaskStatusCommandHandler _sut;

    public ChangeTaskStatusCommandHandlerTests()
    {
        var fakeMediator = new FakeMediator(_journalRepository);
        _sut = new ChangeTaskStatusCommandHandler(
            _repository,
            fakeMediator,
            new FakeCurrentUserService(),
            NullLogger<ChangeTaskStatusCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TransitionIsValid()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new ChangeTaskStatusCommand(task.Id.Value, "InProgress");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Task);
        Assert.Equal(nameof(DomainTaskStatus.InProgress), result.Task!.Status);
    }

    [Fact]
    public async Task Should_UpdateTaskStatus_When_TransitionIsValid()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new ChangeTaskStatusCommand(task.Id.Value, "InProgress");

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Equal(DomainTaskStatus.InProgress, _repository.Tasks[0].Status);
    }

    [Fact]
    public async Task Should_ParseStatusCaseInsensitively_When_StatusIsLowercase()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new ChangeTaskStatusCommand(task.Id.Value, "done");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DomainTaskStatus.Done, _repository.Tasks[0].Status);
    }

    [Fact]
    public async Task Should_CreateTaskStartedJournalEntry_When_StatusChangesToInProgress()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new ChangeTaskStatusCommand(task.Id.Value, "InProgress");

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.TaskStarted, _journalRepository.Entries[0].EventType);
        Assert.Equal(task.Id.Value, _journalRepository.Entries[0].ResourceId);
    }

    [Fact]
    public async Task Should_CreateTaskCompletedJournalEntry_When_StatusChangesToDone()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new ChangeTaskStatusCommand(task.Id.Value, "Done");

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.TaskCompleted, _journalRepository.Entries[0].EventType);
    }

    [Fact]
    public async Task Should_CreateTaskCompletedJournalEntry_When_TransitioningFromInProgressToDone()
    {
        // Arrange
        var task = CreateAndAddTask(DomainTaskStatus.InProgress);
        var command = new ChangeTaskStatusCommand(task.Id.Value, "Done");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DomainTaskStatus.Done, _repository.Tasks[0].Status);
        Assert.Single(_journalRepository.Entries);
        Assert.Equal(JournalEventType.TaskCompleted, _journalRepository.Entries[0].EventType);
        Assert.Equal(task.Id.Value, _journalRepository.Entries[0].ResourceId);
    }

    [Fact]
    public async Task Should_NotCreateJournalEntry_When_StatusChangesToPending()
    {
        // Arrange
        var task = CreateAndAddTask(DomainTaskStatus.InProgress);
        var command = new ChangeTaskStatusCommand(task.Id.Value, "Pending");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_journalRepository.Entries);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TaskDoesNotExist()
    {
        // Arrange
        var command = new ChangeTaskStatusCommand(Guid.NewGuid(), "InProgress");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_StatusStringIsInvalid()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new ChangeTaskStatusCommand(task.Id.Value, "Archived");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ValidationError);
        Assert.Equal(DomainTaskStatus.Pending, _repository.Tasks[0].Status);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_TaskIsAlreadyInRequestedStatus()
    {
        // Arrange
        var task = CreateAndAddTask();
        var command = new ChangeTaskStatusCommand(task.Id.Value, "Pending");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ConflictError);
        Assert.Equal(TaskErrors.Tasks.SameStatus, result.ConflictError);
    }

    // ── Status parsing: exactly one member name, case-insensitive ─────────

    [Theory]
    [InlineData("Pending", DomainTaskStatus.InProgress, DomainTaskStatus.Pending)]
    [InlineData("pending", DomainTaskStatus.InProgress, DomainTaskStatus.Pending)]
    [InlineData("InProgress", DomainTaskStatus.Pending, DomainTaskStatus.InProgress)]
    [InlineData("INPROGRESS", DomainTaskStatus.Pending, DomainTaskStatus.InProgress)]
    [InlineData("Done", DomainTaskStatus.Pending, DomainTaskStatus.Done)]
    [InlineData("done", DomainTaskStatus.Pending, DomainTaskStatus.Done)]
    public async Task Should_ChangeStatus_When_StatusIsMemberName(
        string value, DomainTaskStatus initialStatus, DomainTaskStatus expected)
    {
        var task = CreateAndAddTask(initialStatus);

        var result = await _sut.Handle(new ChangeTaskStatusCommand(task.Id.Value, value));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, _repository.Tasks[0].Status);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("-1")]
    public async Task Should_ReturnValidationError_When_StatusIsNumericOutsideEnum(string value)
    {
        var task = CreateAndAddTask();

        var result = await _sut.Handle(new ChangeTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalidWithoutSideEffect(result, DomainTaskStatus.Pending);
    }

    // Names only: numeric values are not part of the contract, even when they map to a member.
    // The initial status always differs from the member the number maps to: otherwise the
    // request would be refused as a conflict (SameStatus) whatever the parsing does.
    [Theory]
    [InlineData("0", DomainTaskStatus.InProgress)]
    [InlineData("1", DomainTaskStatus.Pending)]
    [InlineData("2", DomainTaskStatus.Pending)]
    public async Task Should_ReturnValidationError_When_StatusIsNumericMatchingMember(
        string value, DomainTaskStatus initialStatus)
    {
        var task = CreateAndAddTask(initialStatus);

        var result = await _sut.Handle(new ChangeTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalidWithoutSideEffect(result, initialStatus);
    }

    // Enum.TryParse ORs comma-separated names — "Pending,Done" is Done (0|2), "InProgress,Done"
    // is the undefined value 3 — and trims surrounding whitespace. The task starts Pending so
    // that none of these would be refused as a conflict.
    [Theory]
    [InlineData("Pending,Done")]
    [InlineData("InProgress,Done")]
    [InlineData(" Done ")]
    [InlineData("InProgress ")]
    public async Task Should_ReturnValidationError_When_StatusIsNotExactlyOneMemberName(string value)
    {
        var task = CreateAndAddTask();

        var result = await _sut.Handle(new ChangeTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalidWithoutSideEffect(result, DomainTaskStatus.Pending);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnValidationError_When_StatusIsBlank(string value)
    {
        var task = CreateAndAddTask();

        var result = await _sut.Handle(new ChangeTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalidWithoutSideEffect(result, DomainTaskStatus.Pending);
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_StatusIsNull()
    {
        var task = CreateAndAddTask();

        var result = await _sut.Handle(new ChangeTaskStatusCommand(task.Id.Value, null!));

        AssertRejectedAsInvalidWithoutSideEffect(result, DomainTaskStatus.Pending);
    }

    private void AssertRejectedAsInvalidWithoutSideEffect(
        ChangeTaskStatusResult result, DomainTaskStatus unchangedStatus)
    {
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ValidationError);
        Assert.Null(result.ConflictError);
        Assert.Equal(unchangedStatus, _repository.Tasks[0].Status);
        Assert.Empty(_journalRepository.Entries);
    }

    private DeveloperTask CreateAndAddTask(DomainTaskStatus? initialStatus = null)
    {
        var task = DeveloperTask.Create(
            TaskTitle.Create("Task to change").Value,
            null,
            DateTime.UtcNow,
            FakeCurrentUserService.TestUserId);

        if (initialStatus is not null && initialStatus != DomainTaskStatus.Pending)
            task.ChangeStatus(initialStatus.Value, DateTime.UtcNow);

        _repository.Tasks.Add(task);
        return task;
    }
}
