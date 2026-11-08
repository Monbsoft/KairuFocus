using KairuFocus.Domain.Community;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Community.Common;

/// <summary>
/// Converts between the step types of a group session (<c>Community</c>) and the Pomodoro session types.
/// Lives in the Application: <c>Community</c> and <c>Pomodoro</c> ignore each other in the Domain.
/// </summary>
internal static class GroupStepTypeMapping
{
    /// <exception cref="ArgumentOutOfRangeException">An unknown value: a programmer error.</exception>
    internal static PomodoroSessionType ToSessionType(this GroupStepType type) =>
        type switch
        {
            GroupStepType.Sprint => PomodoroSessionType.Sprint,
            GroupStepType.ShortBreak => PomodoroSessionType.ShortBreak,
            GroupStepType.LongBreak => PomodoroSessionType.LongBreak,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown group step type."),
        };

    /// <exception cref="ArgumentOutOfRangeException">An unknown value: a programmer error.</exception>
    internal static GroupStepType ToStepType(this PomodoroSessionType type) =>
        type switch
        {
            PomodoroSessionType.Sprint => GroupStepType.Sprint,
            PomodoroSessionType.ShortBreak => GroupStepType.ShortBreak,
            PomodoroSessionType.LongBreak => GroupStepType.LongBreak,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown Pomodoro session type."),
        };
}
