namespace Kairudev.Application.Pomodoro.GetSuggestedSessionType;

public interface IGetSuggestedSessionTypeUseCase
{
    Task Execute(CancellationToken cancellationToken = default);
}
