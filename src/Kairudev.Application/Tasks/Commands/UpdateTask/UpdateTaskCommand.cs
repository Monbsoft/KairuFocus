namespace Kairudev.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(Guid TaskId, string Title, string? Description);
