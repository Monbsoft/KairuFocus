using Kairu.Application.Tasks.Common;

namespace Kairu.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Result of the ListTasks query.
/// </summary>
public sealed record ListTasksResult(IReadOnlyList<TaskViewModel> Tasks);
