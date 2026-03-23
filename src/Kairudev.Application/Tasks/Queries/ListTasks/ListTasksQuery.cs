using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Query to list all tasks (no parameters needed).
/// </summary>
public sealed record ListTasksQuery : IQuery<ListTasksResult>;
