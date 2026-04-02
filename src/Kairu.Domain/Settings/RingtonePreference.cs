namespace Kairu.Domain.Settings;

/// <summary>
/// Value object representing the user's ringtone preference for Pomodoro session end.
/// </summary>
public enum RingtonePreference
{
    /// <summary>
    /// No sound.
    /// </summary>
    None = 0,

    /// <summary>
    /// Alarm clock sound.
    /// </summary>
    AlarmClock = 1,

    /// <summary>
    /// Bird sound.
    /// </summary>
    Bird = 2
}
