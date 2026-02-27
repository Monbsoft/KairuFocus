using Kairudev.Application.Pomodoro.Common;
using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.Queries.GetSettings;

public sealed class GetSettingsQueryHandler
{
    private readonly IPomodoroSettingsRepository _repository;

    public GetSettingsQueryHandler(IPomodoroSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetSettingsResult> HandleAsync(
        GetSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        var viewModel = PomodoroSettingsViewModel.From(settings);
        return new GetSettingsResult(viewModel);
    }
}
