namespace Kairudev.Application.Pomodoro.GetCurrentSession;

public interface IGetCurrentSessionUseCase
{
    Task Execute(CancellationToken cancellationToken = default);
}
