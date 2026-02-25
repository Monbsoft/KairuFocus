namespace Kairudev.Application.Pomodoro.CompleteSession;

public interface ICompleteSessionUseCase
{
    Task Execute(CompleteSessionRequest request, CancellationToken cancellationToken = default);
}
