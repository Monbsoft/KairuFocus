using Kairudev.Application.Common;
using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler : ICommandHandler<UpdateTaskCommand, UpdateTaskResult>
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateTaskCommandHandler> _logger;

    public UpdateTaskCommandHandler(
        ITaskRepository repository,
        ICurrentUserService currentUserService,
        ILogger<UpdateTaskCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UpdateTaskResult> Handle(
        UpdateTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Updating task {TaskId} for user {UserId}", command.TaskId, userId);

        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), userId, cancellationToken);
        if (task is null)
            return UpdateTaskResult.NotFound();

        var titleResult = TaskTitle.Create(command.Title);
        if (titleResult.IsFailure)
            return UpdateTaskResult.Failure(titleResult.Error);

        var descriptionResult = TaskDescription.Create(command.Description);
        if (descriptionResult.IsFailure)
            return UpdateTaskResult.Failure(descriptionResult.Error);

        task.UpdateDetails(titleResult.Value, descriptionResult.Value);

        await _repository.UpdateAsync(task, cancellationToken);
        _logger.LogInformation("Task {TaskId} updated by user {UserId}", command.TaskId, userId);
        return UpdateTaskResult.Success(TaskViewModel.From(task));
    }
}
