using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Pomodoro.Commands.SaveSettings;

public sealed record SaveSettingsCommand(
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes) : ICommand<SaveSettingsResult>;
