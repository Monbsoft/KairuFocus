using Kairudev.Domain.Common;

namespace Kairudev.Domain.Settings;

/// <summary>
/// Aggregate root representing user settings.
/// In this single-user application, there is only one instance with Id = 1.
/// </summary>
public sealed class UserSettings : AggregateRoot<int>
{
    public const int SingletonId = 1;

    public ThemePreference ThemePreference { get; private set; }
    public RingtonePreference RingtonePreference { get; private set; }

    private UserSettings(int id, ThemePreference themePreference, RingtonePreference ringtonePreference) : base(id)
    {
        ThemePreference = themePreference;
        RingtonePreference = ringtonePreference;
    }

    /// <summary>
    /// Creates the default user settings instance.
    /// </summary>
    public static UserSettings CreateDefault()
    {
        return new UserSettings(SingletonId, ThemePreference.System, RingtonePreference.AlarmClock);
    }

    /// <summary>
    /// Updates the theme preference.
    /// </summary>
    public void UpdateThemePreference(ThemePreference newPreference)
    {
        ThemePreference = newPreference;
    }

    /// <summary>
    /// Updates the ringtone preference.
    /// </summary>
    public void UpdateRingtonePreference(RingtonePreference newPreference)
    {
        RingtonePreference = newPreference;
    }

    /// <summary>
    /// Reconstitutes a UserSettings from persistence.
    /// </summary>
    public static UserSettings Reconstitute(int id, ThemePreference themePreference, RingtonePreference ringtonePreference)
    {
        return new UserSettings(id, themePreference, ringtonePreference);
    }
}
