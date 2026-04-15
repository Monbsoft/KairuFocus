using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Tasks.Commands.ChangeTaskStatus;

public sealed record ChangeTaskStatusCommand(Guid TaskId, string NewStatus) : ICommand<ChangeTaskStatusResult>;
