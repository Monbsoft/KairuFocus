namespace Kairudev.Application.Pomodoro.StartSession;

public interface IStartSessionUseCase
{
    Task Execute(StartSessionRequest request, CancellationToken cancellationToken = default);
}
