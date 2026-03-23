using Kairudev.Application.Common;
using Kairudev.Domain.Settings;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Settings.Commands.SaveRingtonePreference;

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
        if (!Enum.TryParse<RingtonePreference>(command.RingtonePreference, ignoreCase: true, out var ringtonePreference))
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
