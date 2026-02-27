using Kairudev.Application.Settings.Common;
using Kairudev.Domain.Settings;

namespace Kairudev.Application.Settings.Queries.GetUserSettings;

public sealed class GetUserSettingsQueryHandler
{
    private readonly IUserSettingsRepository _repository;

    public GetUserSettingsQueryHandler(IUserSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetUserSettingsResult> Handle(GetUserSettingsQuery query)
    {
        var settings = await _repository.GetAsync();

        var viewModel = new UserSettingsViewModel(
            ThemePreference: settings.ThemePreference.ToString(),
            RingtonePreference: settings.RingtonePreference.ToString()
        );

        return GetUserSettingsResult.Success(viewModel);
    }
}
