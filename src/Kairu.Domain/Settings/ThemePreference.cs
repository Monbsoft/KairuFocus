namespace Kairu.Domain.Settings;

/// <summary>
/// Value object representing the user's theme preference.
/// </summary>
public enum ThemePreference
{
    /// <summary>
    /// Light theme.
    /// </summary>
    Light = 0,

    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark = 1,

    /// <summary>
    /// Follow system preference.
    /// </summary>
    System = 2
}
