using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler
{
    private readonly ITaskRepository _repository;

    public DeleteTaskCommandHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeleteTaskResult> HandleAsync(
        DeleteTaskCommand command,
        CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), cancellationToken);
        if (task is null)
            return DeleteTaskResult.NotFound();

        await _repository.DeleteAsync(task.Id, cancellationToken);
        return DeleteTaskResult.Success();
    }
}
