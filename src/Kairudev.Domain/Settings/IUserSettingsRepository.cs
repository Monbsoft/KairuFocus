namespace Kairudev.Domain.Settings;

/// <summary>
/// Repository for user settings.
/// </summary>
public interface IUserSettingsRepository
{
    /// <summary>
    /// Gets the user settings (singleton).
    /// Creates default settings if none exist.
    /// </summary>
    Task<UserSettings> GetAsync();

    /// <summary>
    /// Updates the user settings.
    /// </summary>
    Task UpdateAsync(UserSettings settings);
}
