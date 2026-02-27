using Kairudev.Application.Tasks.Common;
using Kairudev.Domain.Tasks;

namespace Kairudev.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Handles the ListTasks query.
/// </summary>
public sealed class ListTasksQueryHandler
{
    private readonly ITaskRepository _repository;

    public ListTasksQueryHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListTasksResult> HandleAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _repository.GetAllAsync(cancellationToken);
        var viewModels = tasks.Select(TaskViewModel.From).ToList();
        return new ListTasksResult(viewModels);
    }
}
