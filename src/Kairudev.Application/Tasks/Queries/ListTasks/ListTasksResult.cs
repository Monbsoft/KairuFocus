using Kairudev.Application.Tasks.Common;

namespace Kairudev.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Result of the ListTasks query.
/// </summary>
public sealed record ListTasksResult(IReadOnlyList<TaskViewModel> Tasks);
