using KairuFocus.Domain.Common;

namespace KairuFocus.Application.Settings.Commands.SaveThemePreference;

public sealed class SaveThemePreferenceResult : Result
{
    private SaveThemePreferenceResult(bool isSuccess, string error) : base(isSuccess, error) { }

    public static new SaveThemePreferenceResult Success() => new(true, string.Empty);
    public static new SaveThemePreferenceResult Failure(string error) => new(false, error);
}

