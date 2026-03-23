using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Pomodoro.Commands.SaveSettings;

public sealed record SaveSettingsCommand(
    int SprintDurationMinutes,
    int ShortBreakDurationMinutes,
    int LongBreakDurationMinutes) : ICommand<SaveSettingsResult>;
