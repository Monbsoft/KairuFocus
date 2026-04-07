using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Pomodoro.Commands.CreateTaskDuringSession;

public sealed record CreateTaskDuringSessionCommand(string Title, string? Description) : ICommand<CreateTaskDuringSessionResult>;
