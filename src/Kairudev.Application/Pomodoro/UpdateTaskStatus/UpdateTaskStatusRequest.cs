namespace Kairudev.Application.Pomodoro.UpdateTaskStatus;

/// <summary>TargetStatus: "InProgress" ou "Done"</summary>
public sealed record UpdateTaskStatusRequest(Guid TaskId, string TargetStatus);
