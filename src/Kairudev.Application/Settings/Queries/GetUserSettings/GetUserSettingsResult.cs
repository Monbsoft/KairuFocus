using Kairudev.Application.Settings.Common;
using Kairudev.Domain.Common;

namespace Kairudev.Application.Settings.Queries.GetUserSettings;

public sealed class GetUserSettingsResult : Result
{
    public UserSettingsViewModel Settings { get; }

    private GetUserSettingsResult(UserSettingsViewModel settings) : base(true, string.Empty)
    {
        Settings = settings;
    }

    public static GetUserSettingsResult Success(UserSettingsViewModel settings) => new(settings);
}

