using Kairudev.Application.Pomodoro.Common;
using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.GetSettings;

public sealed class GetSettingsInteractor : IGetSettingsUseCase
{
    private readonly IPomodoroSettingsRepository _repository;
    private readonly IGetSettingsPresenter _presenter;

    public GetSettingsInteractor(IPomodoroSettingsRepository repository, IGetSettingsPresenter presenter)
    {
        _repository = repository;
        _presenter = presenter;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        _presenter.PresentSuccess(PomodoroSettingsViewModel.From(settings));
    }
}
