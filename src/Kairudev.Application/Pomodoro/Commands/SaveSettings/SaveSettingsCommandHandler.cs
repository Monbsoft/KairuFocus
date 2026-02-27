using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.Commands.SaveSettings;

public sealed class SaveSettingsCommandHandler
{
    private readonly IPomodoroSettingsRepository _repository;

    public SaveSettingsCommandHandler(IPomodoroSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SaveSettingsResult> HandleAsync(
        SaveSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var settingsResult = PomodoroSettings.Create(
            command.SprintDurationMinutes,
            command.ShortBreakDurationMinutes,
            command.LongBreakDurationMinutes);

        if (settingsResult.IsFailure)
            return SaveSettingsResult.Validation(settingsResult.Error);

        await _repository.SaveAsync(settingsResult.Value, cancellationToken);
        return SaveSettingsResult.Success();
    }
}
