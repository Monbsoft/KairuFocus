using KairuFocus.Application.Pomodoro.Commands.UpdateTaskStatus;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Tasks;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using DomainTaskStatus = KairuFocus.Domain.Tasks.TaskStatus;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;
using TaskErrors = KairuFocus.Domain.Tasks.DomainErrors;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class UpdateTaskStatusCommandHandlerTests
{
    private readonly FakePomodoroSessionRepository _sessionRepository = new();
    private readonly FakeTaskRepository _taskRepository = new();
    private readonly UpdateTaskStatusCommandHandler _sut;

    public UpdateTaskStatusCommandHandlerTests()
    {
        _sut = new UpdateTaskStatusCommandHandler(
            _sessionRepository,
            _taskRepository,
            new FakeCurrentUserService(),
            NullLogger<UpdateTaskStatusCommandHandler>.Instance);
    }

    // ── Handler paths ─────────────────────────────────────────────────────
    // The handler reports three unrelated refusals through the same Failure(string):
    // no active session, invalid status and the domain SameStatus. The tests below pin the
    // first and the last one, so that the parsing tests cannot be satisfied by either.

    [Fact]
    public async Task Should_ReturnFailure_When_NoSessionIsActive()
    {
        var task = GivenTask();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, "Done"));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.NoActiveSession, result.Error);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TaskDoesNotExist()
    {
        GivenActiveSession();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(Guid.NewGuid(), "Done"));

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_TaskIsAlreadyInTargetStatus()
    {
        GivenActiveSession();
        var task = GivenTask();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, "Pending"));

        Assert.False(result.IsSuccess);
        Assert.Equal(TaskErrors.Tasks.SameStatus, result.Error);
    }

    // ── Target status parsing: exactly one member name, case-insensitive ──

    // Only the targets the sprint page sends (InProgress, Done): whether Pending should be
    // reachable from here is a use-case question this parsing test does not settle.
    [Theory]
    [InlineData("InProgress", DomainTaskStatus.InProgress)]
    [InlineData("inprogress", DomainTaskStatus.InProgress)]
    [InlineData("INPROGRESS", DomainTaskStatus.InProgress)]
    [InlineData("Done", DomainTaskStatus.Done)]
    [InlineData("done", DomainTaskStatus.Done)]
    [InlineData("DONE", DomainTaskStatus.Done)]
    public async Task Should_UpdateTaskStatus_When_TargetStatusIsMemberName(
        string value, DomainTaskStatus expected)
    {
        GivenActiveSession();
        var task = GivenTask();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, value));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Task);
        Assert.Equal(expected, task.Status);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("-1")]
    public async Task Should_ReturnFailure_When_TargetStatusIsNumericOutsideEnum(string value)
    {
        GivenActiveSession();
        var task = GivenTask();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalid(result, value);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
    }

    // Names only: numeric values are not part of the contract, even when they map to a member.
    // The initial status always differs from the member the number maps to: otherwise the
    // domain would refuse the change (SameStatus) whatever the parsing does.
    [Theory]
    [InlineData("0", DomainTaskStatus.InProgress)]
    [InlineData("1", DomainTaskStatus.Pending)]
    [InlineData("2", DomainTaskStatus.Pending)]
    public async Task Should_ReturnFailure_When_TargetStatusIsNumericMatchingMember(
        string value, DomainTaskStatus initialStatus)
    {
        GivenActiveSession();
        var task = GivenTask(initialStatus);

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalid(result, value);
        Assert.Equal(initialStatus, task.Status);
    }

    // Enum.TryParse ORs comma-separated names — "Pending,Done" is Done (0|2), "InProgress,Done"
    // is the undefined value 3 — and trims surrounding whitespace. The task starts Pending so
    // that none of these would be refused by the domain as SameStatus.
    [Theory]
    [InlineData("Pending,Done")]
    [InlineData("InProgress,Done")]
    [InlineData(" Done ")]
    [InlineData("InProgress ")]
    public async Task Should_ReturnFailure_When_TargetStatusIsNotExactlyOneMemberName(string value)
    {
        GivenActiveSession();
        var task = GivenTask();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalid(result, value);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
    }

    [Theory]
    [InlineData("Archived")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_ReturnFailure_When_TargetStatusIsUnknown(string value)
    {
        GivenActiveSession();
        var task = GivenTask();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, value));

        AssertRejectedAsInvalid(result, value);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_TargetStatusIsNull()
    {
        GivenActiveSession();
        var task = GivenTask();

        var result = await _sut.Handle(new UpdateTaskStatusCommand(task.Id.Value, null!));

        AssertRejectedAsInvalid(result, null);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
    }

    /// <summary>
    /// The error text is the only thing that tells an invalid status apart from the two other
    /// refusals sharing <c>Failure(string)</c>, hence the assertion on the exact message.
    /// </summary>
    private static void AssertRejectedAsInvalid(UpdateTaskStatusResult result, string? value)
    {
        Assert.False(result.IsSuccess);
        Assert.False(result.IsNotFound);
        Assert.Null(result.Task);
        Assert.Equal($"Invalid status: {value}", result.Error);
    }

    private void GivenActiveSession()
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, FakeCurrentUserService.TestUserId);
        session.Start(DateTime.UtcNow);
        _sessionRepository.Sessions.Add(session);
    }

    private DeveloperTask GivenTask(DomainTaskStatus initialStatus = DomainTaskStatus.Pending)
    {
        var task = DeveloperTask.Create(
            TaskTitle.Create("Task to update").Value,
            null,
            DateTime.UtcNow,
            FakeCurrentUserService.TestUserId);

        if (initialStatus != DomainTaskStatus.Pending)
            task.ChangeStatus(initialStatus, DateTime.UtcNow);

        _taskRepository.Tasks.Add(task);
        return task;
    }
}
