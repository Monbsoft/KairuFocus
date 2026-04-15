using KairuFocus.Domain.Common;

namespace KairuFocus.Application.Settings.Commands.SaveRingtonePreference;

public sealed class SaveRingtonePreferenceResult : Result
{
    private SaveRingtonePreferenceResult(bool isSuccess, string error) : base(isSuccess, error) { }

    public static new SaveRingtonePreferenceResult Success() => new(true, string.Empty);
    public static new SaveRingtonePreferenceResult Failure(string error) => new(false, error);
}
