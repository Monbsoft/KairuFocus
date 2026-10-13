using KairuFocus.Application.Common;
using KairuFocus.Domain.Settings;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Settings.Commands.SaveRingtonePreference;

public sealed class SaveRingtonePreferenceCommandHandler : ICommandHandler<SaveRingtonePreferenceCommand, SaveRingtonePreferenceResult>
{
    private readonly IUserSettingsRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SaveRingtonePreferenceCommandHandler> _logger;

    public SaveRingtonePreferenceCommandHandler(
        IUserSettingsRepository repository,
        ICurrentUserService currentUserService,
        ILogger<SaveRingtonePreferenceCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SaveRingtonePreferenceResult> Handle(SaveRingtonePreferenceCommand command, CancellationToken cancellationToken = default)
    {
        // Enum.TryParse alone also accepts numeric strings ("7", "1"), comma-separated lists ("None,Bird")
        // and surrounding whitespace (" Bird "). The contract is names only: accept a single member name,
        // case-insensitively, and nothing else.
        if (!Enum.TryParse<RingtonePreference>(command.RingtonePreference, ignoreCase: true, out var ringtonePreference)
            || !Enum.GetNames<RingtonePreference>().Contains(command.RingtonePreference, StringComparer.OrdinalIgnoreCase))
        {
            return SaveRingtonePreferenceResult.Failure($"Invalid ringtone preference '{command.RingtonePreference}'. Valid values: None, AlarmClock, Bird.");
        }

        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Saving ringtone preference {RingtonePreference} for user {UserId}", ringtonePreference, userId);
        var settings = await _repository.GetByUserIdAsync(userId);
        settings.UpdateRingtonePreference(ringtonePreference);
        await _repository.UpdateAsync(settings);

        _logger.LogInformation("Ringtone preference updated to {RingtonePreference} for user {UserId}", ringtonePreference, userId);
        return SaveRingtonePreferenceResult.Success();
    }
}
