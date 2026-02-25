namespace Kairudev.Application.Pomodoro.InterruptSession;

public interface IInterruptSessionUseCase
{
    Task Execute(InterruptSessionRequest request, CancellationToken cancellationToken = default);
}
