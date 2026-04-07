using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Pomodoro.Commands.UpdateTaskStatus;

public sealed record UpdateTaskStatusCommand(Guid TaskId, string TargetStatus) : ICommand<UpdateTaskStatusResult>;
