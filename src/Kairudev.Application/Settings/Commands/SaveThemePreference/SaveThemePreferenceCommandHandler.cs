using Kairudev.Domain.Common;
using Kairudev.Domain.Settings;

namespace Kairudev.Application.Settings.Commands.SaveThemePreference;

public sealed class SaveThemePreferenceCommandHandler
{
    private readonly IUserSettingsRepository _repository;

    public SaveThemePreferenceCommandHandler(IUserSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SaveThemePreferenceResult> Handle(SaveThemePreferenceCommand command)
    {
        // Parse theme preference
        if (!Enum.TryParse<ThemePreference>(command.ThemePreference, ignoreCase: true, out var themePreference))
        {
            return SaveThemePreferenceResult.Failure($"Invalid theme preference '{command.ThemePreference}'. Valid values: Light, Dark, System.");
        }

        // Get settings (creates default if not exists)
        var settings = await _repository.GetAsync();

        // Update theme preference
        settings.UpdateThemePreference(themePreference);

        // Persist
        await _repository.UpdateAsync(settings);

        return SaveThemePreferenceResult.Success();
    }
}
