using KairuFocus.Application.Tasks.Common;

namespace KairuFocus.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Result of the ListTasks query.
/// </summary>
public sealed record ListTasksResult(IReadOnlyList<TaskViewModel> Tasks);
