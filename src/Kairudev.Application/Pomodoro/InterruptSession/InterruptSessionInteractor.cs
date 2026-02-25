using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.InterruptSession;

public sealed class InterruptSessionInteractor : IInterruptSessionUseCase
{
    private readonly IPomodoroSessionRepository _repository;
    private readonly IInterruptSessionPresenter _presenter;

    public InterruptSessionInteractor(
        IPomodoroSessionRepository repository,
        IInterruptSessionPresenter presenter)
    {
        _repository = repository;
        _presenter = presenter;
    }

    public async Task Execute(InterruptSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetActiveAsync(cancellationToken);
        if (session is null)
        {
            _presenter.PresentFailure(DomainErrors.Pomodoro.SessionNotActive);
            return;
        }

        session.Interrupt(DateTime.UtcNow);
        await _repository.UpdateAsync(session, cancellationToken);
        _presenter.PresentSuccess();
    }
}
