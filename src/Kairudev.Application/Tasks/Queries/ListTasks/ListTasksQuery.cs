using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Tasks.Queries.ListTasks;

public sealed record ListTasksQuery(
    string? SearchTerm = null,
    TaskStatusFilter StatusFilter = TaskStatusFilter.OpenOnly) : IQuery<ListTasksResult>;
