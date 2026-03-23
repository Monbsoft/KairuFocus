using Kairudev.Application.Common;
using Kairudev.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Pomodoro.Commands.SaveSettings;

public sealed class SaveSettingsCommandHandler : ICommandHandler<SaveSettingsCommand, SaveSettingsResult>
{
    private readonly IPomodoroSettingsRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SaveSettingsCommandHandler> _logger;

    public SaveSettingsCommandHandler(
        IPomodoroSettingsRepository repository,
        ICurrentUserService currentUserService,
        ILogger<SaveSettingsCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SaveSettingsResult> Handle(
        SaveSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Saving Pomodoro settings for user {UserId}", userId);

        var settingsResult = PomodoroSettings.Create(
            command.SprintDurationMinutes,
            command.ShortBreakDurationMinutes,
            command.LongBreakDurationMinutes);

        if (settingsResult.IsFailure)
            return SaveSettingsResult.Validation(settingsResult.Error);

        await _repository.SaveAsync(settingsResult.Value, userId, cancellationToken);
        _logger.LogInformation("Pomodoro settings saved for user {UserId}", userId);
        return SaveSettingsResult.Success();
    }
}
