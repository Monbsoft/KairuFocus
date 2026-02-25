using Kairudev.Application.Pomodoro.Common;
using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.StartSession;

public sealed class StartSessionInteractor : IStartSessionUseCase
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly IStartSessionPresenter _presenter;

    public StartSessionInteractor(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        IStartSessionPresenter presenter)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _presenter = presenter;
    }

    public async Task Execute(StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _sessionRepository.GetActiveAsync(cancellationToken);
        if (existing is not null)
        {
            _presenter.PresentFailure(DomainErrors.Pomodoro.SessionAlreadyActive);
            return;
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken);
        var session = PomodoroSession.Create(settings.SprintDurationMinutes);
        session.Start(DateTime.UtcNow);

        await _sessionRepository.AddAsync(session, cancellationToken);
        _presenter.PresentSuccess(PomodoroSessionViewModel.From(session));
    }
}
