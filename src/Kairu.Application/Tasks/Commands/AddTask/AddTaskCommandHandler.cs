using Kairu.Application.Common;
using Kairu.Application.Tasks.Common;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Tasks.Commands.AddTask;

/// <summary>
/// Handles the AddTask command.
/// </summary>
public sealed class AddTaskCommandHandler : ICommandHandler<AddTaskCommand, AddTaskResult>
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddTaskCommandHandler> _logger;

    public AddTaskCommandHandler(
        ITaskRepository repository,
        ICurrentUserService currentUserService,
        ILogger<AddTaskCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AddTaskResult> Handle(
        AddTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Creating task for user {UserId} with title '{Title}'", userId, command.Title);

        var titleResult = TaskTitle.Create(command.Title);
        if (titleResult.IsFailure)
            return AddTaskResult.Failure(titleResult.Error);

        var descriptionResult = TaskDescription.Create(command.Description);
        if (descriptionResult.IsFailure)
            return AddTaskResult.Failure(descriptionResult.Error);

        var tags = new List<TaskTag>();
        foreach (var raw in command.Tags ?? [])
        {
            var tagResult = TaskTag.Create(raw);
            if (tagResult.IsFailure)
                return AddTaskResult.Failure(tagResult.Error);
            tags.Add(tagResult.Value);
        }

        var task = DeveloperTask.Create(
            titleResult.Value,
            descriptionResult.Value,
            DateTime.UtcNow,
            userId,
            tags);

        await _repository.AddAsync(task, cancellationToken);

        _logger.LogInformation("Task {TaskId} '{Title}' created for user {UserId}", task.Id.Value, task.Title.Value, userId);

        return AddTaskResult.Success(TaskViewModel.From(task));
    }
}

