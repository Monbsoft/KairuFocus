using Kairu.Application.Tasks.Common;

namespace Kairu.Application.Tasks.Queries.GetTaskById;

/// <summary>
/// Result of the GetTaskById query. Task is null when not found.
/// </summary>
public sealed record GetTaskByIdResult(TaskViewModel? Task);
