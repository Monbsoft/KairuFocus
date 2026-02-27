using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Tasks.Commands.AddTask;

/// <summary>
/// Handles the AddTask command.
/// </summary>
public sealed class AddTaskCommandHandler
{
    private readonly ITaskRepository _repository;

    public AddTaskCommandHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<AddTaskResult> HandleAsync(
        AddTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate title
        var titleResult = TaskTitle.Create(command.Title);
        if (titleResult.IsFailure)
            return AddTaskResult.Failure(titleResult.Error);

        // Validate description
        var descriptionResult = TaskDescription.Create(command.Description);
        if (descriptionResult.IsFailure)
            return AddTaskResult.Failure(descriptionResult.Error);

        // Create domain entity
        var task = DeveloperTask.Create(
            titleResult.Value,
            descriptionResult.Value,
            DateTime.UtcNow);

        // Persist
        await _repository.AddAsync(task, cancellationToken);

        // Return result
        return AddTaskResult.Success(TaskViewModel.From(task));
    }
}
