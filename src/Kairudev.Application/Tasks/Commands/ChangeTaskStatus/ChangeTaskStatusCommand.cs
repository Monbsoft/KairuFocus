namespace Kairudev.Application.Tasks.Commands.ChangeTaskStatus;

public sealed record ChangeTaskStatusCommand(Guid TaskId, string NewStatus);
