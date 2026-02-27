using Kairudev.Domain.Settings;

namespace Kairudev.Application.Settings.Commands.SaveRingtonePreference;

public sealed class SaveRingtonePreferenceCommandHandler
{
    private readonly IUserSettingsRepository _repository;

    public SaveRingtonePreferenceCommandHandler(IUserSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SaveRingtonePreferenceResult> Handle(SaveRingtonePreferenceCommand command)
    {
        if (!Enum.TryParse<RingtonePreference>(command.RingtonePreference, ignoreCase: true, out var ringtonePreference))
        {
            return SaveRingtonePreferenceResult.Failure($"Invalid ringtone preference '{command.RingtonePreference}'. Valid values: None, AlarmClock, Bird.");
        }

        var settings = await _repository.GetAsync();
        settings.UpdateRingtonePreference(ringtonePreference);
        await _repository.UpdateAsync(settings);

        return SaveRingtonePreferenceResult.Success();
    }
}
