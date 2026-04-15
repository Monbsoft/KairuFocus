using KairuFocus.Application.Common;
using KairuFocus.Domain.Common;
using KairuFocus.Domain.Settings;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Settings.Commands.SaveThemePreference;

public sealed class SaveThemePreferenceCommandHandler : ICommandHandler<SaveThemePreferenceCommand, SaveThemePreferenceResult>
{
    private readonly IUserSettingsRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SaveThemePreferenceCommandHandler> _logger;

    public SaveThemePreferenceCommandHandler(
        IUserSettingsRepository repository,
        ICurrentUserService currentUserService,
        ILogger<SaveThemePreferenceCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SaveThemePreferenceResult> Handle(SaveThemePreferenceCommand command, CancellationToken cancellationToken = default)
    {
        // Parse theme preference
        if (!Enum.TryParse<ThemePreference>(command.ThemePreference, ignoreCase: true, out var themePreference))
        {
            return SaveThemePreferenceResult.Failure($"Invalid theme preference '{command.ThemePreference}'. Valid values: Light, Dark, System.");
        }

        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Saving theme preference {ThemePreference} for user {UserId}", themePreference, userId);

        // Get settings (creates default if not exists)
        var settings = await _repository.GetByUserIdAsync(userId);

        // Update theme preference
        settings.UpdateThemePreference(themePreference);

        // Persist
        await _repository.UpdateAsync(settings);

        _logger.LogInformation("Theme preference updated to {ThemePreference} for user {UserId}", themePreference, userId);
        return SaveThemePreferenceResult.Success();
    }
}
