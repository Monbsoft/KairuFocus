using Kairu.Application.Common;
using Kairu.Application.Journal.Commands.CreateEntry;
using Kairu.Domain.Journal;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Tasks.Commands.CompleteTask;

/// <summary>
/// Handles the CompleteTask command.
/// </summary>
public sealed class CompleteTaskCommandHandler : ICommandHandler<CompleteTaskCommand, CompleteTaskResult>
{
    private readonly ITaskRepository _repository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CompleteTaskCommandHandler> _logger;

    public CompleteTaskCommandHandler(
        ITaskRepository repository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<CompleteTaskCommandHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<CompleteTaskResult> Handle(
        CompleteTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Fetching task {TaskId} for user {UserId}", command.TaskId, userId);

        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), userId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for user {UserId}", command.TaskId, userId);
            return CompleteTaskResult.NotFound();
        }

        var result = task.Complete();
        if (result.IsFailure)
            return CompleteTaskResult.Failure(result.Error);

        await _repository.UpdateAsync(task, cancellationToken);
        _logger.LogInformation("Task {TaskId} completed by user {UserId}", command.TaskId, userId);

        // Generate journal entry
        await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
            new CreateEntryCommand(
                JournalEventType.TaskCompleted,
                task.Id.Value,
                DateTime.UtcNow,
                userId),
            cancellationToken);

        return CompleteTaskResult.Success();
    }
}
