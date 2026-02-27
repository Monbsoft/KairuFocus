using Kairudev.Application.Journal.Commands.CreateEntry;
using Kairudev.Domain.Journal;
using Kairudev.Domain.Tasks;
using TaskStatus = Kairudev.Domain.Tasks.TaskStatus;

namespace Kairudev.Application.Tasks.Commands.CompleteTask;

/// <summary>
/// Handles the CompleteTask command.
/// </summary>
public sealed class CompleteTaskCommandHandler
{
    private readonly ITaskRepository _repository;
    private readonly CreateEntryCommandHandler _journalHandler;

    public CompleteTaskCommandHandler(
        ITaskRepository repository,
        CreateEntryCommandHandler journalHandler)
    {
        _repository = repository;
        _journalHandler = journalHandler;
    }

    public async Task<CompleteTaskResult> HandleAsync(
        CompleteTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), cancellationToken);
        if (task is null)
            return CompleteTaskResult.NotFound();

        var result = task.Complete();
        if (result.IsFailure)
            return CompleteTaskResult.Failure(result.Error);

        await _repository.UpdateAsync(task, cancellationToken);

        // Generate journal entry
        await _journalHandler.HandleAsync(
            new CreateEntryCommand(
                JournalEventType.TaskCompleted,
                task.Id.Value,
                DateTime.UtcNow),
            cancellationToken);

        return CompleteTaskResult.Success();
    }
}
