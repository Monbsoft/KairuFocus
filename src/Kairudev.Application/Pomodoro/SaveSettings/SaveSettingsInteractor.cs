using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.SaveSettings;

public sealed class SaveSettingsInteractor : ISaveSettingsUseCase
{
    private readonly IPomodoroSettingsRepository _repository;
    private readonly ISaveSettingsPresenter _presenter;

    public SaveSettingsInteractor(IPomodoroSettingsRepository repository, ISaveSettingsPresenter presenter)
    {
        _repository = repository;
        _presenter = presenter;
    }

    public async Task Execute(SaveSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var result = PomodoroSettings.Create(
            request.SprintDurationMinutes,
            request.ShortBreakDurationMinutes,
            request.LongBreakDurationMinutes);

        if (result.IsFailure)
        {
            _presenter.PresentValidationError(result.Error);
            return;
        }

        await _repository.SaveAsync(result.Value, cancellationToken);
        _presenter.PresentSuccess();
    }
}
